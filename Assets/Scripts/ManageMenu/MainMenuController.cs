using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the Main Menu navigation between panels.
/// Flow: Main Menu (Start Game / Options / Quit) → Save Slots Panel → Game
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject saveSlotsPanel;
    [SerializeField] private GameObject optionsPanel;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Save Slots Panel")]
    [SerializeField] private Button slotsBackButton;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private Button[] deleteButtons;
    [SerializeField] private TextMeshProUGUI[] slotTitleTexts;
    [SerializeField] private TextMeshProUGUI[] slotInfoTexts;
    
    [Header("Options Panel")]
    [SerializeField] private Button optionsBackButton;
    
    [Header("Game Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string tutorialSceneName = "TutorialScene";
    [SerializeField] private Vector3 tutorialSpawnPosition = new Vector3(-44.36f, -4.01f, 0f);
    [SerializeField] private PlayerStats defaultPlayerStats;
    
    [Header("Style")]
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 0.85f, 1f);
    [SerializeField] private Color slotEmptyColor = new Color(0.3f, 0.4f, 0.5f, 0.8f);
    [SerializeField] private Color highlightedColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.15f, 0.5f, 0.7f, 1f);

    private Canvas menuCanvas;

    private void Start()
    {
        // Find canvas reference for main menu
        menuCanvas = FindAnyObjectByType<Canvas>();
        
        SetupMainMenuButtons();
        
        // Always create save slots panel dynamically for consistency
        CreateSaveSlotsPanel();
        SetupSlotButtons();
        
        SetupOptionsButtons();
        
        // Start with main menu visible, others hidden
        ShowMainMenu();
    }

    #region Dynamic UI Creation

    private void CreateSaveSlotsPanel()
    {
        Debug.Log("[MainMenuController] Creating Save Slots Panel with own Canvas");
        
        // Create dedicated Canvas (like OptionsMenuController does)
        GameObject canvasGO = new GameObject("SaveSlotsCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above main menu
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create main panel with dark overlay
        saveSlotsPanel = new GameObject("SaveSlotsPanel");
        saveSlotsPanel.transform.SetParent(canvasGO.transform, false);
        
        RectTransform panelRect = saveSlotsPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image panelBg = saveSlotsPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.9f);
        
        // Title
        CreateTextElement(saveSlotsPanel.transform, "SELECT SAVE SLOT", 64, 
            new Vector2(0.3f, 0.80f), new Vector2(0.7f, 0.92f));
        
        // Create 3 slot buttons
        slotButtons = new Button[3];
        slotTitleTexts = new TextMeshProUGUI[3];
        slotInfoTexts = new TextMeshProUGUI[3];
        deleteButtons = new Button[3];
        
        float startY = 0.70f;
        float buttonHeight = 0.12f;
        float spacing = 0.03f;
        
        for (int i = 0; i < 3; i++)
        {
            float yMax = startY - (i * (buttonHeight + spacing));
            float yMin = yMax - buttonHeight;
            
            // Create button container
            GameObject buttonGO = new GameObject($"SlotButton_{i + 1}");
            buttonGO.transform.SetParent(saveSlotsPanel.transform, false);
            
            RectTransform btnRect = buttonGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.25f, yMin);
            btnRect.anchorMax = new Vector2(0.70f, yMax); // Reduced width to make room for delete button
            btnRect.sizeDelta = Vector2.zero;
            
            Image btnImg = buttonGO.AddComponent<Image>();
            btnImg.color = buttonColor;
            btnImg.raycastTarget = true;
            
            Button btn = buttonGO.AddComponent<Button>();
            
            // Setup button colors
            ColorBlock colors = btn.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = highlightedColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.targetGraphic = btnImg;
            
            // Navigation
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.Automatic;
            btn.navigation = nav;
            
            slotButtons[i] = btn;
            
            // Title text (Slot 1, Slot 2, etc.)
            slotTitleTexts[i] = CreateTextElement(buttonGO.transform, $"Slot {i + 1}", 36, 
                new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.95f), TextAlignmentOptions.Left);
            slotTitleTexts[i].fontStyle = FontStyles.Bold;
            
            // Info text (Level / Play Time)
            slotInfoTexts[i] = CreateTextElement(buttonGO.transform, "— New Game —", 24, 
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.5f), TextAlignmentOptions.Left);
            slotInfoTexts[i].color = new Color(0.85f, 0.9f, 1f, 0.85f);
            
            // Delete button (red X)
            GameObject deleteBtnGO = new GameObject($"DeleteButton_{i + 1}");
            deleteBtnGO.transform.SetParent(saveSlotsPanel.transform, false);
            
            RectTransform deleteRect = deleteBtnGO.AddComponent<RectTransform>();
            deleteRect.anchorMin = new Vector2(0.71f, yMin);
            deleteRect.anchorMax = new Vector2(0.75f, yMax);
            deleteRect.sizeDelta = Vector2.zero;
            
            Image deleteImg = deleteBtnGO.AddComponent<Image>();
            deleteImg.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Red color
            deleteImg.raycastTarget = true;
            
            Button deleteBtn = deleteBtnGO.AddComponent<Button>();
            deleteBtn.targetGraphic = deleteImg;
            
            ColorBlock deleteColors = deleteBtn.colors;
            deleteColors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            deleteColors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
            deleteColors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
            deleteColors.selectedColor = new Color(1f, 0.3f, 0.3f, 1f);
            deleteColors.fadeDuration = 0.1f;
            deleteBtn.colors = deleteColors;
            
            // X text
            CreateTextElement(deleteBtnGO.transform, "X", 32, 
                new Vector2(0f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.Center);
            
            deleteButtons[i] = deleteBtn;
        }
        
        // Back button
        GameObject backBtnGO = new GameObject("BackButton");
        backBtnGO.transform.SetParent(saveSlotsPanel.transform, false);
        
        RectTransform backRect = backBtnGO.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.35f, 0.08f);
        backRect.anchorMax = new Vector2(0.65f, 0.18f);
        backRect.sizeDelta = Vector2.zero;
        
        Image backImg = backBtnGO.AddComponent<Image>();
        backImg.color = buttonColor;
        backImg.raycastTarget = true;
        
        slotsBackButton = backBtnGO.AddComponent<Button>();
        slotsBackButton.targetGraphic = backImg;
        
        ColorBlock backColors = slotsBackButton.colors;
        backColors.normalColor = buttonColor;
        backColors.highlightedColor = highlightedColor;
        backColors.pressedColor = pressedColor;
        backColors.selectedColor = highlightedColor;
        backColors.fadeDuration = 0.1f;
        slotsBackButton.colors = backColors;
        
        Navigation backNav = slotsBackButton.navigation;
        backNav.mode = Navigation.Mode.Automatic;
        slotsBackButton.navigation = backNav;
        
        CreateTextElement(backBtnGO.transform, "Back", 32, 
            new Vector2(0f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.Center);
        
        // Start hidden
        saveSlotsPanel.SetActive(false);
        
        Debug.Log("[MainMenuController] Save Slots Panel created successfully with own Canvas!");
    }

    private TextMeshProUGUI CreateTextElement(Transform parent, string text, float fontSize, 
        Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textGO = new GameObject("Text_" + text);
        textGO.transform.SetParent(parent, false);
        
        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        return tmp;
    }

    #endregion

    #region Button Setup

    private void SetupMainMenuButtons()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
            ApplyButtonStyle(startGameButton);
            Debug.Log("[MainMenuController] Start Game button configured");
        }
        else
        {
            Debug.LogWarning("[MainMenuController] Start Game button not assigned!");
        }
        
        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnOptionsClicked);
            ApplyButtonStyle(optionsButton);
            Debug.Log("[MainMenuController] Options button configured");
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
            ApplyButtonStyle(quitButton);
            Debug.Log("[MainMenuController] Quit button configured");
        }
    }

    private void SetupSlotButtons()
    {
        if (slotButtons == null || slotButtons.Length == 0)
        {
            Debug.LogWarning("[MainMenuController] No slot buttons assigned!");
            return;
        }
        
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
            {
                int slotNumber = i + 1;
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotNumber));
                UpdateSlotDisplay(i);
            }
            
            // Setup delete button
            if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
            {
                int slotToDelete = i + 1;
                deleteButtons[i].onClick.RemoveAllListeners();
                deleteButtons[i].onClick.AddListener(() => OnDeleteSlotClicked(slotToDelete));
            }
        }
        
        if (slotsBackButton != null)
        {
            slotsBackButton.onClick.RemoveAllListeners();
            slotsBackButton.onClick.AddListener(OnSlotsBackClicked);
            ApplyButtonStyle(slotsBackButton);
        }
    }

    private void SetupOptionsButtons()
    {
        if (optionsBackButton != null)
        {
            optionsBackButton.onClick.RemoveAllListeners();
            optionsBackButton.onClick.AddListener(OnOptionsBackClicked);
            ApplyButtonStyle(optionsBackButton);
        }
    }

    private void ApplyButtonStyle(Button button)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = highlightedColor;
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        
        // Set automatic navigation for gamepad
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
        
        // Add hover effect if available
        if (button.GetComponent<ButtonHoverEffect>() == null)
        {
            var hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();
        }
    }

    #endregion

    #region Slot Display

    private void UpdateSlotDisplay(int index)
    {
        if (index < 0 || slotButtons == null || index >= slotButtons.Length) return;
        
        int slotNumber = index + 1;
        var data = SaveSystem.LoadPlayer(slotNumber);
        bool hasSave = data != null;
        
        // Update button color
        if (slotButtons[index] != null)
        {
            ColorBlock colors = slotButtons[index].colors;
            colors.normalColor = hasSave ? buttonColor : slotEmptyColor;
            colors.highlightedColor = highlightedColor;
            colors.selectedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            slotButtons[index].colors = colors;
            
            // Apply button style
            Navigation nav = slotButtons[index].navigation;
            nav.mode = Navigation.Mode.Automatic;
            slotButtons[index].navigation = nav;
        }
        
        // Update title text
        if (slotTitleTexts != null && index < slotTitleTexts.Length && slotTitleTexts[index] != null)
        {
            slotTitleTexts[index].text = $"Slot {slotNumber}";
            slotTitleTexts[index].fontSize = 32;
            slotTitleTexts[index].fontStyle = FontStyles.Bold;
            slotTitleTexts[index].color = Color.white;
        }
        
        // Update info text
        if (slotInfoTexts != null && index < slotInfoTexts.Length && slotInfoTexts[index] != null)
        {
            if (hasSave)
            {
                slotInfoTexts[index].text = $"Level {data.level}  •  {FormatPlayTime(data.playTime)}";
            }
            else
            {
                slotInfoTexts[index].text = "— New Game —";
            }
            slotInfoTexts[index].fontSize = 22;
            slotInfoTexts[index].color = new Color(0.9f, 0.95f, 1f, 0.9f);
        }
    }

    private string FormatPlayTime(float playTime)
    {
        int totalSeconds = Mathf.FloorToInt(playTime);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        
        if (hours > 0)
            return $"{hours}h {minutes:D2}m";
        else
            return $"{minutes}m {seconds:D2}s";
    }

    public void RefreshAllSlots()
    {
        if (slotButtons == null) return;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            UpdateSlotDisplay(i);
        }
    }

    #endregion

    #region Panel Navigation

    public void ShowMainMenu()
    {
        Debug.Log("[MainMenuController] Showing Main Menu");
        
        if (mainMenuPanel != null) 
        {
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[MainMenuController] mainMenuPanel is NULL!");
        }
        
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        
        // Select first button for gamepad
        if (startGameButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(startGameButton.gameObject);
        }
    }

    public void ShowSaveSlots()
    {
        Debug.Log("[MainMenuController] Showing Save Slots");
        
        // Hide main menu
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        
        // Show save slots
        if (saveSlotsPanel != null) 
        {
            saveSlotsPanel.SetActive(true);
            Debug.Log("[MainMenuController] SaveSlotsPanel activated");
        }
        else
        {
            Debug.LogError("[MainMenuController] saveSlotsPanel is NULL!");
            return;
        }
        
        // Refresh slot info
        RefreshAllSlots();
        
        // Select first slot for gamepad
        if (slotButtons != null && slotButtons.Length > 0 && slotButtons[0] != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(slotButtons[0].gameObject);
        }
    }

    public void ShowOptions()
    {
        Debug.Log("[MainMenuController] Showing Options via OptionsMenuController");
        
        // Hide main menu while options are shown
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(false);
        
        // Use the shared OptionsMenuController
        var optionsMenu = OptionsMenuController.EnsureExists();
        optionsMenu.Show(OnOptionsMenuClosed);
    }
    
    private void OnOptionsMenuClosed()
    {
        // When options closes, return to main menu
        Debug.Log("[MainMenuController] Options closed, returning to main menu");
        ShowMainMenu();
    }

    #endregion

    #region Button Callbacks

    private void OnStartGameClicked()
    {
        Debug.Log("[MainMenuController] Start Game clicked!");
        ShowSaveSlots();
    }

    private void OnOptionsClicked()
    {
        Debug.Log("[MainMenuController] Options clicked!");
        ShowOptions();
    }

    private void OnSlotsBackClicked()
    {
        Debug.Log("[MainMenuController] Slots Back clicked!");
        ShowMainMenu();
    }

    private void OnOptionsBackClicked()
    {
        Debug.Log("[MainMenuController] Options Back clicked!");
        ShowMainMenu();
    }

    private void OnSlotClicked(int slotNumber)
    {
        Debug.Log($"[MainMenuController] Slot {slotNumber} clicked!");
        
        SaveSlotTracker.CurrentSlot = slotNumber;
        
        var data = SaveSystem.LoadPlayer(slotNumber);
        
        if (data != null)
        {
            // Save exists: load existing game
            Debug.Log($"[MainMenuController] Loading existing save from slot {slotNumber}");
            Debug.Log($"[MainMenuController] Existing save position: ({data.position[0]}, {data.position[1]}, {data.position[2]})");
            GameFlowState.LoadGameOnStart = true;
            SceneManager.LoadScene(data.sceneName);
        }
        else
        {
            // No save: create new game
            Debug.Log($"[MainMenuController] Creating new game in slot {slotNumber}");
            Debug.Log($"[MainMenuController] tutorialSpawnPosition = {tutorialSpawnPosition}");
            string targetScene = string.IsNullOrEmpty(tutorialSceneName) ? gameSceneName : tutorialSceneName;
            
            SaveSystem.CreateNewGameSave(defaultPlayerStats, targetScene, tutorialSpawnPosition, slotNumber);
            GameFlowState.LoadGameOnStart = true;
            SceneManager.LoadScene(targetScene);
        }
    }

    private void OnDeleteSlotClicked(int slotNumber)
    {
        Debug.Log($"[MainMenuController] Delete Slot {slotNumber} clicked!");
        
        // Delete the save file
        SaveSystem.DeleteSave(slotNumber);
        
        // Refresh the slot display
        UpdateSlotDisplay(slotNumber - 1);
        
        Debug.Log($"[MainMenuController] Slot {slotNumber} deleted successfully!");
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuController] Quit clicked!");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    #endregion
}
