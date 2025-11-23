using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Enemy_Health))]
public class Enemy_Golem : EnemyBase
{
    public Golem_Stats typedStats;
    private Enemy_Health enemyHealth;
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private Collider2D col2d;

    [Header("Animator Params")]
    [SerializeField] private string walkBool = "isWalking";
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Movement (no pathfinding)")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float hopCooldownTime = 0.3f;

    private enum State { Idle, Chase, Slam, Return }
    private State state = State.Idle;

    [Header("Slam Attack")]
    [SerializeField] private LayerMask slamHitMask;

    private float cooldown;
    private float windupTimer;
    private float hopCooldown;
    private float lastX;
    private float stuckTimer;
    private const float stuckEpsilon = 0.01f;
    // Friction management to avoid grinding against walls while jumping
    private PhysicsMaterial2D originalMaterial;
    private PhysicsMaterial2D lowFrictionMaterial;
    private float defaultDamping;
    private Vector3 spawnPos;

    void Start()
    {
        typedStats = stats as Golem_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        col2d = GetComponent<Collider2D>();
        if (enemyHealth != null && typedStats != null)
        {
            enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.woodDrop, typedStats.stoneDrop, typedStats.ropeDrop);
        }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        rb.gravityScale = 3f;
        rb.freezeRotation = true;
    defaultDamping = rb.linearDamping;

        // Prepare a low-friction material to reduce wall sticking when airborne
        if (col2d != null)
        {
            originalMaterial = col2d.sharedMaterial;
            lowFrictionMaterial = new PhysicsMaterial2D("GolemLowFriction") { friction = 0f, bounciness = 0f };
        }

