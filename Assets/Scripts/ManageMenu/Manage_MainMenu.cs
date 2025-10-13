using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Manage_MainMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuScene = "MainMenu";

    void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnStartClicked()
    {
        SceneManager.LoadScene(gameSceneName);
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
}