
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// Tracks the current save slot selected by the player
public static class SaveSlotTracker
{
    public static int CurrentSlot = 1;
}

public class Manage_MainMenu : MonoBehaviour
{
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string tutorialSceneName = "TutorialScene";
    [SerializeField] private Vector3 tutorialSpawnPosition = new Vector3(-44.36f, -4.01f, 0f);
    [SerializeField] private Transform tutorialSpawnOverride;
    [SerializeField] private PlayerStats defaultPlayerStats;

    private void Awake()
    {
        EnsureMenuCamera();
    }

    void Start()
    {
        // Button listeners are now handled by MainMenuController
        // This script only handles camera setup and utility functions
    }

    // Call this from your save slot UI (e.g. button click) to set the slot
    public void SelectSaveSlot(int slot)
    {
        SaveSlotTracker.CurrentSlot = Mathf.Clamp(slot, 1, 3);
    }

    private void OnLoadClicked()
    {
        string savedScene = SaveSystem.GetSavedSceneName(SaveSlotTracker.CurrentSlot);
        if (!string.IsNullOrEmpty(savedScene))
        {
            GameFlowState.LoadGameOnStart = true;
            SceneManager.LoadScene(savedScene);
        }
        else
        {
            // No save found: start a fresh game
            GameFlowState.LoadGameOnStart = false;
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void OnNewGameClicked()
    {
        Vector3 spawnPos = tutorialSpawnPosition;
        if (tutorialSpawnOverride != null)
        {
            spawnPos = tutorialSpawnOverride.position;
        }
        SaveSystem.CreateNewGameSave(defaultPlayerStats, tutorialSceneName, spawnPos, SaveSlotTracker.CurrentSlot);
        GameFlowState.LoadGameOnStart = true;
        SceneManager.LoadScene(string.IsNullOrEmpty(tutorialSceneName) ? gameSceneName : tutorialSceneName);
    }

    public void GoBackToMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    private void OnQuitClicked()
    {
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void EnsureMenuCamera()
    {
        var existingCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var existing in existingCameras)
        {
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }

        var camGO = new GameObject("MainMenuCamera", typeof(Camera), typeof(AudioListener));
        var cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        cam.enabled = true;

        var canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null)
            {
                c.worldCamera = cam;
            }
        }
    }
}