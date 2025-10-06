using UnityEngine;

public class VenomShooting : MonoBehaviour
{
    public GameObject venom;
    public Transform shootPoint;
    private GameObject player;
    private float timer;
    public float distanceToPlayer;
    public float meleeRange;
    private HealthSystem healthSystem;
    private XP_System xP_System;
    public int enemyHealth;


    void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.OnHealthChanged += OnHealthChanged;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        healthSystem.Initialize(enemyHealth);
        if (player != null)
        {
            xP_System = player.GetComponent<XP_System>();
        }
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
            //Bite();
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
                playerHealth.TakeDamage(5);
                Debug.Log("Player Hit!");
                Debug.Log(playerHealth.CurrentHealth);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanceToPlayer);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }

    void OnHealthChanged(int current, int max)
    {
        Debug.Log("Serpent Health: " + current + "/" + max);
        if (current <= 0)
        {
            Destroy(gameObject);
            if (xP_System != null)
            {
                xP_System.DropXP(transform.position, 5);
            }
        }
    }
}
