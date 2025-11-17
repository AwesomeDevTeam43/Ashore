using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Keeps the camera from fully following the player into a LevelPortal by temporarily
// switching the camera Follow target to an anchor that clamps near the portal edge.
// Works with Cinemachine 3 (Unity.Cinemachine) and a CinemachineCamera.
public class CameraPortalStick : MonoBehaviour
{
  public static CameraPortalStick Instance; // singleton for persistence/dedup
  [Header("References")]
  [SerializeField] private CinemachineCamera cinemachineCamera;
  [SerializeField] private string playerTag = "Player";

  [Header("Behavior")]
  [Tooltip("Ignore sticking briefly after a teleport to avoid camera jump.")]
  [SerializeField] private float ignoreAfterTeleportSeconds = 0.25f;

  [Tooltip("Padding to keep sticky active until the player exits by this margin.")]
  [SerializeField] private float exitPadding = 0.25f;

  private Transform _player;
  private Transform _originalFollow;
  private Transform _anchor;
  private bool _stickyActive = false;

  private void Reset()
  {
    if (cinemachineCamera == null)
      cinemachineCamera = GetComponent<CinemachineCamera>();
  }

  private void Awake()
  {
    // Dedupe: keep first instance only
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  private void Start()
  {
    if (cinemachineCamera == null)
      cinemachineCamera = GetComponent<CinemachineCamera>();
    _originalFollow = cinemachineCamera != null ? cinemachineCamera.Follow : null;
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      Instance = null;
    }
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    // Force rebind of player and original follow target after scene change
    _player = null;
    if (cinemachineCamera == null)
      cinemachineCamera = GetComponent<CinemachineCamera>();
    // If the previous follow target was destroyed, clear and reacquire
    if (_originalFollow == null || _originalFollow.gameObject == null)
    {
      _originalFollow = cinemachineCamera != null ? cinemachineCamera.Follow : null;
      if (_originalFollow == null)
      {
        // Try to assign player as follow if nothing set
        var playerGo = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGo != null)
        {
          _originalFollow = playerGo.transform;
          if (cinemachineCamera != null)
            cinemachineCamera.Follow = _originalFollow;
        }
      }
    }
    // Release sticky state across scenes; camera will re-stick if in area
    ReleaseSticky();
  }

  private void Update()
  {
    if (_player == null)
    {
      var go = GameObject.FindGameObjectWithTag(playerTag);
      if (go != null) _player = go.transform;
      if (_player == null) return;
    }

    // Ignore sticky right after teleport
    if (LevelTransitionManager.Instance != null &&
        (Time.unscaledTime - LevelTransitionManager.LastTeleportTime) < ignoreAfterTeleportSeconds)
    {
      ReleaseSticky();
      return;
    }

    var portals = Object.FindObjectsByType<LevelPortal>(FindObjectsSortMode.None);
    if (portals == null || portals.Length == 0)
    {
      ReleaseSticky();
      return;
    }

    var playerPos = _player.position;

    // If we already have a portal, prefer to keep it until exiting with padding
    if (_stickyActive && _anchor != null && _currentPortal != null)
    {
      if (IsInsideStickArea(_currentPortal, playerPos, exitPadding))
      {
        ApplySticky(_currentPortal);
        return;
      }
      else
      {
        _currentPortal = null;
      }
    }

    // Find a portal whose stick area contains the player
    LevelPortal chosen = null;
    float best = float.MaxValue;
    foreach (var p in portals)
    {
      if (p == null || !p.stickEnabled || p.cameraStickAnchor == null) continue;
      if (!IsInsideStickArea(p, playerPos, 0f)) continue;
      float d = (playerPos - p.transform.position).sqrMagnitude;
      if (d < best)
      {
        best = d;
        chosen = p;
      }
    }

    if (chosen != null)
    {
      ApplySticky(chosen);
    }
    else
    {
      ReleaseSticky();
    }
  }

  private LevelPortal _currentPortal;

  private void ApplySticky(LevelPortal portal)
  {
    if (cinemachineCamera == null) return;
    if (portal == null || portal.cameraStickAnchor == null) return;
    _anchor = portal.cameraStickAnchor;
    _currentPortal = portal;

    if (!_stickyActive)
    {
      if (_originalFollow == null)
        _originalFollow = cinemachineCamera.Follow;
      cinemachineCamera.Follow = _anchor;
      _stickyActive = true;
    }
  }

  private void ReleaseSticky()
  {
    if (!_stickyActive) return;
    if (cinemachineCamera != null && _originalFollow != null)
      cinemachineCamera.Follow = _originalFollow;
    _stickyActive = false;
    _currentPortal = null;
  }

  private bool IsInsideStickArea(LevelPortal p, Vector3 worldPos, float padding)
  {
    var center = (Vector2)p.transform.position + p.stickAreaCenter;
    var half = (p.stickAreaSize * 0.5f) + Vector2.one * padding;
    return (worldPos.x >= center.x - half.x && worldPos.x <= center.x + half.x &&
            worldPos.y >= center.y - half.y && worldPos.y <= center.y + half.y);
  }
}
