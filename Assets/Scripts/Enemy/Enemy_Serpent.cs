using UnityEngine;

public class VenomShooting : MonoBehaviour
{
    public Serpent_Stats stats;
    public GameObject venom;
    public Transform shootPoint;

    private Enemy_Health enemyHealth;
    private GameObject player;
    private float timer;
    private float timeBtwAttack;
    private bool inRange;

    void Start()
    {
        enemyHealth = GetComponent<Enemy_Health>();
        player = GameObject.FindGameObjectWithTag("Player");

        if (stats == null)
        {
            Debug.LogWarning("VenomShooting: Serpent_Stats not assigned -> disabling script.");
            enabled = false;
            return;
        }

        if (enemyHealth != null)
        {
            enemyHealth.Initialize(stats.maxHealth, stats.xpOnDeath, stats.dropA, stats.dropB, stats.dropC);
        }

        transform.localScale = stats.baseScale;
        timeBtwAttack = 0f;
    }

    void Update()
    {
        if (player == null || stats == null) return;

        float distance = Vector2.Distance(player.transform.position, transform.position);

        if (distance < stats.meleeRange)
        {
            inRange = true;
            Bite();
        }
        else
        {
            inRange = false;
            if (distance < stats.distanceToPlayer)
            {
                timer += Time.deltaTime;
                if (timer > 2f)
                {
                    timer = 0f;
                    Shoot();
                }
            }
        }

        if (timeBtwAttack > 0f) timeBtwAttack -= Time.deltaTime;
    }

    void Shoot()
    {
        if (venom != null && shootPoint != null)
            Instantiate(venom, shootPoint.position, Quaternion.identity);
    }

    void Bite()
    {
        if (player == null || !inRange) return;

        if (timeBtwAttack <= 0f)
        {
            if (player.TryGetComponent<HealthSystem>(out var ph))
            {
                if (stats.biteDamage <= 0) Debug.LogWarning("VenomShooting: biteDamage <= 0");
                ph.TakeDamage(stats.biteDamage);
            }
            else
            {
                var phParent = player.GetComponentInParent<HealthSystem>();
                if (phParent != null) phParent.TakeDamage(stats.biteDamage);
                else Debug.LogWarning("VenomShooting: Player HealthSystem not found.");
            }

            timeBtwAttack = stats.startTimeBtwAttack;
        }
    }

    void OnDrawGizmos()
    {
        if (stats == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.distanceToPlayer);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stats.meleeRange);
    }
}