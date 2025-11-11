using UnityEngine;

public class VenomShooting : EnemyBase
{
    private Serpent_Stats typedStats;
    public GameObject venomPrefab;
    public Transform shootPoint;
    private Enemy_Health enemyHealth;
    private Transform player;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool useAttackTrigger = true;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string attackBool = "isAttacking";

    public enum EnemyState { Idle, Biting, PreparingToShoot, Shooting }
    private EnemyState currentState;
    private float meleeCooldownTimer;
    private float shootCooldownTimer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public Color chargeColor = Color.green;
    private float currentChargeTime;
    public const float chargeDuration = 0.5f;
    public float animationAttackCooldown = 1.0f;

    void Start()
    {
        typedStats = stats as Serpent_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        if (stats == null)
        {
            enabled = false;
            return;
        }
        if (enemyHealth != null && typedStats != null)
        {
            enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.dropA, typedStats.dropB, typedStats.dropC);
        }
        transform.localScale = stats.baseScale;
        meleeCooldownTimer = 0f;
        shootCooldownTimer = 0f;
        currentState = EnemyState.Idle;
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (player == null || stats == null) return;
        float distance = Vector2.Distance(player.position, transform.position);
        if (meleeCooldownTimer > 0f) meleeCooldownTimer -= Time.deltaTime;
        if (shootCooldownTimer > 0f) shootCooldownTimer -= Time.deltaTime;
        HandleStateTransitions(distance);
        HandleStateActions(distance);
        if (currentState != EnemyState.Idle)
        {
            FacePlayer();
        }
    }

    void HandleStateTransitions(float distance)
    {
        if (distance < typedStats.meleeRange)
        {
            currentState = EnemyState.Biting;
            return;
        }
        if (currentState == EnemyState.Biting)
        {
            currentState = EnemyState.Idle;
        }
        if (currentState == EnemyState.PreparingToShoot || currentState == EnemyState.Shooting)
        {
            return;
        }
        if (distance < typedStats.distanceToPlayer)
        {
            if (shootCooldownTimer <= 0f)
            {
                currentState = EnemyState.PreparingToShoot;
                currentChargeTime = chargeDuration;
                return;
            }
            else
            {
                currentState = EnemyState.Idle;
            }
        }
        else
        {
            currentState = EnemyState.Idle;
        }
    }

    void HandleStateActions(float distance)
    {
        switch (currentState)
        {
            case EnemyState.Biting:
                Bite();
                break;
            case EnemyState.PreparingToShoot:
                currentChargeTime -= Time.deltaTime;
                float t = 1f - (currentChargeTime / GetChargeDuration());
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(originalColor, chargeColor, t);
                }
                if (currentChargeTime <= 0f)
                {
                    currentState = EnemyState.Shooting;
                }
                break;
            case EnemyState.Shooting:
                if (animator != null)
                {
                    if (useAttackTrigger)
                    {
                        animator.SetTrigger(attackTrigger);
                    }
                    else
                    {
                        animator.SetBool(attackBool, true);
                    }
                    shootCooldownTimer = Mathf.Max(shootCooldownTimer, animationAttackCooldown);
                }
                else
                {
                    Shoot();
                    shootCooldownTimer = typedStats.startTimeBtwAttack;
                }
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
                currentState = EnemyState.Idle;
                break;
            case EnemyState.Idle:
                break;
        }
    }

    private float GetChargeDuration()
    {
        return chargeDuration;
    }

    public void Shoot()
    {
        if (venomPrefab != null && shootPoint != null)
        {
            Instantiate(venomPrefab, shootPoint.position, Quaternion.identity);
        }
        if (animator != null && !useAttackTrigger)
        {
            animator.SetBool(attackBool, false);
        }
    }

    void Bite()
    {
        if (meleeCooldownTimer <= 0f)
        {
            if (player.TryGetComponent<HealthSystem>(out var ph) || (ph = player.GetComponentInParent<HealthSystem>()) != null)
            {
                if (typedStats.biteDamage <= 0) Debug.LogWarning("VenomShooting: biteDamage <= 0");
                ph.TakeDamage(typedStats.biteDamage);
                Debug.Log("Serpent Bite! Player hit.");
            }
            else
            {
                Debug.LogWarning("VenomShooting: Player HealthSystem not found.");
            }
            meleeCooldownTimer = typedStats.startTimeBtwAttack;
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        float dir = (player.position.x >= transform.position.x) ? 1f : -1f;
        Vector3 s = stats.baseScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    void OnDrawGizmos()
    {
        if (stats == null) return;
        if (typedStats == null)
        {
            typedStats = stats as Serpent_Stats;
            if (typedStats == null) return;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, typedStats.distanceToPlayer);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, typedStats.meleeRange);
    }
}