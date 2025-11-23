using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Enemy_Health))]
public class Enemy_RuinsBoss : EnemyBase
{
    public RuinBoss_Stats typedStats;
    private Enemy_Health enemyHealth;
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;

    [SerializeField] private string walkBoolName = "isWalking";
    [Header("Animation")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string attackBoolName = "isAttacking";
    [SerializeField] private bool attackUsesAnimationEvent = true;
    [SerializeField] private float attackCommitDuration = 0.8f;
    [Header("Targeting")]
    [SerializeField] private LayerMask playerMask;

    private bool hasAttackTrigger = false;
    private bool hasAttackBool = false;
    private bool attackInProgress = false;
    private Coroutine attackCommitRoutine;

    public enum EnemyState { Idle, Chasing, Attacking }
    private EnemyState currentState;

    private float timeBtwAttack;

    private void Start()
    {
        typedStats = stats as RuinBoss_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (!string.IsNullOrEmpty(attackTriggerName) && !hasAttackTrigger && p.type == AnimatorControllerParameterType.Trigger && p.name == attackTriggerName)
                    hasAttackTrigger = true;
                if (!string.IsNullOrEmpty(attackBoolName) && !hasAttackBool && p.type == AnimatorControllerParameterType.Bool && p.name == attackBoolName)
                    hasAttackBool = true;
            }
        }

        if (enemyHealth != null && stats != null)
        {
            enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.woodDrop, typedStats.stoneDrop, typedStats.ropeDrop);
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        if (playerMask == 0)
        {
            playerMask = LayerMask.GetMask("Player");
        }

