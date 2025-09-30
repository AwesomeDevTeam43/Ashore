using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask deadZoneLayer;

    void Update()
    {
        if (IsDeadZone())
        {
            player.transform.localPosition = spawnPoint.transform.localPosition;
        }
    }

    private bool IsDeadZone()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.02f, deadZoneLayer);
    }
}
