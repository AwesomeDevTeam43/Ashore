using UnityEngine;

/// <summary>
/// EXEMPLO: Enemy_Fly com Algoritmo Genético integrado.
/// 
/// Esta é uma versão modificada do Enemy_Fly que usa o sistema de evolução genética.
/// Os inimigos vão evoluir automaticamente baseado no desempenho contra o jogador.
/// 
/// DIFERENÇAS DO ORIGINAL:
/// 1. Herda de GeneticEnemyBase em vez de EnemyBase
/// 2. Usa GetGeneticMovementSpeed() para velocidade
/// 3. Usa GetScaledDamage() que inclui genes
/// 4. Registra dano causado com RegisterDamageToPlayer()
/// </summary>
public class Enemy_Fly_Genetic : GeneticEnemyBase
{
    [Header("References")]
    public LayerMask obstacleLayer;
    public LayerMask boundaryLayer;

    private Fly_Stats typedStats;
    private float damageTimer;
    private HealthSystem playerHealth;
    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private float directionTimer;
    private Vector2 startPosition;
    private Vector2 centerPoint;
    
    // Cache de valores genéticos
    private float geneticSpeed;
    private float geneticMaxRadius;

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
        
        // ========== INTEGRAÇÃO GENÉTICA ==========
        // Aplica modificadores genéticos aos stats base
        if (typedStats != null)
        {
            geneticSpeed = GetGeneticMovementSpeed(typedStats.speed);
            geneticMaxRadius = GetGeneticAggressionRange(typedStats.maxRadius);
            
            if (debugGeneticStats)
            {
                Debug.Log($"🧬 [{name}] Genetic Stats: Speed={geneticSpeed:F2} (base: {typedStats.speed}), " +
                          $"Radius={geneticMaxRadius:F2} (base: {typedStats.maxRadius})");
            }
        }
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
        
        // ========== USA RAIO GENÉTICO ==========
        if (distanceFromCenter > geneticMaxRadius)
        {
            currentDirection = (centerPoint - (Vector2)transform.position).normalized;
            FlipSprite();
        }

        // ========== USA VELOCIDADE GENÉTICA ==========
        rb.linearVelocity = currentDirection * geneticSpeed;

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
        
        // ========== DIREÇÃO INFLUENCIADA PELA AGRESSIVIDADE ==========
        // Inimigos mais agressivos tendem a ir em direção ao jogador
        float aggressiveness = GetAggressiveness();
        
        directionTimer = typedStats.directionChangeInterval;
        
        if (aggressiveness > 0.6f && playerHealth != null)
        {
            // Tem chance de ir em direção ao jogador
            if (Random.value < aggressiveness)
            {
                Vector2 toPlayer = ((Vector2)playerHealth.transform.position - (Vector2)transform.position).normalized;
                currentDirection = toPlayer;
                FlipSprite();
                return;
            }
        }
        
        // Direção aleatória normal
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

        // ========== MOSTRA RAIO GENÉTICO ==========
        Gizmos.color = Color.yellow;
        float radius = Application.isPlaying ? geneticMaxRadius : typedStats.maxRadius;
        Gizmos.DrawWireSphere(centerPoint, radius);

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
                // ========== USA DANO GENÉTICO E REGISTRA ==========
                int damage = GetScaledDamage();
                playerHealth.TakeDamage(damage);
                damageTimer = typedStats.damageInterval;
                
                // Registra o dano para o sistema de fitness
                RegisterDamageToPlayer(damage);
                
                if (debugGeneticStats)
                {
                    Debug.Log($"🧬 [{name}] Dealt {damage} damage (base: {typedStats.collisionDamage})");
                }
            }
        }
    }
}