        if (stats != null)
        {
            timeBtwAttack = 0f;
        }
        currentState = EnemyState.Idle;
    }

    private void Update()
    {
        if (player == null || stats == null)
        {
            currentState = EnemyState.Idle;
            return;
        }

        HandleStateTransitions();

        if (timeBtwAttack > 0)
        {
            timeBtwAttack -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        HandleStateActions();
    }

    private void HandleStateTransitions()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        switch (currentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer <= typedStats.followPlayerRange)
                {
                    currentState = EnemyState.Chasing;
                }
                break;
            case EnemyState.Chasing:
                if (distanceToPlayer <= typedStats.attackRange)
                {
                    currentState = EnemyState.Attacking;
                }
                else if (distanceToPlayer > typedStats.followPlayerRange)
                {
                    currentState = EnemyState.Idle;
                }
                break;
            case EnemyState.Attacking:
                if (!attackInProgress && distanceToPlayer > typedStats.attackRange)
                {
                    currentState = EnemyState.Chasing;
                }
                break;
        }
    }

    private void HandleStateActions()
    {
        if (player == null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (animator != null) animator.SetBool(walkBoolName, false);
            return;
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                if (animator != null) animator.SetBool(walkBoolName, false);
                if (animator != null && hasAttackBool) animator.SetBool(attackBoolName, false);
                break;

            case EnemyState.Chasing:
                float moveDirection = (player.position.x > transform.position.x) ? 1f : -1f;
                rb.linearVelocity = new Vector2(moveDirection * typedStats.moveSpeed, rb.linearVelocity.y);
                FlipSpriteOnMove();
                if (animator != null) animator.SetBool(walkBoolName, true);
                if (animator != null && hasAttackBool) animator.SetBool(attackBoolName, false);
                break;

            case EnemyState.Attacking:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                FacePlayer();
                if (animator != null && hasAttackBool) animator.SetBool(attackBoolName, attackInProgress);
                AttemptAttack();
                if (animator != null) animator.SetBool(walkBoolName, false);
                break;
        }
    }

    private void AttemptAttack()
    {
        if (timeBtwAttack <= 0)
        {
            timeBtwAttack = (typedStats.startTimeBtwAttack <= 0) ? 2f : typedStats.startTimeBtwAttack;
            BeginAttackCommit(maybeTimedFallback: true);
            if (attackUsesAnimationEvent)
            {
                if (animator != null)
                {
                    if (hasAttackTrigger)
                    {
                        animator.SetTrigger(attackTriggerName);
                    }
                    else if (hasAttackBool)
                    {
                        // handled by setting the bool elsewhere
                    }
                    else
                    {
                        // Fallback
                        DoMeleeCheck();
                    }
                }
                else
                {
                    DoMeleeCheck();
                }
            }
            else
            {
                DoMeleeCheck();
            }
        }
    }

    private void BeginAttackCommit(bool maybeTimedFallback)
    {
        attackInProgress = true;
        bool expectAnimToEnd = attackUsesAnimationEvent && animator != null && (hasAttackTrigger || hasAttackBool);
        if (!expectAnimToEnd && maybeTimedFallback)
        {
            if (attackCommitRoutine != null) StopCoroutine(attackCommitRoutine);
            attackCommitRoutine = StartCoroutine(AttackCommitWindow(attackCommitDuration));
        }
    }

    private IEnumerator AttackCommitWindow(float duration)
    {
        yield return new WaitForSeconds(duration);
        attackInProgress = false;
        attackCommitRoutine = null;
    }

    // Animation event: perform the damage (if using child collider this can be a no-op or used to toggle)
    public void AnimEvent_DoAttack()
    {
        DoMeleeCheck();
    }

    public void AnimEvent_AttackStart()
    {
        if (attackCommitRoutine != null)
        {
            StopCoroutine(attackCommitRoutine);
            attackCommitRoutine = null;
        }
        attackInProgress = true;
    }

    public void AnimEvent_AttackEnd()
    {
        attackInProgress = false;
        if (attackCommitRoutine != null)
        {
            StopCoroutine(attackCommitRoutine);
            attackCommitRoutine = null;
        }
    }

    // Called by the boss or by the sword collider when a hit should be applied
    public void DoMeleeHit(Transform target)
    {
        if (target == null || !IsPlayerTransform(target)) return;

        HealthSystem targetHealth = target.GetComponent<HealthSystem>();
        Rigidbody2D targetRb = null;
        if (targetHealth == null)
        {
            targetHealth = target.GetComponentInParent<HealthSystem>();
            if (targetHealth != null)
            {
                targetRb = targetHealth.GetComponent<Rigidbody2D>();
            }
        }
        else
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(typedStats.attackDamage);
            if (targetRb != null)
            {
                StartCoroutine(ApplyPlayerKnockback(targetRb, target));
            }
        }
    }

    private void DoMeleeCheck()
    {
        if (player == null) return;
        // fallback melee check: overlap circle centered on boss to catch targets
        int mask = playerMask.value != 0 ? playerMask.value : LayerMask.GetMask("Player");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, typedStats.attackRange, mask);
        foreach (var h in hits)
        {
            if (!IsPlayerCollider(h)) continue;
            HealthSystem hs = h.GetComponent<HealthSystem>() ?? h.GetComponentInParent<HealthSystem>();
            if (hs != null)
            {
                hs.TakeDamage(typedStats.attackDamage);
                Rigidbody2D prb = hs.GetComponent<Rigidbody2D>();
                if (prb != null)
                {
                    StartCoroutine(ApplyPlayerKnockback(prb, h.transform));
                }
            }
        }
    }

    private IEnumerator ApplyPlayerKnockback(Rigidbody2D playerRb, Transform playerTransform)
    {
        if (playerRb == null || stats == null) yield break;

        Transform attackOrigin = transform;

        Vector2 dir = (playerTransform.position - attackOrigin.position).normalized;

        playerRb.linearVelocity = Vector2.zero;

        playerRb.AddForce(dir * typedStats.knockbackForce, ForceMode2D.Impulse);

        float t = 0f;

        Vector2 startVel = playerRb.linearVelocity;

        while (t < typedStats.knockbackDuration && playerRb != null)
        {
            playerRb.linearVelocity = Vector2.Lerp(startVel, Vector2.zero, t / typedStats.knockbackDuration);
            t += Time.deltaTime;
            yield return null;
        }

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    void FlipSpriteOnMove()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            float dir = Mathf.Sign(rb.linearVelocity.x);
            Vector3 s = stats.baseScale;
            s.x = Mathf.Abs(s.x) * dir;
            transform.localScale = s;
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
    
    public bool IsAttackInProgress() => attackInProgress;

    private static bool IsPlayerCollider(Collider2D col)
    {
        if (col == null) return false;
        if (col.CompareTag("Player")) return true;
        Transform root = col.transform.root;
        return root != null && root.CompareTag("Player");
    }

    private static bool IsPlayerTransform(Transform t)
    {
        if (t == null) return false;
        if (t.CompareTag("Player")) return true;
        Transform root = t.root;
        return root != null && root.CompareTag("Player");
    }
}
