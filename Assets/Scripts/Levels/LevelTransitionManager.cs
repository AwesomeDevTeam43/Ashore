using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Central manager for transitioning between portals and scenes.
// Future-proofed for multi-scene Metroidvania structure; supports simple in-scene teleport now.
public class LevelTransitionManager : MonoBehaviour
{
  public static LevelTransitionManager Instance { get; private set; }
  public static float LastTeleportTime { get; private set; }

  [Header("Visuals")]
  [SerializeField] private bool useFade = true;
  [SerializeField] private float fadeOutDuration = 0.2f;
  [SerializeField] private float fadeHoldDuration = 0f;
  [SerializeField] private float fadeInDuration = 0.2f;
  [SerializeField] private bool snapCinemachineOnTeleport = true; // move vcams by player delta to avoid soft drift

  // Local fade overlay to avoid cross-assembly dependencies
  private Canvas _fadeCanvas;
  private UnityEngine.UI.Image _fadeImage;
  private Coroutine _fadeRoutine;

  // Registry of portals by (sceneName, portalId)
  private readonly Dictionary<(string scene, string id), LevelPortal> _portals = new();

  // Transition gating to avoid immediate re-trigger upon arrival
  [SerializeField] private float portalTriggerIgnoreSeconds = 0.5f;
  private float _ignoreUntilTime = 0f;
  private bool _isTransitioning = false;

  // Pending target for cross-scene transitions
  private string _pendingTargetScene;
  private string _pendingTargetPortalId;

