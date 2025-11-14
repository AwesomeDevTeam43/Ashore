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

    [Header("Animator Params")]
    [SerializeField] private string walkBool = "isWalking";
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Pathfinding")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Vector2 gridWorldSize = new Vector2(10, 6);
    [SerializeField] private float nodeRadius = 0.25f;
    [SerializeField] private float repathInterval = 0.2f;
    [SerializeField] private float waypointThreshold = 0.12f;

    private GridPathfinder2D pathfinder;
    private readonly List<Vector2> path = new List<Vector2>();
    private int pathIndex;
    private float repathTimer;

    private enum State { Idle, Chase, Slam }
    private State state = State.Idle;

    [Header("Slam Attack")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float slamRange = 1.6f;
    [SerializeField] private float slamWindup = 0.5f;
    [SerializeField] private float slamCooldown = 2.2f;
    [SerializeField] private int slamDamage = 6;
    [SerializeField] private float slamRadius = 2.75f;
    [SerializeField] private LayerMask slamHitMask;

    private float cooldown;
    private float windupTimer;

    void Start()
    {
        typedStats = stats as Golem_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        if (enemyHealth != null && typedStats != null)
        {
            enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.dropA, typedStats.dropB, typedStats.dropC);
        }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        pathfinder = new GridPathfinder2D(gridWorldSize, nodeRadius, obstacleMask);
        pathfinder.SetClearance(0.25f);
        pathfinder.SetGroundSupport(true, groundMask, 0.5f, 0.05f);
        pathfinder.SetCache(true, 0.5f);
    }

    void Update()
    {
        if (!player || stats == null) return;
        if (cooldown > 0f) cooldown -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.position);
        switch (state)
        {
            case State.Idle:
                if (dist <= detectRange) state = State.Chase;
                break;
            case State.Chase:
                if (dist <= slamRange && cooldown <= 0f) { state = State.Slam; windupTimer = slamWindup; rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); if (animator) animator.SetBool(walkBool,false); if (animator) animator.SetTrigger(attackTrigger); }
                break;
            case State.Slam:
                windupTimer -= Time.deltaTime;
                if (windupTimer <= 0f)
                {
                    DoSlam();
                    cooldown = slamCooldown;
                    state = State.Chase;
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (!player) return;
        if (state == State.Chase)
        {
            FollowPath(player.position, typedStats.moveSpeed);
            if (animator) animator.SetBool(walkBool, Mathf.Abs(rb.linearVelocity.x) > 0.05f);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void FollowPath(Vector2 target, float speed)
    {
        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f || pathIndex >= path.Count)
        {
            repathTimer = repathInterval;
            pathfinder.Configure(gridWorldSize, nodeRadius, obstacleMask);
            pathfinder.SetGroundSupport(true, groundMask, 0.5f, 0.05f);
            pathfinder.SetClearance(GetClearance());
            var found = NavGrid2D.Instance ? NavGrid2D.Instance.FindPath(transform.position, target) : pathfinder.FindPath(transform.position, target);
            path.Clear(); pathIndex = 0;
            if (found != null) path.AddRange(found);
        }
        if (path.Count == 0) { rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); return; }

        Vector2 wp = path[Mathf.Clamp(pathIndex, 0, path.Count - 1)];
        if (Vector2.Distance(transform.position, wp) <= waypointThreshold)
        {
            pathIndex++;
            if (pathIndex >= path.Count) { rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); return; }
            wp = path[pathIndex];
        }
        Vector2 dir = (wp - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * speed, rb.linearVelocity.y);
        Face(dir.x);
    }

    private float GetClearance()
    {
        var c2d = GetComponent<Collider2D>();
        if (c2d is CapsuleCollider2D cap) return Mathf.Max(cap.size.x, cap.size.y) * 0.5f * Mathf.Abs(transform.localScale.x);
        if (c2d is CircleCollider2D cc) return cc.radius * Mathf.Abs(transform.localScale.x);
        if (c2d is BoxCollider2D box) return Mathf.Max(box.size.x, box.size.y) * 0.5f * Mathf.Abs(transform.localScale.x);
        return 0.3f;
    }

    private void DoSlam()
    {
        if (animator) animator.SetTrigger(attackTrigger);
        var hits = Physics2D.OverlapCircleAll(transform.position, slamRadius, slamHitMask);
        foreach (var h in hits)
        {
            var hs = h.GetComponent<HealthSystem>() ?? h.GetComponentInParent<HealthSystem>();
            if (hs != null) hs.TakeDamage(slamDamage);
        }
    }

    private void Face(float dirX)
    {
        Vector3 s = stats.baseScale; s.x = Mathf.Abs(s.x) * (dirX >= 0 ? 1f : -1f); transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, slamRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, slamRange);
    }
}
