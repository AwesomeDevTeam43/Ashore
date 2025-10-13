using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach this to any Canvas (or the Inventoy_UI GameObject) to get clickable diagnostics.
// Left-click in Game view to print the list of UI elements under the cursor and where they come from.
public class UIRaycastDebugger : MonoBehaviour
{
    GraphicRaycaster raycaster;
    EventSystem es;

    void Awake()
    {
        es = EventSystem.current;
        raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            // Find any GraphicRaycaster in the scene (non-allocating alternative to deprecated API)
            raycaster = FindAnyObjectByType<GraphicRaycaster>();
        }

        if (es == null) Debug.LogWarning("UIRaycastDebugger: No EventSystem in scene. UI will not receive pointer events.");
        if (raycaster == null) Debug.LogWarning("UIRaycastDebugger: No GraphicRaycaster found on any Canvas. UI raycasts will fail.");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;
            Debug.Log($"UIRaycastDebugger: Mouse click at {pos}");

            if (es == null)
            {
                Debug.Log(" - EventSystem.current is null");
                return;
            }

            // Raycast against all GraphicRaycasters found in the scene (covers multiple Canvases and render modes)
            var raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            bool anyFound = false;
            int totalResults = 0;
            foreach (var gr in raycasters)
            {
                Canvas canvas = gr.GetComponent<Canvas>();
                Camera cam = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    cam = canvas.worldCamera;
                }

                PointerEventData ped = new PointerEventData(es);
                ped.position = pos;

                List<RaycastResult> results = new List<RaycastResult>();
                gr.Raycast(ped, results);

                if (results.Count == 0) continue;

                anyFound = true;
                Debug.Log($" - Canvas '{(canvas != null ? canvas.name : gr.gameObject.name)}' (renderMode={canvas?.renderMode.ToString() ?? "n/a"}) found {results.Count} result(s):");
                int idx = 0;
                foreach (var r in results)
                {
                    string comp = "";
                    var g = r.gameObject;
                    var img = g.GetComponent<Image>();
                    var txt = g.GetComponent<Text>();
                    var tmp = g.GetComponent<TMPro.TextMeshProUGUI>();
                    comp += img != null ? "Image," : "";
                    comp += txt != null ? "Text," : "";
                    comp += tmp != null ? "TMP," : "";
                    comp += g.GetComponent<Button>() != null ? "Button," : "";
                    comp += g.GetComponent<CanvasGroup>() != null ? "CanvasGroup," : "";

                    Debug.Log($"   [{idx}] name='{g.name}' depth={r.depth} module={r.module?.GetType().Name} components=[{comp}] goActive={g.activeInHierarchy}");
                    idx++;
                    totalResults++;
                }
            }

            if (!anyFound)
            {
                Debug.Log(" - UI raycast found NO UI elements under pointer.");
            }
            else
            {
                Debug.Log($" - Total UI results across canvases: {totalResults}");
            }

            // If an Inventoy_UI exists in the scene, call its slot-hit diagnostic helper
            var ui = FindAnyObjectByType<Inventoy_UI>();
            if (ui != null)
            {
                ui.LogSlotHitInfo(pos);
            }
        }
    }
}
