using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    private GameObject player;
    private Collider2D blackZone;
    [SerializeField] private Transform tpPoint;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        blackZone = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = tpPoint.position;
        }
    }
}
