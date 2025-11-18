using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Manage_MainMenu : MonoBehaviour
{
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string tutorialSceneName = "TutorialScene";
    [SerializeField] private Vector3 tutorialSpawnPosition = Vector3.zero;
    [SerializeField] private Transform tutorialSpawnOverride;
    [SerializeField] private PlayerStats defaultPlayerStats;

    private void Awake()
    {
        EnsureMenuCamera();
    }

    void Start()
    {
        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadClicked);
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnLoadClicked()
    {
        string savedScene = SaveSystem.GetSavedSceneName();
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
        SaveSystem.CreateNewGameSave(defaultPlayerStats, tutorialSceneName, spawnPos);
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