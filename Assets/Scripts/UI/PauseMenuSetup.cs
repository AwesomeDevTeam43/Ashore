using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to create the Pause Menu UI at runtime.
/// Attach to an empty GameObject in your first game scene (not MainMenu).
/// This creates a complete pause menu with Resume, Options, and Quit buttons.
/// </summary>
public class PauseMenuSetup : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] private Color buttonNormalColor = new Color(0.2f, 0.6f, 0.85f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color buttonPressedColor = new Color(0.15f, 0.5f, 0.7f, 1f);
    [SerializeField] private Color textColor = Color.white;
    
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private int fontSize = 36;
    
    private void Start()
    {
        // Check if PauseMenu already exists
        if (PauseMenu.Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        CreatePauseMenu();
    }

    private void CreatePauseMenu()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("PauseMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Make sure it's on top
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Add PauseMenu component
        PauseMenu pauseMenu = canvasObj.AddComponent<PauseMenu>();
        
        // Create background panel
        GameObject panelObj = CreatePanel(canvasObj.transform, "PauseMenuPanel", backgroundColor);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // Create title
        GameObject titleObj = CreateText(panelObj.transform, "PAUSED", fontSize + 20);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.sizeDelta = new Vector2(400, 100);
        
        // Create buttons container
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        buttonsContainer.transform.SetParent(panelObj.transform, false);
        RectTransform containerRect = buttonsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.35f, 0.25f);
        containerRect.anchorMax = new Vector2(0.65f, 0.65f);
        containerRect.sizeDelta = Vector2.zero;
        
        // Create buttons
        GameObject resumeBtn = CreateButton(buttonsContainer.transform, "ResumeButton", "Resume", 0.7f, 0.9f);
        GameObject optionsBtn = CreateButton(buttonsContainer.transform, "OptionsButton", "Options", 0.4f, 0.6f);
        GameObject quitBtn = CreateButton(buttonsContainer.transform, "QuitButton", "Quit to Menu", 0.1f, 0.3f);
        
        // Setup PauseMenu references via reflection or SerializedObject
        // Since we can't directly set SerializeField, we'll use a different approach
        SetupPauseMenuReferences(pauseMenu, panelObj, 
            resumeBtn.GetComponent<Button>(), 
            optionsBtn.GetComponent<Button>(), 
            quitBtn.GetComponent<Button>(),
            mainMenuSceneName);
        
        // Don't destroy on load
        DontDestroyOnLoad(canvasObj);
        
        // Destroy this setup object
        Destroy(gameObject);
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();
        image.color = color;
        
        return panel;
    }

    private GameObject CreateText(Transform parent, string text, int size)
    {
        GameObject textObj = new GameObject("Title");
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        
        return textObj;
    }

    private GameObject CreateButton(Transform parent, string name, string text, float yMin, float yMax)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, yMin);
        rect.anchorMax = new Vector2(1, yMax);
        rect.sizeDelta = Vector2.zero;
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = buttonNormalColor;
        
        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonNormalColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHoverColor;
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        
        // Add hover effect if available
        if (buttonObj.GetComponent<ButtonHoverEffect>() == null)
        {
            // Try to add ButtonHoverEffect component
            try
            {
                buttonObj.AddComponent<ButtonHoverEffect>();
            }
            catch { }
        }
        
        return buttonObj;
    }

    private void SetupPauseMenuReferences(PauseMenu pauseMenu, GameObject panel, 
        Button resume, Button options, Button quit, string mainMenuScene)
    {
        // We need to set the private fields - using a workaround
        // Create a helper component that will setup the references
        PauseMenuInitializer initializer = pauseMenu.gameObject.AddComponent<PauseMenuInitializer>();
        initializer.Initialize(panel, resume, options, quit, mainMenuScene);
    }
}

/// <summary>
/// Helper class to initialize PauseMenu references after creation
/// </summary>
public class PauseMenuInitializer : MonoBehaviour
{
    private GameObject panel;
    private Button resumeBtn;
    private Button optionsBtn;
    private Button quitBtn;
    private string mainMenuScene;
    private bool initialized = false;

    public void Initialize(GameObject panel, Button resume, Button options, Button quit, string mainMenuScene)
    {
        this.panel = panel;
        this.resumeBtn = resume;
        this.optionsBtn = options;
        this.quitBtn = quit;
        this.mainMenuScene = mainMenuScene;
    }

    private void Start()
    {
        if (initialized) return;
        initialized = true;
        
        PauseMenu pauseMenu = GetComponent<PauseMenu>();
        if (pauseMenu == null) return;
        
        // Setup button click events directly since we can't access private fields
        resumeBtn.onClick.AddListener(() => pauseMenu.ResumeGame());
        optionsBtn.onClick.AddListener(() => Debug.Log("Options clicked - implement your options menu"));
        quitBtn.onClick.AddListener(() => pauseMenu.QuitToMainMenu());
        
        // Hide panel initially
        panel.SetActive(false);
        
        // Store reference for pause toggle
        StartCoroutine(SetupPauseToggle(pauseMenu, panel));
    }

    private System.Collections.IEnumerator SetupPauseToggle(PauseMenu pauseMenu, GameObject panel)
    {
        yield return null;
        
        // Create a simple input handler
        PauseInputHandler handler = gameObject.AddComponent<PauseInputHandler>();
        handler.Setup(panel, mainMenuScene);
        
        // Remove this initializer
        Destroy(this);
    }
}

/// <summary>
/// Handles pause input when PauseMenu is created at runtime
/// </summary>
public class PauseInputHandler : MonoBehaviour
{
    private GameObject pausePanel;
    private string mainMenuScene;
    private bool isPaused = false;

    public void Setup(GameObject panel, string mainMenu)
    {
        pausePanel = panel;
        mainMenuScene = mainMenu;
    }

    private void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == mainMenuScene)
            return;
            
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (pausePanel != null)
            pausePanel.SetActive(isPaused);
            
        Time.timeScale = isPaused ? 0f : 1f;
        
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Confined;
    }
}
