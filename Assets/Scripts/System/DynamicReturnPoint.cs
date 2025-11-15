using UnityEngine;

public class DynamicReturnPoint : MonoBehaviour
{
    public Transform respawnPoint;
    [Tooltip("If set, respawnPoint will be snapped to this object's world position on validate.")]
    public bool snapRespawnToThis = false;

    public Vector3 GetRespawnPosition()
    {
        return respawnPoint != null ? respawnPoint.position : transform.position;
    }

    void OnValidate()
    {
        // auto-create respawnPoint if missing
        if (respawnPoint == null)
        {
            // create a child named RespawnPoint and place it at this transform
            GameObject go = new GameObject("RespawnPoint");
            go.transform.SetParent(transform, false);
            go.transform.position = transform.position;
            respawnPoint = go.transform;
        }

        if (snapRespawnToThis && respawnPoint != null)
        {
            respawnPoint.position = transform.position;
            snapRespawnToThis = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ReturnPointManager.StartTracking(this);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ReturnPointManager.SetReturnPoint(GetRespawnPosition());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ReturnPointManager.StopTracking(this);
        }
    }

    void OnDrawGizmos()
    {
        // Draw the collider bounds (filled faint + wire)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            // faint fill
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.12f);
            Gizmos.DrawCube(b.center, b.size);
            // outline
            Gizmos.color = new Color(0f, 0.9f, 1f, 1f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
        else
        {
            // fallback: draw small box around transform
            Vector3 center = transform.position;
            Vector3 size = new Vector3(1f, 1f, 0.01f);
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.12f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(0f, 0.9f, 1f, 1f);
            Gizmos.DrawWireCube(center, size);
        }

        // Draw respawn point marker and label
        Vector3 rp = GetRespawnPosition();
        Gizmos.color = new Color(1f, 0f, 1f, 1f);
        Gizmos.DrawSphere(rp, 0.06f);
        Gizmos.color = new Color(1f, 0.6f, 1f, 1f);
        Gizmos.DrawWireSphere(rp, 0.12f);

        // line from area center to respawn point
        Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
        Gizmos.DrawLine(transform.position, rp);
    }
}
