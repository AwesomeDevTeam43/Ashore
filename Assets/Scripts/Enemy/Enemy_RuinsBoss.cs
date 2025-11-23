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
    private Collider2D col2d;

    [SerializeField] private string walkBoolName = "isWalking";
    [Header("Animation")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string attackBoolName = "isAttacking";
    [SerializeField] private bool attackUsesAnimationEvent = true;
    [SerializeField] private float attackCommitDuration = 0.8f;
    [Header("Targeting")]
    [SerializeField] private LayerMask playerMask;
    [Header("Navigation")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float hopCooldownTime = 0.3f;
    [SerializeField] private bool drawNavigationGizmos = true;

    private bool hasAttackTrigger = false;
    private bool hasAttackBool = false;
    private bool attackInProgress = false;
    private Coroutine attackCommitRoutine;

    public enum EnemyState { Idle, Chasing, Attacking, Returning }
    private EnemyState currentState;

    private float timeBtwAttack;
    private float hopCooldown;
    private float stuckTimer;
    private float lastX;
        private Vector3 spawnPosition;
    private const float stuckEpsilon = 0.01f;
    private PhysicsMaterial2D originalMaterial;
    private PhysicsMaterial2D lowFrictionMaterial;
    private float defaultDamping;

    private void Start()
    {
        typedStats = stats as RuinBoss_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        col2d = GetComponent<Collider2D>();

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
            enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.woodDrop, typedStats.stoneDrop, typedStats.ropeDrop, typedStats.meleeResistance, typedStats.rangedResistance);
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

        if (groundMask == 0)
        {
            groundMask = LayerMask.GetMask("Ground");
            if (groundMask == 0)
            {
                groundMask = ~0;
            }
        }

        if (obstacleMask == 0)
        {
            obstacleMask = groundMask;
        }

        if (stats != null)
        {
            timeBtwAttack = 0f;
        }
        currentState = EnemyState.Idle;
        spawnPosition = transform.position;

        if (rb != null)
        {
            rb.freezeRotation = true;
            defaultDamping = rb.linearDamping;
        }

        if (col2d != null)
        {
            originalMaterial = col2d.sharedMaterial;
            lowFrictionMaterial = new PhysicsMaterial2D("RuinsBossLowFriction") { friction = 0f, bounciness = 0f };
        }

        lastX = transform.position.x;
    }

    private void OnDrawGizmos()
    {
        if (!drawNavigationGizmos) return;
        DrawNavigationGizmos();
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
        UpdateFrictionAndDrag();
        TrackStuck();
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
                    currentState = EnemyState.Returning;
                }
                break;
            case EnemyState.Attacking:
                if (!attackInProgress && distanceToPlayer > typedStats.attackRange)
                {
                    currentState = EnemyState.Chasing;
                }
                else if (distanceToPlayer > typedStats.followPlayerRange)
                {
                    currentState = EnemyState.Returning;
                    attackInProgress = false;
                }
                break;
            case EnemyState.Returning:
                if (distanceToPlayer <= typedStats.attackRange)
                {
                    currentState = EnemyState.Attacking;
                }
                else if (distanceToPlayer <= typedStats.followPlayerRange)
                {
                    currentState = EnemyState.Chasing;
                }
                else if (Vector2.Distance(transform.position, spawnPosition) <= 0.25f)
                {
                    currentState = EnemyState.Idle;
                    enemyHealth?.RestoreFullHealth();
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
                TryStepOrTallJump(moveDirection);
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

            case EnemyState.Returning:
                float returnDir = Mathf.Sign(spawnPosition.x - transform.position.x);
                TryStepOrTallJump(returnDir);
                rb.linearVelocity = new Vector2(returnDir * typedStats.moveSpeed, rb.linearVelocity.y);
                if (Mathf.Abs(spawnPosition.x - transform.position.x) <= 0.05f)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    transform.position = new Vector3(spawnPosition.x, transform.position.y, transform.position.z);
                }
                FlipSpriteOnDirection(returnDir);
                if (animator != null) animator.SetBool(walkBoolName, true);
                if (animator != null && hasAttackBool) animator.SetBool(attackBoolName, false);
                break;
        }
    }

    private void FlipSpriteOnDirection(float dir)
    {
        if (Mathf.Abs(dir) < 0.01f) return;
        Vector3 s = stats.baseScale;
        s.x = Mathf.Abs(s.x) * (dir >= 0 ? 1f : -1f);
        transform.localScale = s;
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

    private void DrawNavigationGizmos()
    {
        Gizmos.color = Color.yellow;
        float detectRange = typedStats != null ? typedStats.followPlayerRange : 6f;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        float attackRange = typedStats != null ? typedStats.attackRange : 1.5f;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Collider2D gizmoCol = col2d != null ? col2d : GetComponent<Collider2D>();
        Bounds b = gizmoCol != null ? gizmoCol.bounds : new Bounds(transform.position, Vector3.one * 1f);
        float dir = 1f;
        if (Application.isPlaying && player != null)
        {
            dir = Mathf.Sign(player.position.x - transform.position.x);
            if (Mathf.Approximately(dir, 0f)) dir = 1f;
        }
        else
        {
            dir = transform.localScale.x >= 0 ? 1f : -1f;
        }

        float frontX = b.center.x + dir * (b.extents.x + 0.02f);
        float footY = b.min.y + 0.02f;
        float checkDist = typedStats != null ? typedStats.hopCheckDistance : 0.45f;
        float stepH = typedStats != null ? typedStats.maxStepHeight : 0.6f;
        float chestH = typedStats != null ? typedStats.chestHeight : (GetHalfHeight() * 0.6f);
        Vector3 forward = new Vector3(dir * checkDist, 0f, 0f);

        Gizmos.color = Color.cyan;
        Vector3 lowStart = new Vector3(frontX, footY, 0f);
        Gizmos.DrawLine(lowStart, lowStart + forward);

        Vector3 midStart = new Vector3(frontX, footY + chestH, 0f);
        Gizmos.DrawLine(midStart, midStart + forward);

        Vector3 highStart = new Vector3(frontX, footY + stepH, 0f);
        Gizmos.DrawLine(highStart, highStart + forward);

        float headStartY = b.center.y + b.extents.y + 0.5f;
        Vector3 downStartCurr = new Vector3(frontX - dir * 0.02f, headStartY, 0f);
        Vector3 downEndCurr = downStartCurr + Vector3.down * (b.size.y + 2f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(downStartCurr, downEndCurr);

        Vector3 downStartAhead = new Vector3(frontX + dir * checkDist, headStartY, 0f);
        Vector3 downEndAhead = downStartAhead + Vector3.down * (b.size.y + 2f);
        Gizmos.DrawLine(downStartAhead, downEndAhead);
    }

    private void TryStepOrTallJump(float dir)
    {
        if (!IsGrounded()) return;
        if (hopCooldown > 0f)
        {
            hopCooldown -= Time.fixedDeltaTime;
            return;
        }

        Bounds b = col2d != null ? col2d.bounds : new Bounds(transform.position, Vector3.one);
        float frontX = b.center.x + dir * (b.extents.x + 0.02f);
        float footY = b.min.y + 0.02f;
        float checkDist = typedStats != null ? typedStats.hopCheckDistance : 0.45f;
        float stepH = typedStats != null ? typedStats.maxStepHeight : 0.6f;
        float chestH = typedStats != null ? typedStats.chestHeight : (GetHalfHeight() * 0.6f);
        float minRise = typedStats != null ? typedStats.minStepRise : 0.05f;
        int blockMask = obstacleMask.value | groundMask.value;
        if (blockMask == 0) blockMask = ~0;
        int gm = groundMask.value != 0 ? groundMask.value : ~0;
        Vector2 forward = new Vector2(dir, 0f);

        float headStartY = b.center.y + b.extents.y + 0.5f;
        Vector2 downStartCurr = new Vector2(frontX - dir * 0.02f, headStartY);
        float downDist = b.size.y + 2f;
        RaycastHit2D gCurr = Physics2D.Raycast(downStartCurr, Vector2.down, downDist, gm);
        float aheadX = frontX + dir * checkDist;
        Vector2 downStartAhead = new Vector2(aheadX, headStartY);
        RaycastHit2D gAhead = Physics2D.Raycast(downStartAhead, Vector2.down, downDist, gm);
        if (gCurr.collider != null && gAhead.collider != null)
        {
            float deltaY = gAhead.point.y - gCurr.point.y;
            if (deltaY > minRise)
            {
                if (deltaY <= stepH)
                {
                    float hopY = typedStats != null ? typedStats.hopForceY : 6f;
                    float hopX = typedStats != null ? typedStats.hopForceX : 2.5f;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
                    rb.AddForce(new Vector2(dir * hopX, hopY), ForceMode2D.Impulse);
                    hopCooldown = hopCooldownTime;
                    if (typedStats != null && typedStats.debugJumpLogs) Debug.Log($"RuinsBoss: step hop (deltaY={deltaY:F2})");
                    return;
                }
                else
                {
                    float halfH2 = GetHalfHeight();
                    Vector2 head2 = (Vector2)transform.position + Vector2.up * (halfH2 - 0.05f);
                    float clearance2 = typedStats != null ? typedStats.headClearanceCheck : 0.4f;
                    var ceil2 = Physics2D.Raycast(head2, Vector2.up, clearance2, blockMask);
                    if (ceil2.collider == null)
                    {
                        float jY2 = typedStats != null ? typedStats.tallJumpForceY : 10f;
                        float jX2 = typedStats != null ? typedStats.tallJumpForceX : 3.5f;
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                        if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
                        rb.AddForce(new Vector2(dir * jX2, jY2), ForceMode2D.Impulse);
                        hopCooldown = hopCooldownTime;
                        if (typedStats != null && typedStats.debugJumpLogs) Debug.Log($"RuinsBoss: tall jump (deltaY={deltaY:F2})");
                        return;
                    }
                }
            }
        }

        var hitLow = Physics2D.Raycast(new Vector2(frontX, footY), forward, checkDist, blockMask);
        var hitMid = Physics2D.Raycast(new Vector2(frontX, footY + chestH), forward, checkDist, blockMask);
        var hitHigh = Physics2D.Raycast(new Vector2(frontX, footY + stepH), forward, checkDist, blockMask);

        if (hitMid.collider != null || (hitLow.collider != null && hitHigh.collider != null))
        {
            float halfH = GetHalfHeight();
            Vector2 head = (Vector2)transform.position + Vector2.up * (halfH - 0.05f);
            float clearance = typedStats != null ? typedStats.headClearanceCheck : 0.4f;
            var ceil = Physics2D.Raycast(head, Vector2.up, clearance, blockMask);
            if (ceil.collider == null)
            {
                float jY = typedStats != null ? typedStats.tallJumpForceY : 10f;
                float jX = typedStats != null ? typedStats.tallJumpForceX : 3.5f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
                rb.AddForce(new Vector2(dir * jX, jY), ForceMode2D.Impulse);
                hopCooldown = hopCooldownTime;
                if (typedStats != null && typedStats.debugJumpLogs) Debug.Log("RuinsBoss: tall jump (blocked ahead)");
                return;
            }
        }

        float stuckDelay = typedStats != null ? typedStats.returnStuckJumpDelay : 0.25f;
        if (stuckTimer >= stuckDelay)
        {
            float jY = typedStats != null ? typedStats.tallJumpForceY : 10f;
            float jX = typedStats != null ? typedStats.tallJumpForceX : 3.5f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
            rb.AddForce(new Vector2(dir * jX, jY), ForceMode2D.Impulse);
            hopCooldown = hopCooldownTime;
            stuckTimer = 0f;
            if (typedStats != null && typedStats.debugJumpLogs) Debug.Log("RuinsBoss: fallback tall jump (stuck)");
        }
    }

    private bool IsGrounded()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.05f;
        float gDist = typedStats != null ? typedStats.groundCheckDistance : 0.18f;
        int gm = groundMask.value != 0 ? groundMask.value : ~0;
        var hit = Physics2D.OverlapCircle(origin + Vector2.down * gDist * 0.5f, gDist * 0.5f, gm);
        if (hit != null) return true;
        RaycastHit2D r = Physics2D.Raycast(origin, Vector2.down, gDist, gm);
        return r.collider != null;
    }

    private bool IsRoughlyOnGround()
    {
        if (groundMask.value == 0)
        {
            return col2d != null && col2d.IsTouchingLayers();
        }
        return IsGrounded();
    }

    private float GetHalfHeight()
    {
        return col2d != null ? col2d.bounds.size.y * 0.5f : 0.9f;
    }

    private void UpdateFrictionAndDrag()
    {
        if (rb == null) return;
        float dir = rb.linearVelocity.x >= 0 ? 1f : -1f;
        Bounds b = col2d != null ? col2d.bounds : new Bounds(transform.position, Vector3.one);
        float frontX = b.center.x + dir * (b.extents.x + 0.02f);
        float midY = b.center.y;
        int blockMask = obstacleMask.value | groundMask.value;
        if (blockMask == 0) blockMask = ~0;

        bool grounded = IsGrounded();
        rb.linearDamping = grounded ? defaultDamping : 0f;

        bool touchingWallAhead = Physics2D.Raycast(new Vector2(frontX, midY), new Vector2(dir, 0f), 0.08f, blockMask).collider != null;
        bool rising = rb.linearVelocity.y > 0.01f;
        if (!grounded && (touchingWallAhead || rising))
        {
            if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
        }
        else if (col2d != null && col2d.sharedMaterial != originalMaterial)
        {
            col2d.sharedMaterial = originalMaterial;
        }
    }

    private void TrackStuck()
    {
        float moved = Mathf.Abs(transform.position.x - lastX);
        if (moved < stuckEpsilon && currentState == EnemyState.Chasing && IsRoughlyOnGround())
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
        lastX = transform.position.x;
    }
}
