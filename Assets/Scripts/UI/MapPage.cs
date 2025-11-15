using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Displays an unlit-style world map by rendering a separate orthographic camera to a RawImage.
// - Excludes Player/Enemies/UI via layer mask
// - Fits camera to world bounds (auto-computed or manual)
// - Overlays a small UI dot at the player's position
public class MapPage : MonoBehaviour
{
    public enum Plane { XY, XZ }

    [Header("UI")]
    [SerializeField] private RawImage mapImage; // Target image to show the map render texture
    [SerializeField] private RectTransform playerDot; // Optional; created automatically if missing
    [SerializeField] private Sprite playerDotSprite;
    [SerializeField] private Color playerDotColor = Color.white;
    [SerializeField] private Vector2 playerDotSize = new Vector2(8, 8);

    [Header("Camera & Rendering")]
    [Tooltip("Layers to exclude from the map (e.g., Player, Enemies, UI)")]
    [SerializeField] private LayerMask excludeLayers;
    [SerializeField] private Color clearColor = Color.black;
    [SerializeField] private Plane mapPlane = Plane.XY; // XY for 2D, XZ for top-down 3D
    [SerializeField] private float worldPadding = 5f; // world units padding around bounds
    [Tooltip("Optional: set a URP Renderer index that uses a Render Objects feature with an Unlit override material")]
    [SerializeField] private int urpRendererIndex = -1;

