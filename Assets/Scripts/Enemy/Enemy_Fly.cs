using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
public class Enemy_Fly : EnemyBase
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public float directionChangeInterval = 2f;
    public float maxRadius = 5f;
    public Vector2 centerPoint;
    public LayerMask obstacleLayer;
    public float obstacleAvoidanceDistance = 1f;

    [Header("Combat")]
    public int collisionDamage = 1;
    public float damageInterval = 0.5f;

    private Fly_Stats typedStats;
    private Enemy_Health enemyHealth;
    private float damageTimer;
    private HealthSystem playerHealth;
    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private float directionTimer;
    private Vector2 startPosition;

    void Start()
    {
        // Get components
        typedStats = stats as Fly_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        rb = GetComponent<Rigidbody2D>();

        // Initialize health
        if (enemyHealth != null && typedStats != null)
        {
            enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.dropA, typedStats.dropB, typedStats.dropC);
        }

        // Setup rigidbody
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Initialize position and movement
        startPosition = transform.position;
        centerPoint = startPosition;
        PickNewDirection();

        // Initialize combat
        damageTimer = 0f;
    }

    void Update()
    {
        // Check if we need to change direction
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0)
        {
            PickNewDirection();
        }

        // Check if we're about to hit something
        RaycastHit2D hit = Physics2D.Raycast(transform.position, currentDirection, obstacleAvoidanceDistance, obstacleLayer);
        if (hit.collider != null)
        {
            PickNewDirection();
        }

        // Check if we're too far from center
        float distanceFromCenter = Vector2.Distance(transform.position, centerPoint);
        if (distanceFromCenter > maxRadius)
        {
            // Turn back towards center
            currentDirection = (centerPoint - (Vector2)transform.position).normalized;
            FlipSprite();
        }

        // Apply movement
        rb.linearVelocity = currentDirection * speed;

        // Update sprite direction
        if (rb.linearVelocity.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(rb.linearVelocity.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    void PickNewDirection()
    {
        directionTimer = directionChangeInterval;
        float randomAngle = Random.Range(0f, 360f);
        currentDirection = new Vector2(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );
    }

    void FlipSprite()
    {
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    void OnDrawGizmos()
    {
        // Draw movement boundary
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPoint, maxRadius);

        // Draw obstacle detection ray
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, currentDirection * obstacleAvoidanceDistance);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (damageTimer <= 0 && collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth == null)
            {
                playerHealth = collision.gameObject.GetComponent<HealthSystem>();
                if (playerHealth == null)
                {
                    playerHealth = collision.gameObject.GetComponentInParent<HealthSystem>();
                }
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(collisionDamage);
                damageTimer = damageInterval;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerHealth = null;
        }
    }
}