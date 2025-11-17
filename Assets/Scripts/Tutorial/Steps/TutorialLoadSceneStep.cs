using UnityEngine;

public class TutorialLoadSceneStep : TutorialStep
{
    [Header("Target Scene")] 
    [Tooltip("Scene name to load when this step begins.")]
    public string sceneName;

    [Tooltip("Optional minimum seconds to show loading UI.")]
    public float minShowSeconds = 2f;

    [Tooltip("Loading message text.")]
    public string loadingText = "Loading...";

    private bool _complete;

    public override void Begin(TutorialManager mgr)
    {
        // Immediately start load; no freeze gating here
        base.Begin(mgr);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("TutorialLoadSceneStep: sceneName is empty; skipping.");
            _complete = true;
            return;
        }
        // Use global overlay so this is reusable outside portals
        var overlay = GlobalLoadingOverlay.Instance;
        overlay.LoadSceneAsync(this, sceneName, Mathf.Max(0f, minShowSeconds), loadingText);
        // Mark step complete right away; scene load takes over flow
        _complete = true;
    }

    public override bool IsComplete()
    {
        // This step completes immediately upon Begin(); manager will advance
        return _complete;
    }
}
