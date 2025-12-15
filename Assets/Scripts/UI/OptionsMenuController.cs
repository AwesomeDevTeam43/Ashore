using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Standalone Options Menu that can be used in Main Menu or Pause Menu.
/// Creates Audio and Controls tabs with full functionality.
/// </summary>
public class OptionsMenuController : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 0.85f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    
    [Header("Input (Optional - will auto-find if not set)")]
    [SerializeField] private InputActionAsset inputActionsAsset;
    
    // Controllers
    private AudioSettingsController audioController;
    private ControlsRebindController controlsController;
    
    // UI References
    private Canvas optionsCanvas;
    private GameObject optionsPanel;
    private GameObject audioTabContent;
    private GameObject controlsTabContent;
    private Button audioTabButton;
    private Button controlsTabButton;
    private Button backButton;
    
    // Input actions for tab switching
    private InputAction leftTabAction;
    private InputAction rightTabAction;
    private InputActionAsset inputActions;
    
    // State
    private bool isVisible = false;
    private bool isAudioTabActive = true;
    private Action onCloseCallback;
    
    // Singleton
    private static OptionsMenuController instance;
    public static OptionsMenuController Instance => instance;
    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            FindInputActionAsset();
            CreateOptionsUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromActions();
        controlsController?.Dispose();
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (isVisible)
        {
            UIInputMode.DetectThisFrame();
        }
    }

    #region Initialization

    private void FindInputActionAsset()
    {
        // 1. Use serialized asset if assigned
        if (inputActionsAsset != null)
        {
            inputActions = inputActionsAsset;
            Debug.Log("[OptionsMenuController] Using serialized InputActionAsset");
        }
        // 2. Try to get from Player_InputHandler
        else
        {
            var inputHandler = FindAnyObjectByType<Player_InputHandler>();
            if (inputHandler != null && inputHandler.ControlsAsset != null)
            {
                inputActions = inputHandler.ControlsAsset;
                Debug.Log("[OptionsMenuController] Found InputActionAsset from Player_InputHandler");
            }
            else
            {
                // 3. Load from Resources
                inputActions = Resources.Load<InputActionAsset>("Player_Inputs");
                
                if (inputActions != null)
                {
                    Debug.Log("[OptionsMenuController] Loaded InputActionAsset from Resources");
                }
                else
                {
                    Debug.LogWarning("[OptionsMenuController] Could not find InputActionAsset! Controls tab will be unavailable.");
                }
            }
        }
        
        if (inputActions != null)
        {
            // Find L1/R1 tab switching actions
            leftTabAction = inputActions.FindAction("UI/LeftTab");
            rightTabAction = inputActions.FindAction("UI/RightTab");
            
            if (leftTabAction != null)
                leftTabAction.performed += OnLeftTabPerformed;
            if (rightTabAction != null)
                rightTabAction.performed += OnRightTabPerformed;
            
            // Initialize controls rebinding
            controlsController = new ControlsRebindController(inputActions);
            controlsController.LoadInputRebinds();
        }
        
        audioController = new AudioSettingsController();
    }

    private void UnsubscribeFromActions()
    {
        if (leftTabAction != null)
            leftTabAction.performed -= OnLeftTabPerformed;
        if (rightTabAction != null)
            rightTabAction.performed -= OnRightTabPerformed;
    }

    private void OnLeftTabPerformed(InputAction.CallbackContext context)
    {
        if (!isVisible) return;
        if (!isAudioTabActive)
        {
            SwitchToTab(true);
            SelectFirstInCurrentTab();
        }
    }

    private void OnRightTabPerformed(InputAction.CallbackContext context)
    {
        if (!isVisible) return;
        if (isAudioTabActive)
        {
            SwitchToTab(false);
            SelectFirstInCurrentTab();
        }
    }

    #endregion

    #region UI Creation

    private void CreateOptionsUI()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("OptionsCanvas");
        canvasGO.transform.SetParent(transform);
        
        optionsCanvas = canvasGO.AddComponent<Canvas>();
        optionsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        optionsCanvas.sortingOrder = 1000; // Above pause menu
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Options Panel
        optionsPanel = PauseMenuUIFactory.CreatePanel(canvasGO.transform, "OptionsPanel", overlayColor);

        // Title
        PauseMenuUIFactory.CreateTextElement(optionsPanel.transform, "OPTIONS", 72, 
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.95f), new Vector2(600, 0));

        // Tab Container
        GameObject tabContainer = PauseMenuUIFactory.CreateContainer(optionsPanel.transform, "TabContainer",
            new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.82f));
        
        audioTabButton = PauseMenuUIFactory.CreateTabButton(tabContainer.transform, "Audio", 
            new Vector2(0f, 0f), new Vector2(0.48f, 1f), () => SwitchToTab(true));
        
        controlsTabButton = PauseMenuUIFactory.CreateTabButton(tabContainer.transform, "Controls", 
            new Vector2(0.52f, 0f), new Vector2(1f, 1f), () => SwitchToTab(false));

        // Content Container
        GameObject contentContainer = PauseMenuUIFactory.CreateContainer(optionsPanel.transform, "ContentContainer",
            new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.73f), new Color(0.1f, 0.1f, 0.15f, 0.9f));

        // Tab Contents
        audioTabContent = audioController.CreateAudioTabContent(contentContainer.transform);
        
        if (controlsController != null)
        {
            controlsTabContent = controlsController.CreateControlsTabContent(contentContainer.transform, ResetAllInputBindings);
        }
        else
        {
            controlsTabContent = CreateEmptyContent(contentContainer.transform, "Controls not available");
        }

        // Back Button
        backButton = PauseMenuUIFactory.CreateButton(optionsPanel.transform, "Back", 
            new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.16f), OnBackClicked);

        // Setup navigation
        audioController.SetupOptionsNavigation(backButton);
        
        // Start with Audio tab
        SwitchToTab(true);
        
        // Hide initially
        optionsPanel.SetActive(false);
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
        
        if (audioTabContent != null) audioTabContent.SetActive(isAudioTab);
        if (controlsTabContent != null) controlsTabContent.SetActive(!isAudioTab);
        
        // Update tab button colors
        Color activeColor = buttonColor;
        Color inactiveColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        
        if (audioTabButton != null)
            audioTabButton.GetComponent<Image>().color = isAudioTab ? activeColor : inactiveColor;
        if (controlsTabButton != null)
            controlsTabButton.GetComponent<Image>().color = isAudioTab ? inactiveColor : activeColor;
        
        // Rebuild UINavScope
        var scope = optionsPanel?.GetComponent<UINavScope>();
        scope?.Rebuild();
    }

    private void SelectFirstInCurrentTab()
    {
        if (isAudioTabActive)
        {
            if (audioController?.MasterVolumeSlider != null)
            {
                EventSystem.current?.SetSelectedGameObject(audioController.MasterVolumeSlider.gameObject);
            }
        }
        else
        {
            var buttons = controlsController?.ControlsTabButtons;
            if (buttons != null && buttons.Count > 0 && buttons[0] != null)
            {
                EventSystem.current?.SetSelectedGameObject(buttons[0].gameObject);
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Show the options menu with a callback for when it closes.
    /// </summary>
    public void Show(Action onClose = null)
    {
        onCloseCallback = onClose;
        isVisible = true;
        
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
        
        // Enable tab actions
        leftTabAction?.Enable();
        rightTabAction?.Enable();
        
        // Refresh and select first element
        SwitchToTab(true);
        SelectFirstInCurrentTab();
        
        Debug.Log("[OptionsMenuController] Options menu shown");
    }

    /// <summary>
    /// Hide the options menu.
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        
        // Disable tab actions
        leftTabAction?.Disable();
        rightTabAction?.Disable();
        
        // Invoke callback
        onCloseCallback?.Invoke();
        onCloseCallback = null;
        
        Debug.Log("[OptionsMenuController] Options menu hidden");
    }

    #endregion

    #region Callbacks

    private void OnBackClicked()
    {
        Hide();
    }

    private void ResetAllInputBindings()
    {
        if (controlsController == null || optionsPanel == null) return;
        
        controlsController.ResetAllInputBindings(optionsPanel.transform, (parent) =>
        {
            if (controlsTabContent != null)
            {
                Destroy(controlsTabContent);
                
                Transform contentContainer = optionsPanel.transform.Find("ContentContainer");
                if (contentContainer != null)
                {
                    controlsTabContent = controlsController.CreateControlsTabContent(
                        contentContainer, ResetAllInputBindings);
                    controlsTabContent.SetActive(!isAudioTabActive);
                    return controlsTabContent;
                }
            }
            return null;
        });
    }

    #endregion

    #region Static Helper

    /// <summary>
    /// Ensures an OptionsMenuController exists in the scene.
    /// </summary>
    public static OptionsMenuController EnsureExists()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("OptionsMenuController");
            instance = go.AddComponent<OptionsMenuController>();
        }
        return instance;
    }

    #endregion
}
