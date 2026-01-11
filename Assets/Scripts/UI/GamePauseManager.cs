using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;

/// <summary>
/// Self-contained Pause Menu that creates its own UI and works across all scenes.
/// Uses PauseMenuUIFactory for UI creation, and delegates Options to OptionsMenuController.
/// </summary>
public class GamePauseManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string menuActionName = "Menu";
    
    [Header("Appearance")]
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.85f);
    
    // Input System references
    private InputAction menuAction;
    private InputActionAsset inputActions;
    private PlayerInput playerInput;
    
    // UI References
    private Canvas pauseCanvas;
    private GameObject pausePanel;
    private EventSystem es;
    
    // State
    private bool isPaused = false;
    private List<Button> pauseMenuButtons = new List<Button>();
    
    // Navigation cooldown
    private float _navCooldown = 0f;
    private const float NAV_REPEAT_DELAY = 0.15f;
    
    // State tracking for submit buttons (needed when Time.timeScale = 0)
    private bool _prevEnterPressed = false;
    private bool _prevNumpadEnterPressed = false;
    private bool _prevSpacePressed = false;
    private bool _prevGamepadSouthPressed = false;
    
    // Singleton
    private static GamePauseManager instance;
    public static GamePauseManager Instance => instance;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            EnsureEventSystemExists();
            FindInputActionAsset();
            CreatePauseMenuUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (isPaused)
        {
            UIInputMode.DetectThisFrame();
            HandleManualNavigation();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromActions();
        if (instance == this) instance = null;
    }

    #region Initialization

    private void FindInputActionAsset()
    {
        var inputHandler = FindAnyObjectByType<Player_InputHandler>();
        if (inputHandler != null && inputHandler.ControlsAsset != null)
        {
            inputActions = inputHandler.ControlsAsset;
            Debug.Log("[GamePauseManager] Found InputActionAsset from Player_InputHandler");
            
            // Also get PlayerInput component for action map switching
            playerInput = inputHandler.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                Debug.Log("[GamePauseManager] Found PlayerInput component");
            }
            
            SetupMenuAction();
        }
        else
        {
            Debug.LogWarning("[GamePauseManager] Could not find Player_InputHandler or InputActionAsset!");
        }
    }

    private void SetupMenuAction()
    {
        menuAction = inputActions.FindAction(menuActionName);
        if (menuAction != null)
        {
            menuAction.performed += OnMenuActionPerformed;
            menuAction.Enable();
            Debug.Log("[GamePauseManager] Menu action found and enabled (Escape + Gamepad Start)");
        }
        else
        {
            Debug.LogWarning($"[GamePauseManager] Could not find '{menuActionName}' action!");
        }
    }

    private void UnsubscribeFromActions()
    {
        if (menuAction != null) menuAction.performed -= OnMenuActionPerformed;
    }

    private void EnsureEventSystemExists()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            DontDestroyOnLoad(eventSystem);
        }
    }

    #endregion

    #region Input Callbacks

    private void OnMenuActionPerformed(InputAction.CallbackContext context)
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;
        
        // Don't toggle pause if inventory is open
        if (MenuController.InventoryOpen) return;
        
        TogglePause();
    }

    #endregion

    #region Scene Management

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            
            // Also hide shared options menu
            if (OptionsMenuController.Instance != null)
                OptionsMenuController.Instance.Hide();
        }
        EnsureEventSystemExists();
    }

    #endregion

    #region UI Creation

    private void CreatePauseMenuUI()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("PauseCanvas");
        canvasGO.transform.SetParent(transform);
        
        pauseCanvas = canvasGO.AddComponent<Canvas>();
        pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = 999;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();

        // Only create pause panel - options is handled by OptionsMenuController
        pausePanel = CreateMainPausePanel(canvasGO.transform);
        pausePanel.SetActive(false);
    }

    private GameObject CreateMainPausePanel(Transform parent)
    {
        pauseMenuButtons.Clear();
        
        GameObject panel = PauseMenuUIFactory.CreatePanel(parent, "PausePanel", overlayColor);

        // Title
        PauseMenuUIFactory.CreateTextElement(panel.transform, "PAUSED", PauseMenuUIFactory.TitleFontSize, 
            new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.88f), new Vector2(600, 0));

        // Buttons
        pauseMenuButtons.Add(PauseMenuUIFactory.CreateButton(panel.transform, "Resume", 
            new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.65f), Resume));
        
        pauseMenuButtons.Add(PauseMenuUIFactory.CreateButton(panel.transform, "Options", 
            new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.52f), ShowOptions));
        
        pauseMenuButtons.Add(PauseMenuUIFactory.CreateButton(panel.transform, "Quit to Menu", 
            new Vector2(0.35f, 0.29f), new Vector2(0.65f, 0.39f), QuitToMainMenu));
        
        // Setup explicit vertical navigation to ensure middle button is reachable
        SetupButtonNavigation();

        return panel;
    }

    #endregion

    #region Pause Control

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }
    
    /// <summary>
    /// Sets up explicit vertical navigation between pause menu buttons.
    /// This ensures the middle button is always reachable.
    /// Buttons are ordered top-to-bottom: Resume(0), Options(1), Quit(2)
    /// </summary>
    private void SetupButtonNavigation()
    {
        if (pauseMenuButtons.Count < 2) return;
        
        for (int i = 0; i < pauseMenuButtons.Count; i++)
        {
            if (pauseMenuButtons[i] == null) continue;
            
            Navigation nav = pauseMenuButtons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            
            // Buttons are visually top-to-bottom with index 0 at top
            // Up = go to previous index (higher on screen)
            // Down = go to next index (lower on screen)
            nav.selectOnUp = (i > 0) ? pauseMenuButtons[i - 1] : pauseMenuButtons[pauseMenuButtons.Count - 1];
            nav.selectOnDown = (i < pauseMenuButtons.Count - 1) ? pauseMenuButtons[i + 1] : pauseMenuButtons[0];
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            
            pauseMenuButtons[i].navigation = nav;
            
            Debug.Log($"[GamePauseManager] Button {i} ({pauseMenuButtons[i].name}): Up={nav.selectOnUp?.name}, Down={nav.selectOnDown?.name}");
        }
    }
    

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        
        // Hide options menu if it's open
        if (OptionsMenuController.Instance != null)
            OptionsMenuController.Instance.Hide();
        
        // Let UIInputModeManager handle cursor visibility based on current input mode
        // Only set it if in pointer mode
        if (UIInputMode.Current == UIInputMode.Mode.Pointer)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        // Switch to UI input map for proper controller/keyboard navigation
        EnforceInputMap(true);
        EnsureUIModuleNavigationEnabled();
        
        // Ensure EventSystem reference
        // Disable sendNavigationEvents to prevent double-navigation from both EventSystem and HandleManualNavigation
        es = EventSystem.current;
        if (es != null) es.sendNavigationEvents = false;
        
        RebuildNavScope(pausePanel);
        
        if (pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null)
        {
            EventSystem.current?.SetSelectedGameObject(pauseMenuButtons[0].gameObject);
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        
        // Also hide options menu
        if (OptionsMenuController.Instance != null)
            OptionsMenuController.Instance.Hide();
        
        // Switch back to Player input map
        EnforceInputMap(false);
        
        // Re-enable EventSystem navigation for normal gameplay
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = true;
        
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void ShowOptions()
    {
        pausePanel.SetActive(false);
        
        // Use shared OptionsMenuController
        var optionsMenu = OptionsMenuController.EnsureExists();
        optionsMenu.Show(OnOptionsMenuClosed);
    }
    
    private void OnOptionsMenuClosed()
    {
        // When options closes, show pause panel again
        if (isPaused)
        {
            pausePanel.SetActive(true);
            RebuildNavScope(pausePanel);
            
            if (pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null)
            {
                EventSystem.current?.SetSelectedGameObject(pauseMenuButtons[0].gameObject);
            }
        }
    }

    #endregion

    #region Navigation Helpers

    private void RebuildNavScope(GameObject panel)
    {
        // No custom navigation scope management - Unity handles it
    }
    
    /// <summary>
    /// Switches between UI and Player input action maps for proper controller/keyboard navigation.
    /// </summary>
    private void EnforceInputMap(bool useUIMap)
    {
        // Re-find PlayerInput if needed (e.g., after scene reload)
        if (playerInput == null)
        {
            var inputHandler = FindAnyObjectByType<Player_InputHandler>();
            if (inputHandler != null)
            {
                playerInput = inputHandler.GetComponent<PlayerInput>();
            }
        }
        
        if (playerInput == null)
        {
            Debug.LogWarning("[GamePauseManager] EnforceInputMap: playerInput is null");
            return;
        }
        
        if (useUIMap)
        {
            Debug.Log("[GamePauseManager] EnforceInputMap: Switching to UI map");
            try { playerInput.actions.FindActionMap("Player")?.Disable(); } catch { }
            try { playerInput.actions.FindActionMap("UI")?.Enable(); } catch { }
            try { playerInput.SwitchCurrentActionMap("UI"); } catch { }
        }
        else
        {
            Debug.Log("[GamePauseManager] EnforceInputMap: Switching to Player map");
            try { playerInput.actions.FindActionMap("UI")?.Disable(); } catch { }
            try { playerInput.actions.FindActionMap("Player")?.Enable(); } catch { }
            try { playerInput.SwitchCurrentActionMap("Player"); } catch { }
        }
    }
    
    /// <summary>
    /// Ensures the InputSystemUIInputModule's navigation actions (move, submit, cancel) are enabled.
    /// </summary>
    private void EnsureUIModuleNavigationEnabled()
    {
        var uiModule = FindAnyObjectByType<InputSystemUIInputModule>();
        if (uiModule == null)
        {
            Debug.LogWarning("[GamePauseManager] EnsureUIModuleNavigationEnabled: No InputSystemUIInputModule found");
            return;
        }
        
        try
        {
            // Enable move action for navigation
            var moveAction = uiModule.move?.action;
            if (moveAction != null && !moveAction.enabled)
            {
                moveAction.Enable();
                Debug.Log("[GamePauseManager] Enabled UI move action");
            }
            
            // Enable submit action
            var submitAction = uiModule.submit?.action;
            if (submitAction != null && !submitAction.enabled)
            {
                submitAction.Enable();
                Debug.Log("[GamePauseManager] Enabled UI submit action");
            }
            
            // Enable cancel action
            var cancelAction = uiModule.cancel?.action;
            if (cancelAction != null && !cancelAction.enabled)
            {
                cancelAction.Enable();
                Debug.Log("[GamePauseManager] Enabled UI cancel action");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GamePauseManager] EnsureUIModuleNavigationEnabled failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Manual navigation handling as fallback when InputSystemUIInputModule doesn't work.
    /// Reads keyboard/gamepad input directly and moves selection accordingly.
    /// </summary>
    private void HandleManualNavigation()
    {
        // Respect cooldown (use unscaled time since game is paused)
        if (_navCooldown > 0f)
        {
            _navCooldown -= Time.unscaledDeltaTime;
            return;
        }

        if (es == null) es = EventSystem.current;
        if (es == null) return;

        var current = es.currentSelectedGameObject;
        
        // If nothing is selected, select the first button
        if (current == null)
        {
            if (pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null)
            {
                es.SetSelectedGameObject(pauseMenuButtons[0].gameObject);
            }
            return;
        }

        var sel = current.GetComponent<Selectable>();
        if (sel == null) return;

        // Read input from keyboard and gamepad
        Vector2 input = Vector2.zero;
        bool submit = false;
        
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y = 1;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y = -1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x = -1;
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x = 1;
            
            // Use state tracking for submit (wasPressedThisFrame doesn't work when Time.timeScale = 0)
            bool enterPressed = kb.enterKey.isPressed;
            bool numpadEnterPressed = kb.numpadEnterKey.isPressed;
            bool spacePressed = kb.spaceKey.isPressed;
            if ((enterPressed && !_prevEnterPressed) || (numpadEnterPressed && !_prevNumpadEnterPressed) || (spacePressed && !_prevSpacePressed))
                submit = true;
            _prevEnterPressed = enterPressed;
            _prevNumpadEnterPressed = numpadEnterPressed;
            _prevSpacePressed = spacePressed;
        }

        var gp = Gamepad.current;
        if (gp != null)
        {
            if (input == Vector2.zero)
            {
                var stick = gp.leftStick.ReadValue();
                var dpad = gp.dpad.ReadValue();
                if (Mathf.Abs(stick.x) > 0.5f || Mathf.Abs(dpad.x) > 0.5f)
                    input.x = stick.x > 0.5f || dpad.x > 0.5f ? 1 : -1;
                if (Mathf.Abs(stick.y) > 0.5f || Mathf.Abs(dpad.y) > 0.5f)
                    input.y = stick.y > 0.5f || dpad.y > 0.5f ? 1 : -1;
            }
            
            // Use state tracking for gamepad submit
            bool gamepadSouthPressed = gp.buttonSouth.isPressed;
            if (gamepadSouthPressed && !_prevGamepadSouthPressed)
                submit = true;
            _prevGamepadSouthPressed = gamepadSouthPressed;
        }

        // Handle submit
        if (submit && sel is Button btn)
        {
            btn.onClick.Invoke();
            _navCooldown = NAV_REPEAT_DELAY;
            return;
        }

        if (input == Vector2.zero) return;

        Selectable next = null;
        if (input.y > 0) next = sel.FindSelectableOnUp();
        else if (input.y < 0) next = sel.FindSelectableOnDown();
        else if (input.x < 0) next = sel.FindSelectableOnLeft();
        else if (input.x > 0) next = sel.FindSelectableOnRight();

        if (next != null && next.gameObject != current)
        {
            Debug.Log($"[GamePauseManager] Manual nav: {current.name} -> {next.gameObject.name}");
            es.SetSelectedGameObject(next.gameObject);
            _navCooldown = NAV_REPEAT_DELAY;
        }
    }

    #endregion

    #region Menu Actions

    public void QuitToMainMenu()
    {
        pausePanel.SetActive(false);
        
        // Also hide options menu
        if (OptionsMenuController.Instance != null)
            OptionsMenuController.Instance.Hide();
        
        // Switch back to Player input map before loading
        EnforceInputMap(false);
        
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    #endregion
}

