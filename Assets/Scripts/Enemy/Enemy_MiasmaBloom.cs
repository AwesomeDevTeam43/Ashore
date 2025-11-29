using UnityEngine;

public class Enemy_MiasmaBloom : EnemyBase
{
    private enum State { Idle, Charging, Attacking, Cooldown }
    private State state = State.Idle;

    [Header("References")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private Animator animator;
    [SerializeField] private string sideAttackTrigger = "AttackSide";
    [SerializeField] private string upAttackTrigger = "AttackUp";
    [SerializeField] private float upAttackVerticalThreshold = 0.75f;

    private float detectRadius;
    private float attackRange;
    private float attackDelay;
    private float biteRadius;
    private int biteDamage;
    private float attackCooldown;
    [SerializeField] private readonly Color chargeTint = new Color(0.580f, 0f, 0.827f, 1f);

    private MiasmaBloom_Stats typedStats;

    private GameObject player;
    private HealthSystem playerHealth;
    private Collider2D playerCollider;
    private Enemy_Health enemyHealth;
    private SpriteRenderer sr;

    private float stateTimer;
    private Vector2 attackTarget;
    private Color originalColor;
    private float cooldownTimer;
    private bool attackAnimTriggered;

    private void Start()
    {
        typedStats = stats as MiasmaBloom_Stats;

        if (playerMask == 0)
            playerMask = LayerMask.GetMask("Player");

        if (typedStats != null)
        {
            detectRadius = typedStats.playerDetect;
            attackRange = typedStats.attackRange;
            attackDelay = typedStats.attackDelay;
            biteRadius = typedStats.attackRadius;
            biteDamage = typedStats.biteDamage;
            attackCooldown = typedStats.attackCooldown;
        }
        else
        {
            detectRadius = 4f;
            attackRange = 2.5f;
            attackDelay = 0.9f;
            biteRadius = 0.6f;
            biteDamage = 10;
            attackCooldown = 1.2f;
        }

        enemyHealth = GetComponent<Enemy_Health>();
        

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
        else originalColor = Color.white;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>() ?? player.GetComponentInChildren<HealthSystem>();
            playerCollider = player.GetComponent<Collider2D>() ?? player.GetComponentInChildren<Collider2D>();
        }

        if (typedStats != null)
        {
            transform.localScale = typedStats.baseScale;
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                DoIdle();
                break;
            case State.Charging:
                DoCharging();
                break;
            case State.Attacking:
                DoAttack();
                break;
            case State.Cooldown:
                DoCooldown();
                break;
        }

        FacePlayer();
    }

    private void DoIdle()
    {
        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (cooldownTimer <= 0f && dist <= detectRadius && dist <= attackRange)
        {
            attackTarget = GetPlayerFeetPosition();
            stateTimer = attackDelay;
            state = State.Charging;
            attackAnimTriggered = false;
            if (sr != null) sr.color = chargeTint;
        }
    }

    private void DoCharging()
    {
        if (!attackAnimTriggered)
        {
            TriggerAttackAnimation();
            attackAnimTriggered = true;
        }
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            state = State.Attacking;
        }
    }

    private void DoAttack()
    {
        bool hitPlayer = false;

        Collider2D[] cols = Physics2D.OverlapCircleAll(attackTarget, biteRadius, playerMask);
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null) continue;

            if (c.gameObject.CompareTag("Player") || c.transform.root.CompareTag("Player"))
            {
                hitPlayer = true;
                break;
            }
        }

        if (hitPlayer)
        {
            if (playerHealth == null && player != null)
                playerHealth = player.GetComponent<HealthSystem>() ?? player.GetComponentInChildren<HealthSystem>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage((int)currentDamage);
            }
            else
            {
                Debug.LogWarning("Enemy_MiasmaBloom: Player hit but HealthSystem is missing on player.");
            }
        }

        if (sr != null) sr.color = originalColor;
        cooldownTimer = attackCooldown;
        state = State.Cooldown;
    }

    private void DoCooldown()
    {
        if (cooldownTimer <= 0f)
        {
            state = State.Idle;
        }
    }

    private Vector3 GetPlayerFeetPosition()
    {
        if (playerCollider != null)
        {
            Bounds b = playerCollider.bounds;
            return new Vector3(b.center.x, b.min.y, b.center.z);
        }
        return player.transform.position + Vector3.down * 0.5f;
    }

    public void TakeDamage(int amount)
    {
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(amount);
            return;
        }
        Debug.LogWarning("Enemy_MiasmaBloom: TakeDamage called but Enemy_Health is missing.");
    }

    void FacePlayer()
    {
        if (player == null) return;
        float dir = (player.transform.position.x >= transform.position.x) ? 1f : -1f;
        Vector3 s = stats.baseScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    private void TriggerAttackAnimation()
    {
        if (animator == null) return;
        var trigger = DetermineAttackTrigger();
        if (string.IsNullOrEmpty(trigger)) return;
        animator.SetTrigger(trigger);
    }

    private string DetermineAttackTrigger()
    {
        if (string.IsNullOrEmpty(upAttackTrigger) && string.IsNullOrEmpty(sideAttackTrigger))
            return null;

        float targetHeight = attackTarget.y;
        float selfHeight = transform.position.y;
        bool isUpAttack = (targetHeight - selfHeight) >= upAttackVerticalThreshold;

        if (isUpAttack)
        {
            return string.IsNullOrEmpty(upAttackTrigger) ? sideAttackTrigger : upAttackTrigger;
        }
        return sideAttackTrigger;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (Application.isPlaying && state == State.Charging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackTarget, biteRadius);
            Gizmos.DrawLine(transform.position, attackTarget);
        }

        if (state == State.Charging || state == State.Attacking)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackTarget, biteRadius);

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawLine(transform.position, attackTarget);
        }
    }
}