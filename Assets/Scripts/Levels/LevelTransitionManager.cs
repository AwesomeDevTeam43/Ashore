using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Central manager for transitioning between portals and scenes.
// Future-proofed for multi-scene Metroidvania structure; supports simple in-scene teleport now.
public class LevelTransitionManager : MonoBehaviour
{
  public static LevelTransitionManager Instance { get; private set; }
  public static float LastTeleportTime { get; private set; }

  [Header("Visuals")]
  [SerializeField] private bool useFade = true;
  [SerializeField] private float fadeOutDuration = 0.2f;
  [SerializeField] private float fadeHoldDuration = 0f;
  [SerializeField] private float fadeInDuration = 0.2f;
  [SerializeField] private bool snapCinemachineOnTeleport = true; // move vcams by player delta to avoid soft drift
  [Header("Loading Screen")]
  [SerializeField] private bool useLoadingScreen = true;
  [SerializeField] private string loadingText = "Loading...";
  [SerializeField] private float minLoadingShowSeconds = 2.0f;

  // Local fade overlay to avoid cross-assembly dependencies
  private Canvas _fadeCanvas;
  private UnityEngine.UI.Image _fadeImage;
  private Coroutine _fadeRoutine;
  private UnityEngine.CanvasGroup _loadingGroup;
  private UnityEngine.UI.Text _loadingLabel;
  private UnityEngine.UI.Image _loadingBarBg;
  private UnityEngine.UI.Image _loadingBarFill;

  // Registry of portals by (sceneName, portalId)
  private readonly Dictionary<(string scene, string id), LevelPortal> _portals = new();

  // Transition gating to avoid immediate re-trigger upon arrival
  [SerializeField] private float portalTriggerIgnoreSeconds = 0.5f;
  private float _ignoreUntilTime = 0f;
  private bool _isTransitioning = false;

  // Pending target for cross-scene transitions
  private string _pendingTargetScene;
  private string _pendingTargetPortalId;
  private bool _isAsyncLoading;

