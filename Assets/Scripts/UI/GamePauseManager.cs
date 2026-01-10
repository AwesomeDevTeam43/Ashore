using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    
    // UI References
    private Canvas pauseCanvas;
    private GameObject pausePanel;
    
    // State
    private bool isPaused = false;
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

        return panel;
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
        
        // Hide options menu if it's open
        if (OptionsMenuController.Instance != null)
            OptionsMenuController.Instance.Hide();
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
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

    #endregion

    #region Menu Actions

    public void QuitToMainMenu()
    {
        pausePanel.SetActive(false);
        
        // Also hide options menu
        if (OptionsMenuController.Instance != null)
            OptionsMenuController.Instance.Hide();
        
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
