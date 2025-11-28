using UnityEngine;

public class Enemy_Fly : EnemyBase
{
    [Header("References")]
    public LayerMask obstacleLayer;
    public LayerMask boundaryLayer;

    private Fly_Stats typedStats;
    private Enemy_Health enemyHealth;
    private float damageTimer;
    private HealthSystem playerHealth;
    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private float directionTimer;
    private Vector2 startPosition;
    private Vector2 centerPoint;

    void Start()
    {
        typedStats = stats as Fly_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        startPosition = transform.position;
        centerPoint = startPosition;
        PickNewDirection();

        damageTimer = 0f;
    }

    void Update()
    {
        if (typedStats == null) return;

        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0)
        {
            PickNewDirection();
        }

        RaycastHit2D obstacleHit = Physics2D.Raycast(transform.position, currentDirection,
            typedStats.obstacleAvoidanceDistance, obstacleLayer);
        RaycastHit2D boundaryHit = Physics2D.Raycast(transform.position, currentDirection,
            typedStats.obstacleAvoidanceDistance, boundaryLayer);

        if (obstacleHit.collider != null || boundaryHit.collider != null)
        {
            PickNewDirection();
        }

        float distanceFromCenter = Vector2.Distance(transform.position, centerPoint);
        if (distanceFromCenter > typedStats.maxRadius)
        {
            currentDirection = (centerPoint - (Vector2)transform.position).normalized;
            FlipSprite();
        }

        rb.linearVelocity = currentDirection * typedStats.speed;

        if (rb.linearVelocity.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(rb.linearVelocity.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }

        if (damageTimer > 0)
        {
            damageTimer -= Time.deltaTime;
        }
    }

    void PickNewDirection()
    {
        if (typedStats == null) return;
        directionTimer = typedStats.directionChangeInterval;
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
        if (typedStats == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPoint, typedStats.maxRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, currentDirection * typedStats.obstacleAvoidanceDistance);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, currentDirection * typedStats.obstacleAvoidanceDistance);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (typedStats == null) return;

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
                playerHealth.TakeDamage(typedStats.collisionDamage);
                damageTimer = typedStats.damageInterval;
            }
        }
    }
}