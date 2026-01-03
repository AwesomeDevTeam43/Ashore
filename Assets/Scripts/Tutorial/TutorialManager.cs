using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("Steps")]
    public TutorialStep[] steps;
    [Tooltip("Index of the load scene step to skip to. Set in Inspector.")]
    public int loadSceneStepIndex = -1;

    [Header("UI Overlay")]
    public TutorialOverlay overlayPrefab;
    private TutorialOverlay overlayInstance;
    [Header("Bindings")]
    [SerializeField] private InputBindingDisplayResolver bindingResolver;

    [Header("Player")]
    public GameObject player;

    private int currentIndex = -1;
    private Coroutine freezeRoutine;
    private float prevTimeScale = 1f;
    private string currentInstructionRaw = string.Empty;
    private bool isShowingDialogueBox = false;

    [Header("Overlay Dim Settings")]
    [Tooltip("Default dim alpha when a step requests dimming.")]
    public float defaultDimAlpha = 0.75f;
    [Tooltip("Dim alpha to use when inventory UI is open but the current step is not inventory-focused.")]
    public float inventoryOpenDimAlpha = 0.08f;

    public TutorialOverlay Overlay => overlayInstance;

    private void Awake()
    {
        ResolveBindingResolver();
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p;
        }

        if (overlayInstance == null)
        {
            if (overlayPrefab == null)
            {
                // Create a minimal overlay at runtime if no prefab assigned
                var go = new GameObject("TutorialOverlay");
                overlayInstance = go.AddComponent<TutorialOverlay>();
                overlayInstance.BuildRuntimeCanvas();
            }
            else
            {
                overlayInstance = Instantiate(overlayPrefab);
            }
        }
    }

    private void OnEnable()
    {
        SubscribeBindingResolver();
        Advance();
    }

    private void OnDisable()
    {
        UnsubscribeBindingResolver();
    }

    private void Update()
    {
        // Skip to load scene step on Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SkipToLoadScene();
        }

        if (currentIndex < 0 || currentIndex >= (steps?.Length ?? 0)) return;
        var s = steps[currentIndex];
        if (s != null && s.IsComplete())
        {
            s.End();
            Advance();
            return;
        }

        // Adjust dim level depending on inventory visibility and whether this step focuses inventory
        if (s != null && s.dimScreen && overlayInstance != null)
        {
            float alpha = defaultDimAlpha;
            bool invOpen = IsInventoryOpen();
            if (invOpen && !s.requiresInventoryFocus)
            {
                alpha = inventoryOpenDimAlpha;
            }
            overlayInstance.SetDimLevel(alpha);
        }
    }

    private void Advance()
    {
        // Ensure game isn't left frozen from a prior step
        CancelFreezeIfAny();
        currentIndex++;
        if (currentIndex >= (steps?.Length ?? 0))
        {
            // Done. Hide overlay and enable player.
            overlayInstance?.SetDim(false);
            overlayInstance?.SetMessage("");
            currentInstructionRaw = string.Empty;
            SetPlayerMovementEnabled(true);
            CancelFreezeIfAny();
            enabled = false;
            return;
        }
        var s = steps[currentIndex];
        if (s != null)
        {
            s.Begin(this);
        }
    }

    private void SkipToLoadScene()
    {
        if (loadSceneStepIndex >= 0 && loadSceneStepIndex < (steps?.Length ?? 0))
        {
            // End current step
            if (currentIndex >= 0 && currentIndex < (steps?.Length ?? 0))
            {
                var s = steps[currentIndex];
                if (s != null) s.End();
            }
            // Set to the step before the load scene step, then Advance will go to it
            currentIndex = loadSceneStepIndex - 1;
            Advance();
        }
    }

    private void ResolveBindingResolver()
    {
        if (bindingResolver == null)
        {
            bindingResolver = InputBindingDisplayResolver.Instance;
        }
        if (bindingResolver == null)
        {
            bindingResolver = FindFirstObjectByType<InputBindingDisplayResolver>(FindObjectsInactive.Include);
        }
    }

    private void SubscribeBindingResolver()
    {
        ResolveBindingResolver();
        if (bindingResolver != null)
        {
            bindingResolver.OnBindingsChanged += HandleBindingsChanged;
        }
    }

    private void UnsubscribeBindingResolver()
    {
        if (bindingResolver != null)
        {
            bindingResolver.OnBindingsChanged -= HandleBindingsChanged;
        }
    }

    private void HandleBindingsChanged()
    {
        RefreshOverlayInstruction();
    }

    public void ApplyInstructionText(string rawText)
    {
        currentInstructionRaw = rawText ?? string.Empty;
        RefreshOverlayInstruction();
    }

    private void RefreshOverlayInstruction()
    {
        if (overlayInstance == null) return;
        string msg = currentInstructionRaw ?? string.Empty;
        if (bindingResolver != null)
        {
            msg = bindingResolver.FormatText(msg);
        }
        overlayInstance.SetMessage(msg);
    }

    /// <summary>
    /// Shows tutorial text using the RPGMaker-style dialogue box.
    /// The dialogue box displays at the bottom of the screen with typewriter effect.
    /// </summary>
    public void ShowDialogueText(string title, string body)
    {
        // Format the text with current bindings
        string formattedBody = body ?? string.Empty;
        if (bindingResolver != null)
        {
            formattedBody = bindingResolver.FormatText(formattedBody);
        }

        // Get or create the dialogue manager
        var dialogueManager = LoreDialogueManager.Instance ?? LoreDialogueManager.EnsureExists();
        
        // Show the text (this will lock player input automatically)
        dialogueManager.ShowText(title, formattedBody, null, null);
        isShowingDialogueBox = true;
        
        Debug.Log($"[TutorialManager] ShowDialogueText: '{title}' - {formattedBody.Length} chars");
    }

    /// <summary>
    /// Hides the RPGMaker-style dialogue box if it's currently showing.
    /// </summary>
    public void HideDialogueText()
    {
        if (!isShowingDialogueBox) return;
        
        var dialogueManager = LoreDialogueManager.Instance;
        if (dialogueManager != null && dialogueManager.IsDisplaying)
        {
            dialogueManager.CloseDialogue();
        }
        isShowingDialogueBox = false;
        
        Debug.Log("[TutorialManager] HideDialogueText called");
    }

    public void SetPlayerMovementEnabled(bool enabledState)
    {
        if (player == null) return;
        var pm = player.GetComponent<Player_Movement>();
        if (pm != null) pm.enabled = enabledState;

        var rb2D = player.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            if (enabledState)
            {
                rb2D.WakeUp();
            }
            else
            {
                rb2D.linearVelocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.Sleep();
            }
        }

        var rb3D = player.GetComponent<Rigidbody>();
        if (rb3D != null)
        {
            if (enabledState)
            {
                rb3D.WakeUp();
            }
            else
            {
                rb3D.linearVelocity = Vector3.zero;
                rb3D.angularVelocity = Vector3.zero;
                rb3D.Sleep();
            }
        }
    }

    // Freeze/unfreeze game time with unscaled timer
    public void FreezeGameForSeconds(float seconds)
    {
        CancelFreezeIfAny();
        freezeRoutine = StartCoroutine(FreezeRoutine(seconds));
    }

    private System.Collections.IEnumerator FreezeRoutine(float seconds)
    {
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new UnityEngine.WaitForSecondsRealtime(Mathf.Max(0f, seconds));
        RestoreTimeScaleIfAllowed();
        freezeRoutine = null;
    }

    private void CancelFreezeIfAny()
    {
        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
            freezeRoutine = null;
        }
        // Ensure timescale restored if left frozen
        RestoreTimeScaleIfAllowed();
    }

    // Freeze game until player interacts or moves after a short realtime cooldown
    public void FreezeUntilInput(float cooldown)
    {
        CancelFreezeIfAny();
        freezeRoutine = StartCoroutine(FreezeUntilInputRoutine(Mathf.Max(0f, cooldown)));
    }

    private System.Collections.IEnumerator FreezeUntilInputRoutine(float cooldown)
    {
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (cooldown > 0f)
            yield return new WaitForSecondsRealtime(cooldown);

        // Wait until any interact or movement input is detected (unscaled)
        while (!DetectUnfreezeInput())
        {
            yield return null; // next frame (realtime)
        }

        RestoreTimeScaleIfAllowed();
        freezeRoutine = null;
    }

    private bool DetectUnfreezeInput()
    {
        // Prefer Player_InputHandler if available
        Player_InputHandler ih = null;
        if (player != null) ih = player.GetComponent<Player_InputHandler>();
        if (ih != null)
        {
            if (ih.InteractActionTriggered) return true;
            var mv = ih.MovementInput; if (mv.sqrMagnitude > 0.01f) return true;
        }
        // Fallback to Input System polling
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) return true;
            if (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed) return true;
            if (kb.upArrowKey.isPressed || kb.leftArrowKey.isPressed || kb.downArrowKey.isPressed || kb.rightArrowKey.isPressed) return true;
        }
        var gp = UnityEngine.InputSystem.Gamepad.current;
        if (gp != null)
        {
            if (gp.buttonWest.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame) return true;
            if (gp.leftStick.ReadValue().sqrMagnitude > 0.05f) return true;
            if (gp.dpad.up.isPressed || gp.dpad.left.isPressed || gp.dpad.down.isPressed || gp.dpad.right.isPressed) return true;
        }
        return false;
    }

    private void RestoreTimeScaleIfAllowed(bool respectInventoryPause = true)
    {
        if (!Mathf.Approximately(Time.timeScale, 0f)) return;
        if (respectInventoryPause && IsInventoryOpen()) return;
        if (Mathf.Approximately(prevTimeScale, 0f)) return;
        Time.timeScale = prevTimeScale;
    }

    private bool IsInventoryOpen()
    {
        var menus = FindObjectsByType<MenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var menu in menus)
        {
            if (menu != null && menu.isActiveAndEnabled && menu.IsMenuOpen)
            {
                return true;
            }
        }

        // Fallback: consider inventory open if any InventoryPage is active in hierarchy
        var pages = FindObjectsByType<InventoryPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in pages)
        {
            if (p != null && p.isActiveAndEnabled && p.gameObject.activeInHierarchy)
                return true;
        }
        return false;
    }
}
