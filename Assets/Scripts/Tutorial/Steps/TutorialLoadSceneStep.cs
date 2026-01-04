using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Auto-Save After Load")]
    [Tooltip("If true, automatically save the game after the new scene finishes loading (so the player won't have to redo the tutorial).")]
    public bool autoSaveAfterLoad = true;

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
        
        // Set up auto-save after the scene loads (isolated runner so it only applies to this tutorial load)
        if (autoSaveAfterLoad)
        {
            var runner = new GameObject("AutoSaveRunner").AddComponent<AutoSaveRunner>();
            runner.targetScene = sceneName;
            DontDestroyOnLoad(runner.gameObject);
        }
        
        var overlay = GlobalLoadingOverlay.Instance;
        if (overlay == null)
        {
            Debug.LogError("TutorialLoadSceneStep: GlobalLoadingOverlay instance missing; cannot load scene.");
            yield break;
        }
        overlay.LoadSceneAsync(overlay, sceneName, Mathf.Max(0f, minShowSeconds), loadingText);
        _complete = true;
    }

    private class AutoSaveRunner : MonoBehaviour
    {
        public string targetScene;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != targetScene) return;
            // Unsubscribe and perform the delayed save
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            StartCoroutine(DelayedAutoSave());
        }

        public IEnumerator DelayedAutoSave()
        {
            // Wait a couple frames for all scene objects to initialize
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var pc = player.GetComponent<Player_Controller>();
                var xp = player.GetComponent<XP_System>();
                var health = player.GetComponent<Player_Health>();

                if (pc != null && xp != null && health != null)
                {
                    int slot = SaveSlotTracker.CurrentSlot;
                    SaveSystem.SavePlayer(pc, xp, health, Inventory.instance, slot);
                    Debug.Log($"[TutorialLoadSceneStep] Auto-saved game to slot {slot} after tutorial completion.");
                }
                else
                {
                    Debug.LogWarning("[TutorialLoadSceneStep] Could not auto-save: missing player components.");
                }
            }
            else
            {
                Debug.LogWarning("[TutorialLoadSceneStep] Could not auto-save: player not found.");
            }

            Destroy(gameObject);
        }
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