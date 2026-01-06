using UnityEngine;

// Place on a trigger collider that represents a level exit/entrance.
// It can both trigger a transition and act as an arrival point (using portalId).
[RequireComponent(typeof(Collider2D))]
public class LevelPortal : MonoBehaviour
{
  [Tooltip("Unique ID for this portal within its scene.")]
  public string portalId;

  [Tooltip("Destination scene name. Leave empty to stay in current scene.")]
  public string targetScene;

  [Tooltip("ID of the destination portal in the target scene (or current scene if targetScene is empty).")]
  public string targetPortalId;

  [Tooltip("Optional spawn offset applied at destination relative to target portal position.")]
  public Vector2 spawnOffset;

  [Header("Camera Stick (Manual)")]
  [Tooltip("Enable camera stick behavior near this portal.")]
  public bool stickEnabled = true;

  [Tooltip("Stick area center relative to this portal (world XY, unrotated).")]
  public Vector2 stickAreaCenter = Vector2.zero;

  [Tooltip("Stick area size (width x height) around the center.")]
  public Vector2 stickAreaSize = new Vector2(4f, 3f);

  [Tooltip("Camera will follow this anchor while the player is inside the stick area.")]
  public Transform cameraStickAnchor;

  [Header("Sticky Zoom Override")]
  [Tooltip("If true, this portal supplies a custom zoom value while the camera is stuck.")]
  public bool overrideStickyZoom = false;

  [Tooltip("Target orthographic size for the camera while stuck (ignored if the main camera is perspective).")]
  public float portalStickyOrthographicSize = 4.5f;

  [Tooltip("Target field of view for the camera while stuck (ignored if the main camera is orthographic).")]
  public float portalStickyFieldOfView = 40f;

  private Collider2D col;

  private void Reset()
  {
    col = GetComponent<Collider2D>();
    col.isTrigger = true;
    if (string.IsNullOrEmpty(portalId))
      portalId = gameObject.name; // default to name as id
  }

  private void OnEnable()
  {
    LevelTransitionManager.TryEnsureExists();
    LevelTransitionManager.Instance?.RegisterPortal(this);
  }

  private void OnDisable()
  {
    LevelTransitionManager.Instance?.UnregisterPortal(this);
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (!other.CompareTag("Player")) return;
    if (LevelTransitionManager.Instance == null) return;
    if (!LevelTransitionManager.Instance.CanAcceptPortalTrigger(this)) return;

    LevelTransitionManager.Instance.RequestTransition(this);
  }

#if UNITY_EDITOR
  private void OnDrawGizmos()
  {
    // Always-on, dimmer gizmos for portal aids
    // Stick area (dim)
    Gizmos.color = new Color(0f, 1f, 1f, 0.08f);
    var center = (Vector2)transform.position + stickAreaCenter;
    var size3 = new Vector3(stickAreaSize.x, stickAreaSize.y, 0.02f);
    Gizmos.DrawCube(center, size3);

    // Teleport spawn offset (dim)
    Gizmos.color = new Color(1f, 0f, 1f, 0.25f);
    var from = transform.position;
    var to = (Vector2)transform.position + spawnOffset;
    Gizmos.DrawLine(from, to);
    Gizmos.DrawSphere(to, 0.06f);
  }

  private void OnDrawGizmosSelected()
  {
    // Stick area gizmo
    Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
    var center = (Vector2)transform.position + stickAreaCenter;
    var size3 = new Vector3(stickAreaSize.x, stickAreaSize.y, 0.02f);
    Gizmos.DrawCube(center, size3);
    Gizmos.color = new Color(0f, 0.8f, 0.8f, 1f);
    Gizmos.DrawWireCube(center, size3);

    // Camera anchor gizmo
    if (cameraStickAnchor != null)
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawSphere(cameraStickAnchor.position, 0.12f);
      Gizmos.DrawLine(transform.position, cameraStickAnchor.position);
    }

    // Teleport spawn offset gizmo
    Gizmos.color = new Color(1f, 0f, 1f, 0.6f);
    var from = transform.position;
    var to = (Vector2)transform.position + spawnOffset;
    Gizmos.DrawLine(from, to);
    Gizmos.DrawSphere(to, 0.1f);
  }
#endif
}
