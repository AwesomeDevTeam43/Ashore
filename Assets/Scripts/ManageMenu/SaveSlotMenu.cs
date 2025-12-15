using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveSlotMenu : MonoBehaviour
{
    [System.Serializable]
    public class SaveSlotUI
    {
        public Button slotButton;
        public Text infoText;
        public int slotNumber;
    }

    public SaveSlotUI[] slots = new SaveSlotUI[3];
    public string newGameScene = "GameScene";
    public string mainMenuScene = "MainMenu";
    public PlayerStats defaultPlayerStats;
    public string tutorialSceneName = "TutorialScene";
    public Vector3 tutorialSpawnPosition = Vector3.zero;
    public Transform tutorialSpawnOverride;

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
            }
            UpdateSlotInfo(slots[i]);
        }
    }

    void UpdateSlotInfo(SaveSlotUI slotUI)
    {
        var data = SaveSystem.LoadPlayer(slotUI.slotNumber);
        if (data != null)
        {
            // Display info (customize as needed)
            slotUI.infoText.text = $"Level: {data.level}\nPlay Time: {FormatPlayTime(data.playTime)}";
        }
        else
        {
            slotUI.infoText.text = "New Game";
        }
    }

    string FormatPlayTime(float playTime)
    {
        int totalSeconds = Mathf.FloorToInt(playTime);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
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
}
