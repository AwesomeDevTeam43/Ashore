using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask deadZoneLayer;

    void Update()
    {
        if (IsDeadZone() && player != null && spawnPoint != null)
        {
            player.transform.localPosition = spawnPoint.transform.localPosition;
        }
    }

    private bool IsDeadZone()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(groundCheck.position, 0.02f, deadZoneLayer);
    }
}