  public static void TryEnsureExists()
  {
    if (Instance != null) return;
    // Prefer an author-placed instance in the scene if available
    var existing = Object.FindObjectsByType<LevelTransitionManager>(FindObjectsSortMode.None);
    if (existing != null && existing.Length > 0)
    {
      Instance = existing[0];
      DontDestroyOnLoad(Instance.gameObject);
      return;
    }
    // Otherwise create a default one with code defaults
    var go = new GameObject("LevelTransitionManager");
    Instance = go.AddComponent<LevelTransitionManager>();
    DontDestroyOnLoad(go);
  }

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
    SceneManager.sceneLoaded += OnSceneLoaded;
    // Ensure there is a GameState singleton present
    if (GameState.Instance == null)
    {
      var gs = new GameObject("GameState");
      gs.AddComponent<GameState>();
    }
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }
  }

  private void OnValidate()
  {
    if (fadeOutDuration < 0f) fadeOutDuration = 0f;
    if (fadeInDuration < 0f) fadeInDuration = 0f;
    if (fadeHoldDuration < 0f) fadeHoldDuration = 0f;
  }

  public void RegisterPortal(LevelPortal portal)
  {
    if (portal == null) return;
    var sceneName = portal.gameObject.scene.name;
    var key = (sceneName, portal.portalId);
    _portals[key] = portal;
  }

  public void UnregisterPortal(LevelPortal portal)
  {
    if (portal == null) return;
    var sceneName = portal.gameObject.scene.name;
    var key = (sceneName, portal.portalId);
    if (_portals.ContainsKey(key)) _portals.Remove(key);
  }

  public bool CanAcceptPortalTrigger(LevelPortal from)
  {
    if (_isTransitioning) return false;
    return Time.unscaledTime >= _ignoreUntilTime;
  }

  public void RequestTransition(LevelPortal from)
  {
    if (from == null) return;
    if (_isTransitioning) return;

    _isTransitioning = true;

    // Capture game state before leaving (future-proof for cross-scene)
    GameState.Instance?.CaptureAll();

    var targetScene = string.IsNullOrEmpty(from.targetScene) ? SceneManager.GetActiveScene().name : from.targetScene;
    var targetPortalId = string.IsNullOrEmpty(from.targetPortalId) ? from.portalId : from.targetPortalId;

    if (targetScene == SceneManager.GetActiveScene().name)
    {
      if (useFade)
      {
        StartCoroutine(DoInSceneTeleportWithFade(targetScene, targetPortalId));
      }
      else
      {
        // In-scene teleport (use destination portal's own spawnOffset)
        TeleportInScene(targetScene, targetPortalId);
        _isTransitioning = false;
        _ignoreUntilTime = Time.unscaledTime + portalTriggerIgnoreSeconds;
      }
    }
    else
    {
      // Cross-scene: remember target and load scene
      _pendingTargetScene = targetScene;
      _pendingTargetPortalId = targetPortalId;

      if (useFade) StartFadeOut(fadeOutDuration);
      SceneManager.LoadSceneAsync(targetScene);
    }
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    if (string.IsNullOrEmpty(_pendingTargetScene))
    {
      // No pending transition, just ignore.
      return;
    }

    // Restore states now that scene objects exist
    GameState.Instance?.RestoreAll();

    // Place player (use destination portal's own spawnOffset)
    TeleportInScene(_pendingTargetScene, _pendingTargetPortalId);

    _pendingTargetScene = null;
    _pendingTargetPortalId = null;

    _isTransitioning = false;
    _ignoreUntilTime = Time.unscaledTime + portalTriggerIgnoreSeconds;

    if (useFade) StartFadeIn(fadeInDuration);
  }

  private void TeleportInScene(string sceneName, string targetPortalId)
  {
    var key = (sceneName, targetPortalId);
    if (!_portals.TryGetValue(key, out var dest))
    {
      Debug.LogWarning($"LevelTransitionManager: Destination portal '{targetPortalId}' not found in scene '{sceneName}'.");
      return;
    }

    // Find player
    var player = GameObject.FindGameObjectWithTag("Player");
    if (player == null)
    {
      Debug.LogWarning("LevelTransitionManager: Player not found when attempting teleport.");
      return;
    }

    var oldPlayerPos = player.transform.position;
    var targetPos = (Vector2)dest.transform.position + dest.spawnOffset;
    player.transform.position = new Vector3(targetPos.x, targetPos.y, player.transform.position.z);

    // Optional: zero player velocity to avoid re-trigger or launch issues
    var rb2d = player.GetComponent<Rigidbody2D>();
    if (rb2d != null)
    {
      rb2d.linearVelocity = Vector2.zero;
      rb2d.angularVelocity = 0f;
    }

    LastTeleportTime = Time.unscaledTime;

    if (snapCinemachineOnTeleport)
    {
      var delta = player.transform.position - oldPlayerPos;
      // Move all active virtual cameras by the same delta to reduce large drift
      var vcams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
      foreach (var v in vcams)
      {
        if (v != null) v.transform.position += delta;
      }

      // Also nudge main camera to reduce a visible frame of drift if any
      var cam = Camera.main;
      if (cam != null) cam.transform.position += delta;

      // And zero damping for one frame to truly snap
      StartCoroutine(SnapCinemachineOneFrame());
    }
  }

  private System.Collections.IEnumerator DoInSceneTeleportWithFade(string sceneName, string portalId)
  {
    yield return FadeOutInRoutine(fadeOutDuration, fadeHoldDuration, fadeInDuration, () =>
    {
      TeleportInScene(sceneName, portalId);
    });

    _isTransitioning = false;
    _ignoreUntilTime = Time.unscaledTime + portalTriggerIgnoreSeconds;
  }

  private struct ComposerSnap
  {
    public CinemachinePositionComposer comp;
    public Vector3 damping;
  }

  private System.Collections.IEnumerator SnapCinemachineOneFrame()
  {
    var composers = Object.FindObjectsByType<CinemachinePositionComposer>(FindObjectsSortMode.None);
    var vcams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);

    var compSnaps = new System.Collections.Generic.List<ComposerSnap>(composers.Length);
    foreach (var c in composers)
    {
      if (c == null) continue;
      var snap = new ComposerSnap { comp = c, damping = c.Damping };
      compSnaps.Add(snap);
      c.Damping = Vector3.zero;
    }

  // Disable and re-enable vcams around one frame to force a hard cut recenter
  foreach (var v in vcams) if (v != null) v.enabled = false;
  yield return null;
  foreach (var v in vcams) if (v != null) v.enabled = true;

    foreach (var s in compSnaps)
    {
      if (s.comp != null) s.comp.Damping = s.damping;
    }
  }

  private void EnsureFader()
  {
    if (_fadeCanvas != null && _fadeImage != null) return;
    var go = new GameObject("LevelTransitionFader");
    DontDestroyOnLoad(go);
    _fadeCanvas = go.AddComponent<Canvas>();
    _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    _fadeCanvas.sortingOrder = short.MaxValue;
    go.AddComponent<UnityEngine.UI.CanvasScaler>();
    go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

    var imgGo = new GameObject("Fade");
    imgGo.transform.SetParent(go.transform, false);
    _fadeImage = imgGo.AddComponent<UnityEngine.UI.Image>();
    _fadeImage.color = Color.black;
    var rt = _fadeImage.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    SetFadeAlpha(0f);
  }

  private void SetFadeAlpha(float a)
  {
    if (_fadeImage == null) return;
    var c = _fadeImage.color; c.a = Mathf.Clamp01(a); _fadeImage.color = c;
  }

  private void StartFadeOut(float duration)
  {
    EnsureFader();
    if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
    _fadeRoutine = StartCoroutine(FadeTo(1f, duration));
  }

  private void StartFadeIn(float duration)
  {
    EnsureFader();
    if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
    _fadeRoutine = StartCoroutine(FadeTo(0f, duration));
  }

  private System.Collections.IEnumerator FadeOutInRoutine(float outDur, float hold, float inDur, System.Action middle)
  {
    EnsureFader();
    yield return FadeTo(1f, outDur);
    middle?.Invoke();
    if (hold > 0f) yield return new WaitForSecondsRealtime(hold);
    yield return FadeTo(0f, inDur);
    _fadeRoutine = null;
  }

  private System.Collections.IEnumerator FadeTo(float target, float duration)
  {
    EnsureFader();
    float start = _fadeImage.color.a; float t = 0f;
    while (t < duration)
    {
      t += Time.unscaledDeltaTime;
      float a = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
      SetFadeAlpha(a);
      yield return null;
    }
    SetFadeAlpha(target);
  }
}
