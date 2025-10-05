using UnityEngine;

public class VenomShooting : MonoBehaviour
{
    public GameObject venom;
    public Transform shootPoint;
    private GameObject player;
    private float timer;
    public float distanceToPlayer;
    public float meleeRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance < distanceToPlayer)
        {
            timer += Time.deltaTime;

            if (timer > 2)
            {
                timer = 0;
                shoot();
            }
        }
    }

    void shoot()
    {
        Instantiate(venom, shootPoint.position, Quaternion.identity);
    }

    void Bite()
    {
        if (player != null)
        {
            HealthSystem playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(10);
                Debug.Log("Player Hit!");
                Debug.Log(playerHealth.CurrentHealth);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanceToPlayer);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
