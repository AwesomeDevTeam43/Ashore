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

        ResetPlayerStateForMainGame();
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

    private void ResetPlayerStateForMainGame()
    {
        Inventory.instance?.Clear();
        Time.timeScale = 1f;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (manager != null)
        {
            manager.SetPlayerMovementEnabled(true);
        }
        else
        {
            ForceEnablePlayerMovement(player);
        }

        var inputHandler = player.GetComponent<Player_InputHandler>();
        if (inputHandler != null)
        {
            inputHandler.EnablePlayerActions();
        }

        var controller = player.GetComponent<Player_Controller>();
        if (controller != null)
        {
            controller.ResetProgressToFreshStart();
        }
    }

    private void ForceEnablePlayerMovement(GameObject player)
    {
        var movement = player.GetComponent<Player_Movement>();
        if (movement != null)
        {
            movement.enabled = true;
        }

        var rb2D = player.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.WakeUp();
        }

        var rb3D = player.GetComponent<Rigidbody>();
        if (rb3D != null)
        {
            rb3D.linearVelocity = Vector3.zero;
            rb3D.angularVelocity = Vector3.zero;
            rb3D.WakeUp();
        }
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