  public static void TryEnsureExists()
  {
    if (Instance != null) return;
    // Prefer an author-placed instance in the scene if available
    var existing = Object.FindObjectsByType<LevelTransitionManager>(FindObjectsSortMode.None);
    if (existing != null && existing.Length > 0)
    {
      Instance = existing[0];
      DontDestroyOnLoad(Instance.gameObject);
      return;
    }
    // Otherwise create a default one with code defaults
    var go = new GameObject("LevelTransitionManager");
    Instance = go.AddComponent<LevelTransitionManager>();
    DontDestroyOnLoad(go);
  }

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
    SceneManager.sceneLoaded += OnSceneLoaded;
    // Ensure there is a GameState singleton present
    if (GameState.Instance == null)
    {
      var gs = new GameObject("GameState");
      gs.AddComponent<GameState>();
    }
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }
  }

  private void OnValidate()
  {
    if (fadeOutDuration < 0f) fadeOutDuration = 0f;
    if (fadeInDuration < 0f) fadeInDuration = 0f;
    if (fadeHoldDuration < 0f) fadeHoldDuration = 0f;
  }

  public void RegisterPortal(LevelPortal portal)
  {
    if (portal == null) return;
    var sceneName = portal.gameObject.scene.name;
    var key = (sceneName, portal.portalId);
    _portals[key] = portal;
  }

  public void UnregisterPortal(LevelPortal portal)
  {
    if (portal == null) return;
    var sceneName = portal.gameObject.scene.name;
    var key = (sceneName, portal.portalId);
    if (_portals.ContainsKey(key)) _portals.Remove(key);
  }

  public bool CanAcceptPortalTrigger(LevelPortal from)
  {
    if (_isTransitioning) return false;
    return Time.unscaledTime >= _ignoreUntilTime;
  }

  public void RequestTransition(LevelPortal from)
  {
    if (from == null) return;
    if (_isTransitioning) return;

    _isTransitioning = true;

    // Capture game state before leaving (future-proof for cross-scene)
    GameState.Instance?.CaptureAll();

    var targetScene = string.IsNullOrEmpty(from.targetScene) ? SceneManager.GetActiveScene().name : from.targetScene;
    var targetPortalId = string.IsNullOrEmpty(from.targetPortalId) ? from.portalId : from.targetPortalId;

    if (targetScene == SceneManager.GetActiveScene().name)
    {
      if (useFade)
      {
        StartCoroutine(DoInSceneTeleportWithFade(targetScene, targetPortalId));
      }
      else
      {
        // In-scene teleport (use destination portal's own spawnOffset)
        TeleportInScene(targetScene, targetPortalId);
        _isTransitioning = false;
        _ignoreUntilTime = Time.unscaledTime + portalTriggerIgnoreSeconds;
      }
    }
    else
    {
      // Cross-scene: remember target and load scene
      _pendingTargetScene = targetScene;
      _pendingTargetPortalId = targetPortalId;

      if (useLoadingScreen)
      {
        if (!_isAsyncLoading)
        {
          StartCoroutine(DoCrossSceneTransitionAsync(targetScene, targetPortalId));
        }
      }
      else
      {
        if (useFade) StartFadeOut(fadeOutDuration);
        SceneManager.LoadSceneAsync(targetScene);
      }
    }
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    if (_isAsyncLoading)
    {
      // The async transition coroutine will handle post-load work and fade-in
      return;
    }
    if (string.IsNullOrEmpty(_pendingTargetScene))
    {
      // No pending transition, just ignore.
      return;
    }

    // Restore states now that scene objects exist
    GameState.Instance?.RestoreAll();

    // Place player (use destination portal's own spawnOffset)
    TeleportInScene(_pendingTargetScene, _pendingTargetPortalId);

    _pendingTargetScene = null;
    _pendingTargetPortalId = null;

    _isTransitioning = false;
    _ignoreUntilTime = Time.unscaledTime + portalTriggerIgnoreSeconds;

    if (useFade) StartFadeIn(fadeInDuration);
  }

  private void TeleportInScene(string sceneName, string targetPortalId)
  {
    var key = (sceneName, targetPortalId);
    if (!_portals.TryGetValue(key, out var dest))
    {
      Debug.LogWarning($"LevelTransitionManager: Destination portal '{targetPortalId}' not found in scene '{sceneName}'.");
      return;
    }

    // Find player
    var player = GameObject.FindGameObjectWithTag("Player");
    if (player == null)
    {
      Debug.LogWarning("LevelTransitionManager: Player not found when attempting teleport.");
      return;
    }

    var oldPlayerPos = player.transform.position;
    var targetPos = (Vector2)dest.transform.position + dest.spawnOffset;
    player.transform.position = new Vector3(targetPos.x, targetPos.y, player.transform.position.z);

    // Optional: zero player velocity to avoid re-trigger or launch issues
    var rb2d = player.GetComponent<Rigidbody2D>();
    if (rb2d != null)
    {
      rb2d.linearVelocity = Vector2.zero;
      rb2d.angularVelocity = 0f;
    }

    LastTeleportTime = Time.unscaledTime;

    if (snapCinemachineOnTeleport)
    {
      var delta = player.transform.position - oldPlayerPos;
      // Move all active virtual cameras by the same delta to reduce large drift
      var vcams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
      foreach (var v in vcams)
      {
        if (v != null) v.transform.position += delta;
      }

      // Also nudge main camera to reduce a visible frame of drift if any
      var cam = Camera.main;
      if (cam != null) cam.transform.position += delta;

      // And zero damping for one frame to truly snap
      StartCoroutine(SnapCinemachineOneFrame());
    }
  }

  private System.Collections.IEnumerator DoInSceneTeleportWithFade(string sceneName, string portalId)
  {
    yield return FadeOutInRoutine(fadeOutDuration, fadeHoldDuration, fadeInDuration, () =>
    {
      TeleportInScene(sceneName, portalId);
    });

    _isTransitioning = false;
    _ignoreUntilTime = Time.unscaledTime + portalTriggerIgnoreSeconds;
  }

  private struct ComposerSnap
  {
    public CinemachinePositionComposer comp;
    public Vector3 damping;
  }

  private System.Collections.IEnumerator SnapCinemachineOneFrame()
  {
    var composers = Object.FindObjectsByType<CinemachinePositionComposer>(FindObjectsSortMode.None);
    var vcams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);

    var compSnaps = new System.Collections.Generic.List<ComposerSnap>(composers.Length);
    foreach (var c in composers)
    {
      if (c == null) continue;
      var snap = new ComposerSnap { comp = c, damping = c.Damping };
      compSnaps.Add(snap);
      c.Damping = Vector3.zero;
    }

  // Disable and re-enable vcams around one frame to force a hard cut recenter
  foreach (var v in vcams) if (v != null) v.enabled = false;
  yield return null;
  foreach (var v in vcams) if (v != null) v.enabled = true;

    foreach (var s in compSnaps)
    {
      if (s.comp != null) s.comp.Damping = s.damping;
    }
  }

  private void EnsureFader()
  {
    if (_fadeCanvas != null && _fadeImage != null) return;
    // Prefer a shared global overlay if present
    var global = GlobalLoadingOverlay.Instance;
    if (global != null && global.FadeImage != null)
    {
      _fadeCanvas = global.GetComponent<Canvas>();
      _fadeImage = global.FadeImage;
      _loadingGroup = global.LoadingGroup;
      _loadingLabel = global.LoadingLabel;
      _loadingBarBg = global.LoadingBarBg;
      _loadingBarFill = global.LoadingBarFill;
      SetFadeAlpha(0f);
      return;
    }
    var go = new GameObject("LevelTransitionFader");
    DontDestroyOnLoad(go);
    _fadeCanvas = go.AddComponent<Canvas>();
    _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    _fadeCanvas.sortingOrder = short.MaxValue;
    go.AddComponent<UnityEngine.UI.CanvasScaler>();
    // No GraphicRaycaster needed on fader root; we'll control raycast on the image/panel itself

    var imgGo = new GameObject("Fade");
    imgGo.transform.SetParent(go.transform, false);
    _fadeImage = imgGo.AddComponent<UnityEngine.UI.Image>();
    _fadeImage.color = Color.black;
    var rt = _fadeImage.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    _fadeImage.raycastTarget = false; // do not block UI when transparent
    SetFadeAlpha(0f);

    // Build a simple loading UI (hidden by default)
    var panel = new GameObject("LoadingPanel");
    panel.transform.SetParent(go.transform, false);
    var prt = panel.AddComponent<RectTransform>();
    prt.anchorMin = new Vector2(0.5f, 0.15f);
    prt.anchorMax = new Vector2(0.5f, 0.15f);
    prt.sizeDelta = new Vector2(420, 80);
    prt.anchoredPosition = Vector2.zero;

    _loadingGroup = panel.AddComponent<UnityEngine.CanvasGroup>();
    _loadingGroup.alpha = 0f;
    _loadingGroup.blocksRaycasts = false;
    _loadingGroup.interactable = false;

    var textGo = new GameObject("Label");
    textGo.transform.SetParent(panel.transform, false);
    _loadingLabel = textGo.AddComponent<UnityEngine.UI.Text>();
    _loadingLabel.text = loadingText;
    _loadingLabel.alignment = TextAnchor.MiddleCenter;
    _loadingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    _loadingLabel.fontSize = 24;
    var trt = _loadingLabel.rectTransform; trt.anchorMin = new Vector2(0.5f, 1f); trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 1f); trt.sizeDelta = new Vector2(420, 30); trt.anchoredPosition = new Vector2(0, 0);

    // Progress bar background
    var barBgGo = new GameObject("BarBG");
    barBgGo.transform.SetParent(panel.transform, false);
    _loadingBarBg = barBgGo.AddComponent<UnityEngine.UI.Image>();
    _loadingBarBg.color = new Color(1f, 1f, 1f, 0.2f);
    var bgrt = _loadingBarBg.rectTransform; bgrt.anchorMin = new Vector2(0.5f, 0f); bgrt.anchorMax = new Vector2(0.5f, 0f); bgrt.pivot = new Vector2(0.5f, 0f); bgrt.sizeDelta = new Vector2(420, 20); bgrt.anchoredPosition = new Vector2(0, 0);

    // Progress bar fill
    var barFillGo = new GameObject("BarFill");
    barFillGo.transform.SetParent(barBgGo.transform, false);
    _loadingBarFill = barFillGo.AddComponent<UnityEngine.UI.Image>();
    _loadingBarFill.color = new Color(1f, 1f, 1f, 0.85f);
    var frt = _loadingBarFill.rectTransform; frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f); frt.pivot = new Vector2(0f, 0.5f); frt.sizeDelta = new Vector2(0, 0);
  }

  private void SetFadeAlpha(float a)
  {
    if (_fadeImage == null) return;
    var c = _fadeImage.color; c.a = Mathf.Clamp01(a); _fadeImage.color = c;
    // Only block UI when visible enough to matter
    _fadeImage.raycastTarget = c.a > 0.02f;
  }

  private void StartFadeOut(float duration)
  {
    EnsureFader();
    if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
    _fadeRoutine = StartCoroutine(FadeTo(1f, duration));
  }

  private void StartFadeIn(float duration)
  {
    EnsureFader();
    if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
    _fadeRoutine = StartCoroutine(FadeTo(0f, duration));
  }

  private System.Collections.IEnumerator FadeOutInRoutine(float outDur, float hold, float inDur, System.Action middle)
  {
    EnsureFader();
    yield return FadeTo(1f, outDur);
    middle?.Invoke();
    if (hold > 0f) yield return new WaitForSecondsRealtime(hold);
    yield return FadeTo(0f, inDur);
    _fadeRoutine = null;
  }

  private System.Collections.IEnumerator FadeTo(float target, float duration)
  {
    EnsureFader();
    float start = _fadeImage.color.a; float t = 0f;
    while (t < duration)
    {
      t += Time.unscaledDeltaTime;
      float a = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
      SetFadeAlpha(a);
      yield return null;
    }
    SetFadeAlpha(target);
  }

  private void SetLoadingVisible(bool visible)
  {
    if (_loadingGroup == null) return;
    _loadingGroup.alpha = visible ? 1f : 0f;
    _loadingGroup.blocksRaycasts = visible;
    _loadingGroup.interactable = visible;
  }

  private void SetLoadingProgress(float progress)
  {
    if (_loadingBarFill == null || _loadingBarBg == null) return;
    progress = Mathf.Clamp01(progress);
    var maxWidth = _loadingBarBg.rectTransform.rect.width;
    var frt = _loadingBarFill.rectTransform;
    frt.sizeDelta = new Vector2(maxWidth * progress, 0f);
  }

  private System.Collections.IEnumerator DoCrossSceneTransitionAsync(string sceneName, string targetPortalId)
  {
    _isAsyncLoading = true;
    EnsureFader();
    // Go to full black immediately to hide background changes
    if (useFade) SetFadeAlpha(1f);
    SetLoadingVisible(true);
    SetLoadingProgress(0f);
    Canvas.ForceUpdateCanvases();
    yield return null; // present loading UI for at least one frame

    var startShown = Time.unscaledTime;
    var op = SceneManager.LoadSceneAsync(sceneName);
    op.allowSceneActivation = false;

    while (op.progress < 0.9f)
    {
      // Unity reports progress up to 0.9 while loading
      SetLoadingProgress(op.progress / 0.9f);
      yield return null;
    }
    SetLoadingProgress(1f);

    // Make sure the loading UI is visible for a minimal duration to avoid flicker
    float elapsed = Time.unscaledTime - startShown;
    if (elapsed < minLoadingShowSeconds)
      yield return new WaitForSecondsRealtime(minLoadingShowSeconds - elapsed);

    // Activate the scene
    op.allowSceneActivation = true;
    while (!op.isDone) yield return null;

    // After activation, portals should have registered; complete placement
    GameState.Instance?.RestoreAll();
    TeleportInScene(sceneName, targetPortalId);
    // Extra delayed snap to ensure new scene's Cinemachine cameras have initialized
    StartCoroutine(SnapCinemachineAfterSceneLoad());

    _pendingTargetScene = null;
    _pendingTargetPortalId = null;
    _isTransitioning = false;
    _ignoreUntilTime = Time.unscaledTime + portalTriggerIgnoreSeconds;

    if (useFade) yield return FadeTo(0f, fadeInDuration);
    SetLoadingVisible(false);
    _isAsyncLoading = false;
  }

  // Wait a couple of frames after scene activation so newly spawned Cinemachine cameras are present
  private System.Collections.IEnumerator SnapCinemachineAfterSceneLoad()
  {
    // Two frames gives time for camera brains & virtual cameras to finish Awake/Start
    yield return null;
    yield return null;
    yield return SnapCinemachineOneFrame();
  }
}
