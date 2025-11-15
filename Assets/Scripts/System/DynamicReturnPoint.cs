using UnityEngine;

public class DynamicReturnPoint : MonoBehaviour
{
    public Transform respawnPoint;

    public Vector3 GetRespawnPosition()
    {
        return respawnPoint != null ? respawnPoint.position : transform.position;
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
}
