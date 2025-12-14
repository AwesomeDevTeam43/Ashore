using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
            
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
            pauseMenuPanel.SetActive(true);
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