    lastX = transform.position.x;
    spawnPos = transform.position;

    }

    void Update()
    {
        if (!player || stats == null) return;
        if (cooldown > 0f) cooldown -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.position);
        switch (state)
        {
            case State.Idle:
                if (dist <= (typedStats != null ? typedStats.detectRange : 6f)) state = State.Chase;
                break;
            case State.Chase:
                if (dist > (typedStats != null ? typedStats.leashDistance : 12f))
                {
                    state = State.Return;
                    if (animator) animator.SetBool(walkBool, true);
                    break;
                }
                if (dist <= (typedStats != null ? typedStats.slamRange : 1.6f) && cooldown <= 0f)
                {
                    state = State.Slam;
                    windupTimer = typedStats != null ? typedStats.slamWindup : 0.5f;
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    if (animator) animator.SetBool(walkBool,false);
                    if (animator) animator.SetTrigger(attackTrigger);
                }
                break;
            case State.Slam:
                if (dist > (typedStats != null ? typedStats.leashDistance : 12f))
                {
                    state = State.Return;
                    break;
                }
                windupTimer -= Time.deltaTime;
                if (windupTimer <= 0f)
                {
                    // Damage is applied via AnimEvent_GolemSlam from the animation. Just exit the slam state.
                    cooldown = typedStats != null ? typedStats.slamCooldown : 2.2f;
                    state = State.Chase;
                }
                break;
            case State.Return:
                // If player comes close again, resume chase
                if (dist <= (typedStats != null ? typedStats.detectRange : 6f))
                {
                    state = State.Chase;
                    break;
                }
                // Arrived at spawn -> idle
                if (Vector2.Distance(transform.position, spawnPos) <= (typedStats != null ? typedStats.returnStopDistance : 0.5f))
                {
                    state = State.Idle;
                    if (animator) animator.SetBool(walkBool, false);
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (!player) return;
        if (state == State.Chase)
        {
            SimpleFollow();
            if (animator) animator.SetBool(walkBool, Mathf.Abs(rb.linearVelocity.x) > 0.05f);
        }
        else if (state == State.Return)
        {
            ReturnToSpawn();
            if (animator) animator.SetBool(walkBool, Mathf.Abs(rb.linearVelocity.x) > 0.05f);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Manage friction/drag to avoid sticking to walls when airborne
        UpdateFrictionAndDrag();

        // Update stuck detection
        float moved = Mathf.Abs(transform.position.x - lastX);
        // Consider stuck if we haven't moved horizontally much while trying to chase on the ground
        if (moved < stuckEpsilon && (state == State.Chase || state == State.Return) && IsRoughlyOnGround())
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
        lastX = transform.position.x;
    }

    private void SimpleFollow()
    {
        if (!player) return;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        // try a step hop or tall jump if an obstacle is directly ahead
        TryStepOrTallJump(dir);
        float moveSpeed = typedStats != null ? typedStats.moveSpeed : 1.2f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        Face(dir);
    }

    private void ReturnToSpawn()
    {
        float dir = Mathf.Sign(spawnPos.x - transform.position.x);
        // Use same hop/jump logic to navigate back
        TryStepOrTallJump(dir);
        float speed = typedStats != null && typedStats.returnSpeed > 0f ? typedStats.returnSpeed : (typedStats != null ? typedStats.moveSpeed : 1.2f);
        // Stop when close enough
        if (Vector2.Distance(transform.position, spawnPos) <= (typedStats != null ? typedStats.returnStopDistance : 0.5f))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
        Face(dir);
    }

    private bool IsGrounded()
    {
        // Use a small circle to check for ground contact (more reliable than a single ray)
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.05f;
        float gDist = typedStats != null ? typedStats.groundCheckDistance : 0.18f;
        int gm = groundMask.value != 0 ? groundMask.value : ~0; // if unset in Inspector, fall back to all layers
        var hit = Physics2D.OverlapCircle(origin + Vector2.down * gDist * 0.5f, gDist * 0.5f, gm);
        if (hit != null) return true;
        // fallback ray
        RaycastHit2D r = Physics2D.Raycast(origin, Vector2.down, gDist, gm);
        return r.collider != null;
    }

    // A bit more permissive when we only need a rough ground check (e.g., for stuck detection)
    private bool IsRoughlyOnGround()
    {
        if (groundMask.value == 0)
        {
            // If no ground mask configured, use collider contacts as a permissive fallback
            return col2d != null && col2d.IsTouchingLayers();
        }
        return IsGrounded();
    }

    private float GetHalfHeight()
    {
        var c2d = GetComponent<Collider2D>();
        if (c2d != null) return c2d.bounds.size.y * 0.5f;
        return 0.9f; // sensible default
    }

    private void TryStepOrTallJump(float dir)
    {
        if (!IsGrounded()) return;
        if (hopCooldown > 0f) { hopCooldown -= Time.fixedDeltaTime; return; }
        // establish better ray origins at collider front so rays don't start inside geometry
        Bounds b = col2d != null ? col2d.bounds : new Bounds(transform.position, Vector3.one * 1f);
        float frontX = b.center.x + dir * (b.extents.x + 0.02f);
        float footY = b.min.y + 0.02f;
        float checkDist = typedStats != null ? typedStats.hopCheckDistance : 0.45f;
        float stepH = typedStats != null ? typedStats.maxStepHeight : 0.6f;
        float chestH = typedStats != null ? typedStats.chestHeight : (GetHalfHeight() * 0.6f);
        int blockMask = obstacleMask.value | groundMask.value;
        if (blockMask == 0) blockMask = ~0; // defensive: if masks not set, consider all colliders as obstacles
        int gm = groundMask.value != 0 ? groundMask.value : ~0;
        Vector2 forward = new Vector2(dir, 0f);

        // Primary: ground-height based step/stair detection (treat stairs as ground)
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
            float minRise = typedStats != null ? typedStats.minStepRise : 0.05f;
            if (deltaY > minRise)
            {
                if (deltaY <= stepH)
                {
                    // Small step/stair: perform a hop
                    float hopY0 = typedStats != null ? typedStats.hopForceY : 6f;
                    float hopX0 = typedStats != null ? typedStats.hopForceX : 2.5f;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
                    rb.AddForce(new Vector2(dir * hopX0, hopY0), ForceMode2D.Impulse);
                    hopCooldown = hopCooldownTime;
                    if (typedStats != null && typedStats.debugJumpLogs) Debug.Log($"Golem: step hop (deltaY={deltaY:F2})");
                    return;
                }
                else
                {
                    // Taller than step height: tall jump if there's head clearance
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
                        if (typedStats != null && typedStats.debugJumpLogs) Debug.Log($"Golem: tall jump (deltaY={deltaY:F2})");
                        return;
                    }
                }
            }
        }

        // low, mid, high rays from the front face (fallback for walls/ledges)
        var hitLow = Physics2D.Raycast(new Vector2(frontX, footY), forward, checkDist, blockMask);
        var hitMid = Physics2D.Raycast(new Vector2(frontX, footY + chestH), forward, checkDist, blockMask);
        var hitHigh = Physics2D.Raycast(new Vector2(frontX, footY + stepH), forward, checkDist, blockMask);

        // Case 2: tall obstacle (mid or high blocked) -> attempt big jump if there's head clearance
        if (hitMid.collider != null || (hitLow.collider != null && hitHigh.collider != null))
        {
            float halfH = GetHalfHeight();
            Vector2 head = (Vector2)transform.position + Vector2.up * (halfH - 0.05f);
            float clearance = typedStats != null ? typedStats.headClearanceCheck : 0.4f;
            var ceil = Physics2D.Raycast(head, Vector2.up, clearance, blockMask);
            if (ceil.collider == null)
            {
                float jY = typedStats != null ? typedStats.tallJumpForceY : 10f;
                // Default tall jump if height-based branch didn't early-return
                float jX = typedStats != null ? typedStats.tallJumpForceX : 3.5f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
                rb.AddForce(new Vector2(dir * jX, jY), ForceMode2D.Impulse);
                hopCooldown = hopCooldownTime;
                if (typedStats != null && typedStats.debugJumpLogs) Debug.Log("Golem: tall jump (blocked ahead)");
                return;
            }
        }

        // Fallback: if we've been stuck for a bit, force a tall jump
        if (stuckTimer >= 0.25f)
        {
            float jY = typedStats != null ? typedStats.tallJumpForceY : 10f;
            float jX = typedStats != null ? typedStats.tallJumpForceX : 3.5f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
            rb.AddForce(new Vector2(dir * jX, jY), ForceMode2D.Impulse);
            hopCooldown = hopCooldownTime;
            stuckTimer = 0f;
            if (typedStats != null && typedStats.debugJumpLogs) Debug.Log("Golem: fallback tall jump (stuck)");
        }
    }

    private void UpdateFrictionAndDrag()
    {
        // Determine facing direction for the front face ray
        float dir = player ? Mathf.Sign(player.position.x - transform.position.x) : Mathf.Sign(transform.localScale.x);
        Bounds b = col2d != null ? col2d.bounds : new Bounds(transform.position, Vector3.one);
        float frontX = b.center.x + dir * (b.extents.x + 0.02f);
        float midY = b.center.y;
        int blockMask = obstacleMask.value | groundMask.value;
    if (blockMask == 0) blockMask = ~0;

    bool grounded = IsGrounded();
    // Reduce drag in air to keep momentum
    rb.linearDamping = grounded ? defaultDamping : 0f;

        // If airborne and touching a wall ahead (or just rising), use low friction
        bool touchingWallAhead = Physics2D.Raycast(new Vector2(frontX, midY), new Vector2(dir, 0f), 0.08f, blockMask).collider != null;
    bool rising = rb.linearVelocity.y > 0.01f;
        if (!grounded && (touchingWallAhead || rising))
        {
            if (col2d != null && lowFrictionMaterial != null) col2d.sharedMaterial = lowFrictionMaterial;
        }
        else
        {
            if (col2d != null && col2d.sharedMaterial != originalMaterial) col2d.sharedMaterial = originalMaterial;
        }
    }

    // Animation event hook: call from the slam animation at the hit frame
    public void AnimEvent_GolemSlam()
        
    {
        float radius = typedStats != null ? typedStats.slamRadius : 2.75f;
        int damage = typedStats != null ? typedStats.slamDamage : 6;
        int mask = slamHitMask.value != 0 ? slamHitMask.value : LayerMask.GetMask("Player");
        var hits = Physics2D.OverlapCircleAll(transform.position, radius, mask);
        foreach (var h in hits)
        {
            if (!IsPlayerCollider(h)) continue;
            var hs = h.GetComponent<HealthSystem>() ?? h.GetComponentInParent<HealthSystem>();
            if (hs != null) hs.TakeDamage(damage);
        }
    }

    private void Face(float dirX)
    {
        Vector3 s = stats.baseScale; s.x = Mathf.Abs(s.x) * (dirX >= 0 ? 1f : -1f); transform.localScale = s;
    }

    private static bool IsPlayerCollider(Collider2D col)
    {
        if (col == null) return false;
        if (col.CompareTag("Player")) return true;
        Transform root = col.transform.root;
        return root != null && root.CompareTag("Player");
    }

    void OnDrawGizmos()
    {
        float radius = typedStats != null ? typedStats.slamRadius : 2.75f;
        float dRange = typedStats != null ? typedStats.detectRange : 6f;
        float sRange = typedStats != null ? typedStats.slamRange : 1.6f;
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, dRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, sRange);
    // hop debug rays
        Gizmos.color = Color.magenta;
        float dir = Application.isPlaying && player ? Mathf.Sign(player.position.x - transform.position.x) : Mathf.Sign(transform.localScale.x);
        Bounds gb = col2d != null ? col2d.bounds : new Bounds(transform.position, Vector3.one);
        float frontX = gb.center.x + dir * (gb.extents.x + 0.02f);
        float footY = gb.min.y + 0.02f;
        float checkDist = typedStats != null ? typedStats.hopCheckDistance : 0.45f;
        float stepH = typedStats != null ? typedStats.maxStepHeight : 0.6f;
        float chestH = typedStats != null ? typedStats.chestHeight : (GetHalfHeight() * 0.6f);
        Vector3 a = new Vector3(frontX, footY, 0f);
        Gizmos.DrawLine(a, a + new Vector3(dir * checkDist, 0f, 0f));
        // Chest height ray (mid)
        Gizmos.color = new Color(1f, 0.64f, 0f); // orange
        Vector3 m = new Vector3(frontX, footY + chestH, 0f);
        Gizmos.DrawLine(m, m + new Vector3(dir * checkDist, 0f, 0f));
        // Step height ray (high)
        Gizmos.color = Color.magenta;
        Vector3 h = new Vector3(frontX, footY + stepH, 0f);
        Gizmos.DrawLine(h, h + new Vector3(dir * checkDist, 0f, 0f));

        // Ground check gizmos: visualize groundCheckDistance used by IsGrounded()
        float gDist = typedStats != null ? typedStats.groundCheckDistance : 0.18f;
        Vector3 groundOrigin = transform.position + Vector3.down * 0.05f;
        Vector3 circleCenter = groundOrigin + Vector3.down * gDist * 0.5f;
        float circleRadius = gDist * 0.5f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(circleCenter, circleRadius);
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * gDist);

        // Spawn marker
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.75f);
            Gizmos.DrawWireSphere(spawnPos, 0.2f);
        }
    }
}
