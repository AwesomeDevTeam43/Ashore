using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Self-contained Pause Menu that creates its own UI and works across all scenes.
/// Uses PauseMenuUIFactory, AudioSettingsController, and ControlsRebindController for separation of concerns.
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
    private InputAction leftTabAction;
    private InputAction rightTabAction;
    private InputActionAsset inputActions;
    
    // Controllers
    private AudioSettingsController audioController;
    private ControlsRebindController controlsController;
    
    // UI References
    private Canvas pauseCanvas;
    private GameObject pausePanel;
    private GameObject optionsPanel;
    private GameObject audioTabContent;
    private GameObject controlsTabContent;
    private Button audioTabButton;
    private Button controlsTabButton;
    private Button optionsBackButton;
    
    // State
    private bool isPaused = false;
    private bool isAudioTabActive = true;
    private List<Button> pauseMenuButtons = new List<Button>();
    
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
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromActions();
        controlsController?.Dispose();
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
            
            SetupMenuAction();
            SetupTabActions();
            
            // Initialize controllers
            controlsController = new ControlsRebindController(inputActions);
            controlsController.LoadInputRebinds();
            audioController = new AudioSettingsController();
        }
        else
        {
            Debug.LogWarning("[GamePauseManager] Could not find Player_InputHandler or InputActionAsset!");
            audioController = new AudioSettingsController();
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

    private void SetupTabActions()
    {
        leftTabAction = inputActions.FindAction("UI/LeftTab");
        rightTabAction = inputActions.FindAction("UI/RightTab");
        
        if (leftTabAction != null)
        {
            leftTabAction.performed += OnLeftTabPerformed;
            Debug.Log("[GamePauseManager] LeftTab (L1) action found");
        }
        if (rightTabAction != null)
        {
            rightTabAction.performed += OnRightTabPerformed;
            Debug.Log("[GamePauseManager] RightTab (R1) action found");
        }
    }

    private void UnsubscribeFromActions()
    {
        if (menuAction != null) menuAction.performed -= OnMenuActionPerformed;
        if (leftTabAction != null) leftTabAction.performed -= OnLeftTabPerformed;
        if (rightTabAction != null) rightTabAction.performed -= OnRightTabPerformed;
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
        TogglePause();
    }

    private void OnLeftTabPerformed(InputAction.CallbackContext context)
    {
        if (!isPaused || optionsPanel == null || !optionsPanel.activeSelf) return;
        if (!isAudioTabActive)
        {
            SwitchToTab(true);
            SelectFirstButtonInCurrentTab();
        }
    }

    private void OnRightTabPerformed(InputAction.CallbackContext context)
    {
        if (!isPaused || optionsPanel == null || !optionsPanel.activeSelf) return;
        if (isAudioTabActive)
        {
            SwitchToTab(false);
            SelectFirstButtonInCurrentTab();
        }
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
            if (optionsPanel != null) optionsPanel.SetActive(false);
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

        pausePanel = CreateMainPausePanel(canvasGO.transform);
        optionsPanel = CreateOptionsPanel(canvasGO.transform);
        
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);
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

        return panel;
    }

    private GameObject CreateOptionsPanel(Transform parent)
    {
        GameObject panel = PauseMenuUIFactory.CreatePanel(parent, "OptionsPanel", overlayColor);

        // Title
        PauseMenuUIFactory.CreateTextElement(panel.transform, "OPTIONS", PauseMenuUIFactory.TitleFontSize, 
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.95f), new Vector2(600, 0));

        // Tab Container
        GameObject tabContainer = PauseMenuUIFactory.CreateContainer(panel.transform, "TabContainer",
            new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.82f));
        
        audioTabButton = PauseMenuUIFactory.CreateTabButton(tabContainer.transform, "Audio", 
            new Vector2(0f, 0f), new Vector2(0.48f, 1f), () => SwitchToTab(true));
        
        controlsTabButton = PauseMenuUIFactory.CreateTabButton(tabContainer.transform, "Controls", 
            new Vector2(0.52f, 0f), new Vector2(1f, 1f), () => SwitchToTab(false));

        // Content Container
        GameObject contentContainer = PauseMenuUIFactory.CreateContainer(panel.transform, "ContentContainer",
            new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.73f), new Color(0.1f, 0.1f, 0.15f, 0.9f));

        // Tab Contents
        audioTabContent = audioController.CreateAudioTabContent(contentContainer.transform);
        controlsTabContent = controlsController?.CreateControlsTabContent(contentContainer.transform, ResetAllInputBindings) 
            ?? CreateEmptyContent(contentContainer.transform, "Controls not available");

        // Back Button
        optionsBackButton = PauseMenuUIFactory.CreateButton(panel.transform, "Back", 
            new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.16f), HideOptions);

        // Setup navigation
        audioController.SetupOptionsNavigation(optionsBackButton);
        
        // Start with Audio tab
        SwitchToTab(true);

        return panel;
    }

    private GameObject CreateEmptyContent(Transform parent, string message)
    {
        GameObject content = new GameObject("EmptyContent");
        content.transform.SetParent(parent, false);
        
        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        PauseMenuUIFactory.CreateTextElement(content.transform, message, 32,
            new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), Vector2.zero);
        
        return content;
    }

    #endregion

    #region Tab Management

    private void SwitchToTab(bool isAudioTab)
    {
        isAudioTabActive = isAudioTab;
        
        audioTabContent.SetActive(isAudioTab);
        controlsTabContent.SetActive(!isAudioTab);
        
        // Update tab button colors
        Color activeColor = PauseMenuUIFactory.ButtonColor;
        Color inactiveColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        
        audioTabButton.GetComponent<Image>().color = isAudioTab ? activeColor : inactiveColor;
        controlsTabButton.GetComponent<Image>().color = isAudioTab ? inactiveColor : activeColor;
        
        RebuildNavScope(optionsPanel);
    }

    private void SelectFirstButtonInCurrentTab()
    {
        if (optionsPanel == null || !optionsPanel.activeSelf) return;
        
        if (isAudioTabActive)
        {
            if (audioController.MasterVolumeSlider != null)
            {
                EventSystem.current.SetSelectedGameObject(audioController.MasterVolumeSlider.gameObject);
            }
        }
        else
        {
            var buttons = controlsController?.ControlsTabButtons;
            if (buttons != null && buttons.Count > 0 && buttons[0] != null)
            {
                EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
            }
        }
    }

    #endregion

    #region Pause Control

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        leftTabAction?.Enable();
        rightTabAction?.Enable();
        
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
        optionsPanel.SetActive(false);
        
        leftTabAction?.Disable();
        rightTabAction?.Disable();
        
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void ShowOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
        
        RebuildNavScope(optionsPanel);
        SelectFirstButtonInCurrentTab();
    }

    public void HideOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
        
        RebuildNavScope(pausePanel);
        
        if (pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null)
        {
            EventSystem.current?.SetSelectedGameObject(pauseMenuButtons[0].gameObject);
        }
    }

    #endregion

    #region Navigation Helpers

    private void RebuildNavScope(GameObject panel)
    {
        if (panel == null) return;
        var scope = panel.GetComponent<UINavScope>();
        scope?.Rebuild();
    }

    #endregion

    #region Menu Actions

    private void ResetAllInputBindings()
    {
        controlsController?.ResetAllInputBindings(optionsPanel.transform, RecreateControlsTab);
    }

    private GameObject RecreateControlsTab(Transform optionsPanelTransform)
    {
        if (controlsTabContent != null)
        {
            Destroy(controlsTabContent);
            
            Transform contentContainer = optionsPanelTransform.Find("ContentContainer");
            if (contentContainer != null)
            {
                controlsTabContent = controlsController.CreateControlsTabContent(
                    contentContainer, ResetAllInputBindings);
                controlsTabContent.SetActive(true);
                return controlsTabContent;
            }
        }
        return null;
    }

    public void QuitToMainMenu()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);
        
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
