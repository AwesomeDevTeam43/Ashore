using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class PersistentCamera : MonoBehaviour
{
  [Tooltip("Custom name used to group cameras. When empty, the GameObject name is used.")]
  [SerializeField] private string persistenceKey;

  private string PersistenceName => string.IsNullOrEmpty(persistenceKey) ? gameObject.name : persistenceKey;

  private void Awake()
  {
    DontDestroyOnLoad(gameObject);
    RemoveDuplicateCameras();
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
    RemoveDuplicateCameras();
  }

  private void RemoveDuplicateCameras()
  {
    // Keep only this camera instance for the given persistence name.
    string targetName = PersistenceName;
    var allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
    foreach (var cam in allCameras)
    {
      if (cam == null || cam.gameObject == gameObject) continue;
      if (cam.gameObject.name == targetName)
      {
        Destroy(cam.gameObject);
      }
    }
  }
}
