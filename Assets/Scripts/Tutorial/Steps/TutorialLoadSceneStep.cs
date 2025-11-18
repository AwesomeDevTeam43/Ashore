using System.Collections;
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

    [Tooltip("Delay (realtime seconds) before triggering the scene load so players can read the instructions.")]
    public float delayBeforeLoad = 1f;

    [Tooltip("Optional spawn point ID for the destination scene (SceneSpawnPoint or portal ID).")]
    public string spawnPointId;

    private bool _complete;
    private Coroutine _loadRoutine;

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
        if (!string.IsNullOrEmpty(spawnPointId))
        {
            PlayerPersistence.SetNextSpawn(spawnPointId);
        }

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
        }
        _complete = false;
        _loadRoutine = StartCoroutine(LoadSceneAfterDelay());
    }

    public override bool IsComplete()
    {
        // Remain active until the load coroutine fires to preserve instructions/freeze.
        return _complete;
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        if (delayBeforeLoad > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeLoad);
        }

        GameState.Instance?.ClearAll();
        var overlay = GlobalLoadingOverlay.Instance;
        if (overlay == null)
        {
            Debug.LogError("TutorialLoadSceneStep: GlobalLoadingOverlay instance missing; cannot load scene.");
            yield break;
        }
        overlay.LoadSceneAsync(overlay, sceneName, Mathf.Max(0f, minShowSeconds), loadingText);
        _complete = true;
    }

    private void OnDisable()
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }
        _complete = false;
    }
}