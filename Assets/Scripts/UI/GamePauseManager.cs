using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Self-contained Pause Menu that creates its own UI and works across all scenes.
/// Just add this script to any GameObject in your first game scene.
/// </summary>
public class GamePauseManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("Appearance")]
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 0.85f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    [SerializeField] private int buttonFontSize = 42;
    [SerializeField] private int titleFontSize = 72;
    
    // Runtime references
    private Canvas pauseCanvas;
    private GameObject pausePanel;
    private GameObject optionsPanel;
    private bool isPaused = false;
    
    private static GamePauseManager instance;
    public static GamePauseManager Instance => instance;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        // Singleton - persist across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventSystemExists();
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Always hide pause menu when entering main menu
        if (scene.name == mainMenuSceneName)
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }
        
        // Ensure EventSystem exists in each scene
        EnsureEventSystemExists();
    }

    /// <summary>
    /// Ensures an EventSystem exists in the scene (required for UI buttons to work)
    /// </summary>
    private void EnsureEventSystemExists()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystem);
        }
    }

    private void Update()
    {
        // Don't pause in main menu
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;

        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

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

        // Create main pause panel
        pausePanel = CreateMainPausePanel(canvasGO.transform);
        
        // Create options panel
        optionsPanel = CreateOptionsPanel(canvasGO.transform);
        
        // Hide both panels initially
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    private GameObject CreateMainPausePanel(Transform parent)
    {
        // Background overlay
        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(parent, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image bgImage = panel.AddComponent<Image>();
        bgImage.color = overlayColor;

        // Title
        CreateTextElement(panel.transform, "PAUSED", titleFontSize, 
            new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.88f), new Vector2(600, 0));

        // Resume Button
        CreateButton(panel.transform, "Resume", 
            new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.65f),
            Resume);

        // Options Button
        CreateButton(panel.transform, "Options", 
            new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.52f),
            ShowOptions);

        // Quit Button
        CreateButton(panel.transform, "Quit to Menu", 
            new Vector2(0.35f, 0.29f), new Vector2(0.65f, 0.39f),
            QuitToMainMenu);

        return panel;
    }

    private GameObject CreateOptionsPanel(Transform parent)
    {
        // Background overlay
        GameObject panel = new GameObject("OptionsPanel");
        panel.transform.SetParent(parent, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image bgImage = panel.AddComponent<Image>();
        bgImage.color = overlayColor;

        // Title
        CreateTextElement(panel.transform, "OPTIONS", titleFontSize, 
            new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.88f), new Vector2(600, 0));

        // Placeholder text
        CreateTextElement(panel.transform, "Options coming soon...", buttonFontSize - 10, 
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.55f), new Vector2(600, 0));

        // Back Button
        CreateButton(panel.transform, "Back", 
            new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.35f),
            HideOptions);

        return panel;
    }

    private void CreateTextElement(Transform parent, string text, int fontSize, 
        Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject textGO = new GameObject("Text_" + text);
        textGO.transform.SetParent(parent, false);
        
        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        
        // Add gradient effect for title
        if (fontSize >= 60)
        {
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(
                new Color(0.6f, 0.85f, 1f),  // top left
                new Color(0.6f, 0.85f, 1f),  // top right
                new Color(1f, 0.95f, 0.8f),  // bottom left
                new Color(1f, 0.95f, 0.8f)   // bottom right
            );
        }
    }

    private void CreateButton(Transform parent, string text, 
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject("Button_" + text);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        
        Image image = buttonGO.AddComponent<Image>();
        image.color = buttonColor;
        
        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = new Color(buttonColor.r * 0.8f, buttonColor.g * 0.8f, buttonColor.b * 0.8f);
        colors.selectedColor = buttonHoverColor;
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        
        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = buttonFontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    public void ShowOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void HideOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void QuitToMainMenu()
    {
        // Hide panels first
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

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