    [Header("Bounds")]
    [Tooltip("Auto-compute bounds from scene renderers (excluding excluded layers)")]
    [SerializeField] private bool autoComputeBounds = true;
    [Tooltip("Optional root to limit auto-compute bounds; if null, scans entire scene.")]
    [SerializeField] private Transform boundsRoot;
    [Tooltip("Manual bounds if auto-compute disabled")] 
    [SerializeField] private Bounds manualBounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));

    [Header("Player Tracking")]
    [SerializeField] private Transform player; // If null, will find by tag "Player"

    [Header("Controls")]
    [Tooltip("World-units per second at orthographicSize = base; scales with zoom for constant on-screen speed.")]
    [SerializeField] private float panSpeed = 30f;
    [Tooltip("Enable old toggle zoom mode (Space/Enter or Gamepad South). Disable to use continuous axis-based zoom.")]
    [SerializeField] private bool enableZoomToggle = false;
    [Tooltip("Enable continuous hold-based zoom using mouse wheel, keys, or gamepad triggers/stick.")]
    [SerializeField] private bool enableAxisZoom = true;

    private Camera mapCamera;
    private RenderTexture rt;
    private Bounds worldBounds;
    private Vector2 lastRTSize;
    private Vector3 currentCenter; // camera center in world space
    private float baseOrthoSize;   // computed to fit bounds
    [Header("Zoom")]
    [Tooltip("When toggle zoom is used, show this fraction of the full-map height. 0.25 = 25% of map height (strong zoom).")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float zoomedCoverageFraction = 0.25f;
    [Tooltip("Minimum coverage fraction for axis-zoom (lower = more zoom in). 0.1 means max zoom shows 10% of map height.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float axisZoomMinCoverageFraction = 0.15f;
    [Tooltip("Maximum coverage fraction for axis-zoom (1 = full map).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float axisZoomMaxCoverageFraction = 1f;
    [Tooltip("Speed at which the coverage fraction changes per second when holding zoom.")]
    [SerializeField] private float axisZoomSpeed = 1.5f;
    [Tooltip("Mouse wheel sensitivity scaling for axis-zoom.")]
    [SerializeField] private float mouseWheelZoomScale = 0.15f;
    private bool isZoomed = false; // legacy toggle path
    private float zoomFraction = 1f; // 1 = full map, lower = zoom in
    // Persist last map view across menu close/reopen (per play session)
    private static bool s_HasState = false;
    private static Vector3 s_SavedCenter;
    private static bool s_SavedIsZoomed;
    private static float s_SavedZoomFraction = 1f;

    private void Awake()
    {
        if (mapImage == null)
        {
            mapImage = GetComponentInChildren<RawImage>(true);
        }
        // Set a reasonable default exclusion if none provided
        if (excludeLayers.value == 0)
        {
            excludeLayers = LayerMask.GetMask("UI", "Player", "Enemies", "Enemy");
        }
        EnsurePlayerDot();
        BuildOrUpdateCamera();
        ComputeWorldBounds();
        FitCameraToBounds();
        UpdatePlayerDot();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (rt != null)
        {
            if (mapImage != null && mapImage.texture == rt) mapImage.texture = null;
            rt.Release();
            Destroy(rt);
        }
        if (mapCamera != null)
        {
            Destroy(mapCamera.gameObject);
        }
    }

    public void Refresh()
    {
        BuildOrUpdateCamera();
        ComputeWorldBounds();
        FitCameraToBounds();

        // Restore previously saved state (if any)
        if (s_HasState)
        {
            isZoomed = s_SavedIsZoomed;
            currentCenter = s_SavedCenter;
            zoomFraction = Mathf.Clamp(s_SavedZoomFraction, axisZoomMinCoverageFraction, axisZoomMaxCoverageFraction);
            if (enableAxisZoom)
                mapCamera.orthographicSize = baseOrthoSize * zoomFraction;
            else
                mapCamera.orthographicSize = baseOrthoSize * (isZoomed ? Mathf.Clamp01(zoomedCoverageFraction) : 1f);
            ApplyCameraTransform();
        }

        UpdatePlayerDot();
    }

    private void Update()
    {
        // Only update when visible
        if (!isActiveAndEnabled) return;
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }
        HandleInput();
        ApplyCameraTransform();
        UpdatePlayerDot();
    }

    private void OnRectTransformDimensionsChange()
    {
        // Recreate RT if size changed
        if (mapImage == null) return;
        var size = GetTargetPixelSize();
        if (size != lastRTSize && size.x > 0 && size.y > 0)
        {
            BuildOrUpdateCamera();
            FitCameraToBounds();
        }
    }

    private void EnsurePlayerDot()
    {
        if (playerDot == null && mapImage != null)
        {
            var dotGO = new GameObject("PlayerDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dotGO.transform.SetParent(mapImage.transform, false);
            playerDot = dotGO.GetComponent<RectTransform>();
            var img = dotGO.GetComponent<Image>();
            img.sprite = playerDotSprite;
            img.color = playerDotColor;
            img.raycastTarget = false;
            playerDot.sizeDelta = playerDotSize;
            playerDot.anchorMin = Vector2.zero;
            playerDot.anchorMax = Vector2.zero;
            playerDot.pivot = new Vector2(0.5f, 0.5f);
            // Ensure the dot renders above the map image
            playerDot.SetAsLastSibling();
        }
    }

    private void BuildOrUpdateCamera()
    {
        if (mapImage == null) return;
        var pixelSize = GetTargetPixelSize();
        if (pixelSize.x <= 0 || pixelSize.y <= 0)
        {
            pixelSize = new Vector2(512, 512);
        }

        if (rt == null || lastRTSize != pixelSize)
        {
            if (rt != null)
            {
                if (mapImage.texture == rt) mapImage.texture = null;
                rt.Release();
                Destroy(rt);
            }
            rt = new RenderTexture(Mathf.RoundToInt(pixelSize.x), Mathf.RoundToInt(pixelSize.y), 16, RenderTextureFormat.Default);
            rt.name = "MapPage_RT";
            rt.Create();
            mapImage.texture = rt;
            lastRTSize = pixelSize;
        }

        if (mapCamera == null)
        {
            var camGO = new GameObject("MapCamera");
            camGO.transform.SetParent(transform, false);
            mapCamera = camGO.AddComponent<Camera>();
            var urpData = camGO.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderPostProcessing = false;
            urpData.antialiasing = AntialiasingMode.None;
            urpData.volumeLayerMask = 0; // no volumes
        }

        mapCamera.orthographic = true;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = clearColor;
        mapCamera.nearClipPlane = 0.01f;
        mapCamera.farClipPlane = 10000f;
        mapCamera.targetTexture = rt;
        mapCamera.depth = -10f;

        // Culling: include everything except excluded layers
        mapCamera.cullingMask = ~excludeLayers;

        // URP renderer override if configured
        var data = mapCamera.GetComponent<UniversalAdditionalCameraData>();
        if (data != null && urpRendererIndex >= 0)
        {
            try { data.SetRenderer(urpRendererIndex); } catch { }
        }

        // Orient based on plane
        if (mapPlane == Plane.XY)
        {
            mapCamera.transform.rotation = Quaternion.identity;
        }
        else // XZ top-down
        {
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private Vector2 GetTargetPixelSize()
    {
        if (mapImage == null) return Vector2.zero;
        var r = mapImage.rectTransform.rect;
        var w = Mathf.Max(1, Mathf.RoundToInt(r.width));
        var h = Mathf.Max(1, Mathf.RoundToInt(r.height));
        return new Vector2(w, h);
    }

    private void ComputeWorldBounds()
    {
        if (!autoComputeBounds)
        {
            worldBounds = manualBounds;
            return;
        }

        var renderers = boundsRoot != null
            ? boundsRoot.GetComponentsInChildren<Renderer>(true)
            : FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        bool hasAny = false;
        var mask = ~excludeLayers;
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue;
            if (((1 << r.gameObject.layer) & mask) == 0) continue; // excluded
            if (!hasAny)
            {
                worldBounds = r.bounds;
                hasAny = true;
            }
            else
            {
                worldBounds.Encapsulate(r.bounds);
            }
        }

        if (!hasAny)
        {
            // Fallback to a reasonable area around origin
            worldBounds = new Bounds(Vector3.zero, new Vector3(50, 50, 50));
        }

        // Add padding
        worldBounds.Expand(new Vector3(worldPadding * 2f, worldPadding * 2f, worldPadding * 2f));
    }

    private void FitCameraToBounds()
    {
        if (mapCamera == null) return;

        // Position camera at center plus elevation on the third axis
        var center = worldBounds.center;
        currentCenter = center;
        if (mapPlane == Plane.XY)
            mapCamera.transform.position = new Vector3(currentCenter.x, currentCenter.y, -1000f);
        else
            mapCamera.transform.position = new Vector3(currentCenter.x, 1000f, currentCenter.z);

        // Compute orthographic size to fit the bounds while preserving aspect
        var sizeX = worldBounds.extents.x;
        var sizeY = mapPlane == Plane.XY ? worldBounds.extents.y : worldBounds.extents.z;
        float aspect = rt != null ? (rt.width / (float)rt.height) : 1f;
    float orthoSize = Mathf.Max(sizeY, sizeX / Mathf.Max(0.0001f, aspect));
    baseOrthoSize = Mathf.Max(1f, orthoSize);
    mapCamera.orthographicSize = baseOrthoSize * (enableAxisZoom ? zoomFraction : (isZoomed ? Mathf.Clamp01(zoomedCoverageFraction) : 1f));
    }

    private void UpdatePlayerDot()
    {
        if (playerDot == null || mapImage == null || mapCamera == null) return;
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo == null) return;
            player = pgo.transform;
        }

        // Use the camera's viewport mapping for perfect alignment with the rendered map (accounts for aspect/ortho size)
        Vector3 vp = mapCamera.WorldToViewportPoint(player.position);
        // If behind camera, skip (shouldn't happen with our ortho setup, but just in case)
        if (vp.z < 0f) return;

        // Handle RawImage preserveAspect by computing the actual drawn texture rect inside the RawImage rect
        var rect = mapImage.rectTransform.rect;
        float viewW = rect.width, viewH = rect.height, offX = 0f, offY = 0f;

        // Anchors at bottom-left (0,0), so anchoredPosition is in local pixel space from the RawImage's bottom-left
        float px = offX + viewW * Mathf.Clamp01(vp.x);
        float py = offY + viewH * Mathf.Clamp01(vp.y);
        playerDot.anchoredPosition = new Vector2(px, py);
    }

    private void HandleInput()
    {
        // Use unscaled delta (menu likely paused with timeScale=0)
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f || mapCamera == null) return;

        // Read WASD/Arrow keys and gamepad left stick
        Vector2 move = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
        }
        var gp = Gamepad.current;
        if (gp != null)
        {
            move += gp.leftStick.ReadValue();
        }
        move = Vector2.ClampMagnitude(move, 1f);

        if (move.sqrMagnitude > 0.0001f)
        {
            // Scale pan speed by current zoom to keep on-screen speed roughly constant
            // World-units per second proportional to orthographicSize
            float speed = panSpeed * (Mathf.Max(0.0001f, mapCamera.orthographicSize) / Mathf.Max(0.0001f, baseOrthoSize));
            Vector2 delta = move * speed * dt;

            if (mapPlane == Plane.XY)
                currentCenter += new Vector3(delta.x, delta.y, 0f);
            else
                currentCenter += new Vector3(delta.x, 0f, delta.y);
        }

        if (enableAxisZoom)
        {
            float input = 0f;
            // Mouse wheel
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var scroll = mouse.scroll.ReadValue().y; // typically positive up
                input += scroll * mouseWheelZoomScale; // not scaled by dt; scroll is event-like
            }
            // Keyboard keys: E/+ = zoom in, Q/- = zoom out
            if (kb != null)
            {
                if (kb.eKey.isPressed || kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) input += 1f;
                if (kb.qKey.isPressed || kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) input -= 1f;
            }
            // Gamepad: RT zoom in, LT zoom out; also dpad up/down (optional)
            if (gp != null)
            {
                input += gp.rightTrigger.ReadValue();
                input -= gp.leftTrigger.ReadValue();
                input += gp.dpad.up.isPressed ? 0.5f : 0f;
                input -= gp.dpad.down.isPressed ? 0.5f : 0f;
                // Right stick vertical can also control zoom a bit
                input += gp.rightStick.ReadValue().y * 0.5f;
            }

            if (Mathf.Abs(input) > 0.0001f)
            {
                // Convert input to coverage fraction change (lower fraction = more zoom in)
                float delta = input * axisZoomSpeed * dt;
                // Mouse wheel already large; minor cap to avoid huge jumps per frame
                delta = Mathf.Clamp(delta, -0.5f, 0.5f);
                zoomFraction = Mathf.Clamp(zoomFraction - delta, axisZoomMinCoverageFraction, axisZoomMaxCoverageFraction);
                mapCamera.orthographicSize = baseOrthoSize * zoomFraction;
            }
        }
        else if (enableZoomToggle)
        {
            bool toggle = false;
            if (kb != null)
            {
                toggle |= kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
            }
            if (gp != null)
            {
                toggle |= gp.buttonSouth.wasPressedThisFrame; // A/Cross
            }
            if (toggle)
            {
                isZoomed = !isZoomed;
                mapCamera.orthographicSize = baseOrthoSize * (isZoomed ? Mathf.Clamp01(zoomedCoverageFraction) : 1f);

                // When entering zoomed mode, center on the player by default
                if (isZoomed)
                {
                    if (player == null)
                    {
                        var pgo = GameObject.FindGameObjectWithTag("Player");
                        if (pgo != null) player = pgo.transform;
                    }
                    if (player != null)
                    {
                        var p = player.position;
                        if (mapPlane == Plane.XY)
                            currentCenter = new Vector3(p.x, p.y, currentCenter.z);
                        else
                            currentCenter = new Vector3(p.x, currentCenter.y, p.z);
                    }
                }
            }
        }

        // Interact-to-recenter when zoomed in (works for both zoom modes)
        bool isZoomedInNow = enableAxisZoom ? (zoomFraction < 0.999f) : isZoomed;
        if (isZoomedInNow)
        {
            bool interactPressed = false;
            if (kb != null)
            {
                // Common interact keys: F (typical), E (project uses sometimes), Enter/Space as fallbacks
                interactPressed |= kb.fKey.wasPressedThisFrame
                                   || kb.eKey.wasPressedThisFrame
                                   || kb.enterKey.wasPressedThisFrame
                                   || kb.spaceKey.wasPressedThisFrame;
            }
            if (gp != null)
            {
                // Common interact buttons: West (X) and South (A/Cross) as fallback
                interactPressed |= gp.buttonWest.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame;
            }

            if (interactPressed)
            {
                if (player == null)
                {
                    var pgo = GameObject.FindGameObjectWithTag("Player");
                    if (pgo != null) player = pgo.transform;
                }
                if (player != null)
                {
                    var p = player.position;
                    if (mapPlane == Plane.XY)
                        currentCenter = new Vector3(p.x, p.y, currentCenter.z);
                    else
                        currentCenter = new Vector3(p.x, currentCenter.y, p.z);
                }
            }
        }
    }

    private void ApplyCameraTransform()
    {
        if (mapCamera == null) return;

        // Clamp center so the view remains within world bounds
        float aspect = rt != null ? (rt.width / (float)rt.height) : 1f;
        float halfH = mapCamera.orthographicSize;
        float halfW = halfH * aspect;

        if (mapPlane == Plane.XY)
        {
            float minX = worldBounds.min.x + halfW;
            float maxX = worldBounds.max.x - halfW;
            float minY = worldBounds.min.y + halfH;
            float maxY = worldBounds.max.y - halfH;

            // If bounds smaller than view, keep center at bounds center
            if (minX <= maxX) currentCenter.x = Mathf.Clamp(currentCenter.x, minX, maxX); else currentCenter.x = worldBounds.center.x;
            if (minY <= maxY) currentCenter.y = Mathf.Clamp(currentCenter.y, minY, maxY); else currentCenter.y = worldBounds.center.y;

            mapCamera.transform.position = new Vector3(currentCenter.x, currentCenter.y, -1000f);
        }
        else
        {
            float minX = worldBounds.min.x + halfW;
            float maxX = worldBounds.max.x - halfW;
            float minZ = worldBounds.min.z + halfH;
            float maxZ = worldBounds.max.z - halfH;

            if (minX <= maxX) currentCenter.x = Mathf.Clamp(currentCenter.x, minX, maxX); else currentCenter.x = worldBounds.center.x;
            if (minZ <= maxZ) currentCenter.z = Mathf.Clamp(currentCenter.z, minZ, maxZ); else currentCenter.z = worldBounds.center.z;

            mapCamera.transform.position = new Vector3(currentCenter.x, 1000f, currentCenter.z);
        }
    }

    private void OnDisable()
    {
        SaveState();
    }

    private void SaveState()
    {
        // Save last camera center and zoom flag
        s_SavedCenter = currentCenter;
        s_SavedIsZoomed = isZoomed;
        s_SavedZoomFraction = zoomFraction;
        s_HasState = true;
    }
}
