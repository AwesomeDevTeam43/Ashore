using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Pause Menu that works across all game scenes.
/// Attach this to a Canvas GameObject that will persist between scenes.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Options Panel (Optional)")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button optionsBackButton;
    
    private bool isPaused = false;
    private static PauseMenu instance;
    private EventSystem es;
    private GameObject lastSelected;
    
    public static PauseMenu Instance => instance;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        // Singleton pattern - persist across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Setup button listeners
        SetupButtons();
        
        // Wire up navigation between buttons
        SetupNavigation();
        
        // Get EventSystem reference
        es = EventSystem.current;
        
        // Start with menu hidden
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Don't show pause menu in main menu scene
        if (scene.name == mainMenuSceneName)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        // If GamePauseManager is present, let it handle pausing instead
        if (GamePauseManager.Instance != null)
            return;
            
        // Don't process pause in main menu
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;
            
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
        
        // Handle manual navigation when paused
        if (isPaused && pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            HandleManualNavigation();
        }
    }
    
    /// <summary>
    /// Manual navigation fallback using direct input polling.
    /// Uses legacy Input for keyboard since it works when Time.timeScale = 0.
    /// </summary>
    private void HandleManualNavigation()
    {
        if (es == null) es = EventSystem.current;
        if (es == null) return;
        
        // Ensure we have a valid selection
        if (es.currentSelectedGameObject == null || !es.currentSelectedGameObject.activeInHierarchy)
        {
            SetDefaultSelection();
            return;
        }
        
        Selectable current = es.currentSelectedGameObject.GetComponent<Selectable>();
        if (current == null) return;
        
        Selectable next = null;
        bool submit = false;
        
        // Use legacy Input.GetKeyDown - works even when Time.timeScale = 0
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            next = current.FindSelectableOnDown();
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            next = current.FindSelectableOnUp();
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            next = current.FindSelectableOnLeft();
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            next = current.FindSelectableOnRight();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            submit = true;
        
        // Check gamepad using legacy Input for joystick buttons (works when paused)
        if (next == null && !submit)
        {
            // Joystick button mappings vary, but we can check common ones
            // Also check new Input System with manual state tracking for gamepad
            if (Gamepad.current != null)
            {
                // For gamepad, we need to track state changes manually since wasPressedThisFrame doesn't work at timeScale=0
                CheckGamepadNavigation(current, ref next, ref submit);
            }
        }
        
        if (submit && current is Button btn)
        {
            btn.onClick.Invoke();
        }
        else if (next != null && next.interactable)
        {
            es.SetSelectedGameObject(next.gameObject);
        }
    }
    
    // Gamepad state tracking for when Time.timeScale = 0
    private bool prevDpadUp, prevDpadDown, prevDpadLeft, prevDpadRight, prevSubmit;
    private float prevStickY, prevStickX;
    private float stickRepeatTimer = 0f;
    private const float STICK_THRESHOLD = 0.5f;
    private const float STICK_REPEAT_DELAY = 0.3f;
    
    private void CheckGamepadNavigation(Selectable current, ref Selectable next, ref bool submit)
    {
        if (Gamepad.current == null) return;
        
        bool dpadUp = Gamepad.current.dpad.up.isPressed;
        bool dpadDown = Gamepad.current.dpad.down.isPressed;
        bool dpadLeft = Gamepad.current.dpad.left.isPressed;
        bool dpadRight = Gamepad.current.dpad.right.isPressed;
        bool submitBtn = Gamepad.current.buttonSouth.isPressed;
        
        Vector2 stick = Gamepad.current.leftStick.ReadValue();
        
        // D-pad: detect press (transition from not pressed to pressed)
        if (dpadDown && !prevDpadDown)
            next = current.FindSelectableOnDown();
        else if (dpadUp && !prevDpadUp)
            next = current.FindSelectableOnUp();
        else if (dpadLeft && !prevDpadLeft)
            next = current.FindSelectableOnLeft();
        else if (dpadRight && !prevDpadRight)
            next = current.FindSelectableOnRight();
        
        // Submit button
        if (submitBtn && !prevSubmit)
            submit = true;
        
        // Stick navigation with repeat using unscaled time
        stickRepeatTimer -= Time.unscaledDeltaTime;
        if (stickRepeatTimer <= 0f)
        {
            if (stick.y < -STICK_THRESHOLD && next == null)
            {
                next = current.FindSelectableOnDown();
                stickRepeatTimer = STICK_REPEAT_DELAY;
            }
            else if (stick.y > STICK_THRESHOLD && next == null)
            {
                next = current.FindSelectableOnUp();
                stickRepeatTimer = STICK_REPEAT_DELAY;
            }
            else if (stick.x < -STICK_THRESHOLD && next == null)
            {
                next = current.FindSelectableOnLeft();
                stickRepeatTimer = STICK_REPEAT_DELAY;
            }
            else if (stick.x > STICK_THRESHOLD && next == null)
            {
                next = current.FindSelectableOnRight();
                stickRepeatTimer = STICK_REPEAT_DELAY;
            }
        }
        
        // Reset timer when stick returns to center
        if (Mathf.Abs(stick.x) < STICK_THRESHOLD && Mathf.Abs(stick.y) < STICK_THRESHOLD)
        {
            stickRepeatTimer = 0f;
        }
        
        // Store previous states for next frame
        prevDpadUp = dpadUp;
        prevDpadDown = dpadDown;
        prevDpadLeft = dpadLeft;
        prevDpadRight = dpadRight;
        prevSubmit = submitBtn;
        prevStickX = stick.x;
        prevStickY = stick.y;
    }

    private void SetupButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitToMainMenu);
            
        if (optionsBackButton != null)
            optionsBackButton.onClick.AddListener(CloseOptions);
    }
    
    private void SetupNavigation()
    {
        // Wire vertical navigation between main menu buttons
        Button[] mainButtons = new Button[] { resumeButton, optionsButton, quitButton };
        
        for (int i = 0; i < mainButtons.Length; i++)
        {
            if (mainButtons[i] == null) continue;
            
            Navigation nav = mainButtons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            
            // Find previous valid button
            for (int j = i - 1; j >= 0; j--)
            {
                if (mainButtons[j] != null)
                {
                    nav.selectOnUp = mainButtons[j];
                    break;
                }
            }
            
            // Find next valid button
            for (int j = i + 1; j < mainButtons.Length; j++)
            {
                if (mainButtons[j] != null)
                {
                    nav.selectOnDown = mainButtons[j];
                    break;
                }
            }
            
            mainButtons[i].navigation = nav;
        }
        
        // Wire options back button
        if (optionsBackButton != null)
        {
            Navigation nav = optionsBackButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            optionsBackButton.navigation = nav;
        }
    }
    
    private void SetDefaultSelection()
    {
        if (es == null) es = EventSystem.current;
        if (es == null) return;
        
        // If options panel is open, select options back button
        if (optionsPanel != null && optionsPanel.activeSelf && optionsBackButton != null)
        {
            es.SetSelectedGameObject(optionsBackButton.gameObject);
            return;
        }
        
        // Otherwise select resume button
        if (resumeButton != null && resumeButton.gameObject.activeInHierarchy)
        {
            es.SetSelectedGameObject(resumeButton.gameObject);
        }
        else if (optionsButton != null && optionsButton.gameObject.activeInHierarchy)
        {
            es.SetSelectedGameObject(optionsButton.gameObject);
        }
        else if (quitButton != null && quitButton.gameObject.activeInHierarchy)
        {
            es.SetSelectedGameObject(quitButton.gameObject);
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
            
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Set default selection for keyboard/controller navigation
        SetDefaultSelection();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
            
        // Hide cursor if needed (for first-person games)
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            optionsPanel.SetActive(true);
            
            // Select the back button for navigation
            if (optionsBackButton != null && es != null)
            {
                es.SetSelectedGameObject(optionsBackButton.gameObject);
            }
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
            pauseMenuPanel.SetActive(true);
            
            // Re-select default button
            SetDefaultSelection();
        }
    }

    public void QuitToMainMenu()
    {
        // Resume time before loading new scene
        Time.timeScale = 1f;
        isPaused = false;
        
        // Load main menu
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

    private void OnDestroy()
    {
        // Clean up button listeners
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);
            
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(OpenOptions);
            
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitToMainMenu);
            
        if (optionsBackButton != null)
            optionsBackButton.onClick.RemoveListener(CloseOptions);
            
        if (instance == this)
            instance = null;
    }
}
