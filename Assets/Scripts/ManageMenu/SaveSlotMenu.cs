using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveSlotMenu : MonoBehaviour
{
    [System.Serializable]
    public class SaveSlotUI
    {
        public Button slotButton;
        public TextMeshProUGUI titleText;      // "Slot 1", "Slot 2", etc.
        public TextMeshProUGUI infoText;       // Level/PlayTime or "New Game"
        [HideInInspector] public int slotNumber;
    }

    [Header("Save Slots")]
    public SaveSlotUI[] slots = new SaveSlotUI[3];
    
    [Header("Scene Settings")]
    public string newGameScene = "GameScene";
    public string mainMenuScene = "MainMenu";
    public string tutorialSceneName = "TutorialScene";
    
    [Header("New Game Settings")]
    public PlayerStats defaultPlayerStats;
    public Vector3 tutorialSpawnPosition = new Vector3(-44.36f, -4.01f, 0f);
    public Transform tutorialSpawnOverride;
    
    [Header("Style Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 0.85f, 1f);
    [SerializeField] private Color highlightedColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.15f, 0.5f, 0.7f, 1f);
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.4f, 0.5f, 0.8f);
    [SerializeField] private float titleFontSize = 32f;
    [SerializeField] private float infoFontSize = 22f;

    void Start()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int slot = i + 1;
            slots[i].slotNumber = slot;
            
            if (slots[i].slotButton != null)
            {
                int capturedSlot = slot;
                slots[i].slotButton.onClick.AddListener(() => OnSlotClicked(capturedSlot));
                
                // Apply consistent button style
                ApplyButtonStyle(slots[i].slotButton, slots[i]);
            }
            
            UpdateSlotInfo(slots[i]);
        }
    }

    void ApplyButtonStyle(Button button, SaveSlotUI slotUI)
    {
        // Check if save exists to determine color
        var data = SaveSystem.LoadPlayer(slotUI.slotNumber);
        bool hasSave = data != null;
        
        ColorBlock colors = button.colors;
        colors.normalColor = hasSave ? normalColor : emptySlotColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = highlightedColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        
        // Set navigation for gamepad
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
        
        // Add hover effect if available
        if (button.GetComponent<ButtonHoverEffect>() == null)
        {
            button.gameObject.AddComponent<ButtonHoverEffect>();
        }
        
        // Style title text
        if (slotUI.titleText != null)
        {
            slotUI.titleText.fontSize = titleFontSize;
            slotUI.titleText.fontStyle = FontStyles.Bold;
            slotUI.titleText.color = Color.white;
            slotUI.titleText.text = $"Slot {slotUI.slotNumber}";
        }
        
        // Style info text
        if (slotUI.infoText != null)
        {
            slotUI.infoText.fontSize = infoFontSize;
            slotUI.infoText.color = new Color(0.9f, 0.95f, 1f, 0.9f);
        }
    }

    void UpdateSlotInfo(SaveSlotUI slotUI)
    {
        if (slotUI.infoText == null) return;
        
        var data = SaveSystem.LoadPlayer(slotUI.slotNumber);
        if (data != null)
        {
            slotUI.infoText.text = $"Level {data.level}  •  {FormatPlayTime(data.playTime)}";
            
            // Update button color to show it has a save
            if (slotUI.slotButton != null)
            {
                ColorBlock colors = slotUI.slotButton.colors;
                colors.normalColor = normalColor;
                slotUI.slotButton.colors = colors;
            }
        }
        else
        {
            slotUI.infoText.text = "— Empty Slot —";
            
            // Use dimmer color for empty slots
            if (slotUI.slotButton != null)
            {
                ColorBlock colors = slotUI.slotButton.colors;
                colors.normalColor = emptySlotColor;
                slotUI.slotButton.colors = colors;
            }
        }
        
        // Update title
        if (slotUI.titleText != null)
        {
            slotUI.titleText.text = $"Slot {slotUI.slotNumber}";
        }
    }

    string FormatPlayTime(float playTime)
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

    public void OnSlotClicked(int slot)
    {
        SaveSlotTracker.CurrentSlot = slot;
        var data = SaveSystem.LoadPlayer(slot);
        if (data != null)
        {
            // Save exists: load game
            GameFlowState.LoadGameOnStart = true;
            SceneManager.LoadScene(data.sceneName);
        }
        else
        {
            // No save: create new game and start
            Vector3 spawnPos = tutorialSpawnPosition;
            if (tutorialSpawnOverride != null)
                spawnPos = tutorialSpawnOverride.position;
            SaveSystem.CreateNewGameSave(defaultPlayerStats, tutorialSceneName, spawnPos, slot);
            GameFlowState.LoadGameOnStart = true;
            SceneManager.LoadScene(string.IsNullOrEmpty(tutorialSceneName) ? newGameScene : tutorialSceneName);
        }
    }
    
    /// <summary>
    /// Refresh all slot displays (call after deleting a save)
    /// </summary>
    public void RefreshSlots()
    {
        foreach (var slot in slots)
        {
            UpdateSlotInfo(slot);
        }
    }
}
