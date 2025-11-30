using UnityEngine;

public class BeeEnemy : EnemyBase
{
  private GiantBee_Stats typedStats;
  private GameObject player;
  private Rigidbody2D rb;
  private Collider2D col2D;
  private Collider2D playerCollider;
  private HealthSystem playerHealth;

  private enum EnemyState { Roaming, AttackWindup, Lunging, Retreating }
  private EnemyState enemyState;

  private float currentCooldown;
  private float lungeTimer;

  private Vector3 retreatTargetPosition;
  private Vector3 playerAttackPoint;
  private Vector3 lungeStartPosition;
  private Vector3 spawnPosition;
  private Vector2 desiredVelocity;

  [Header("Animation")]
  [SerializeField] private Animator animator;
  [SerializeField] private string isAttackingParam = "isAttacking";
  [SerializeField] private string isLungingParam = "isLunging";
  [SerializeField] private string isRetreatingParam = "isRetreating";

  [Header("Stinger")] 
  [SerializeField] private Collider2D stingerHitbox; // optional trigger hitbox to enable while stinger is out
  [SerializeField] private SpriteRenderer stingerVisual; // optional separate visual; will be enabled/disabled with stinger

  [Header("Attack Timings")] 
  [SerializeField] private float windupDuration = 0.25f; // time pulling stinger out before lunge begins
  private float windupTimer = 0f;

  [Header("Pathfinding")]
  [SerializeField] private LayerMask obstacleMask; // assign Ground | MovingPlatform
  [SerializeField] private Vector2 gridWorldSize = new Vector2(12, 8);
  [SerializeField] private float nodeRadius = 0.2f;
  [SerializeField] private float pathPointThreshold = 0.15f;
  [SerializeField] private float repathInterval = 0.25f;

  [Header("Combat Feedback")]
  [SerializeField] private EnemyCombatFeedback combatFeedback;
  [SerializeField] private LineRenderer lungeTelegraphLine;
  [SerializeField] private Color telegraphLineColor = new Color(1f, 0f, 0f, 0.5f);

  private GridPathfinder2D pathfinder;
  private readonly System.Collections.Generic.List<Vector2> currentPath = new System.Collections.Generic.List<Vector2>();
  private int pathIndex = 0;
  private float repathTimer = 0f;

  private void Start()
  {
    typedStats = stats as GiantBee_Stats;

    enemyHealth = GetComponent<Enemy_Health>();
    
    rb = GetComponent<Rigidbody2D>();
    if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
    rb.gravityScale = 0f;
    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    rb.freezeRotation = true;

    col2D = GetComponent<Collider2D>();
    if (col2D == null)
    {
      var circle = gameObject.AddComponent<CircleCollider2D>();
      circle.radius = 0.25f;
      circle.isTrigger = false;
      col2D = circle;
      Debug.LogWarning("BeeEnemy: No Collider2D found. Added CircleCollider2D automatically.");
    }

    player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerHealth = player.GetComponent<HealthSystem>();
      playerCollider = player.GetComponent<Collider2D>();
    }

    if (stats == null)
    {
      Debug.LogWarning("BeeEnemy: stats (GiantBee_Stats) not assigned. This enemy will not run.");
      enabled = false;
      return;
    }

    transform.localScale = stats.baseScale;

    pathfinder = new GridPathfinder2D(gridWorldSize, nodeRadius, obstacleMask);
    pathfinder.SetCache(true, 0.75f);

    spawnPosition = transform.position;

    if (animator == null)
    {
      animator = GetComponentInChildren<Animator>();
    }
    if (animator != null)
    {
      animator.SetBool(isAttackingParam, false);
      animator.SetBool(isLungingParam, false);
      animator.SetBool(isRetreatingParam, false);
    }

    if (stingerHitbox != null) stingerHitbox.enabled = false;
    if (stingerVisual != null) stingerVisual.enabled = false;
  }

  private void Update()
  {
    if (player == null || stats == null) return;

    if (currentCooldown > 0f) currentCooldown -= Time.deltaTime;
    FlipSprite();

    float playerDistance = Vector3.Distance(transform.position, player.transform.position);
    StateMachine(playerDistance);
  }

  public bool TryAttack()
  {
    if (currentCooldown > 0f) return false;
    if (player == null) return false;
    float playerDistance = Vector3.Distance(transform.position, player.transform.position);
    if (playerDistance > typedStats.lungeRange) return false;
    var feet = GetPlayerFeetPosition();
    if (!HasLineOfSightToAttackPoint(feet)) return false;
    StartAttackWindup();
    return true;
  }

  private void FixedUpdate()
  {
    // Pre-move collision query to prevent tunneling through Ground/MovingPlatform even at high speeds
    float stepDist = desiredVelocity.magnitude * Time.fixedDeltaTime;
    if (stepDist > 0.0001f)
    {
      Vector2 origin = transform.position;
      Vector2 dir = desiredVelocity.normalized;
      float rad = Mathf.Max(nodeRadius, GetClearance());
      var hit = Physics2D.CircleCast(origin, rad, dir, stepDist, obstacleMask);
      if (hit.collider != null)
      {
        // Blocked this frame; stop and force a quick repath
        desiredVelocity = Vector2.zero;
        repathTimer = 0f;
      }
    }

    // Apply velocity in physics step
    rb.linearVelocity = desiredVelocity;
  }

  private void StateMachine(float playerDistance)
  {
    switch (enemyState)
    {
      case EnemyState.Roaming:
        RoamBehavior(playerDistance);
        break;
      case EnemyState.AttackWindup:
        AttackWindupBehavior(playerDistance);
        break;
      case EnemyState.Lunging:
        LungeBehavior(playerDistance);
        break;
      case EnemyState.Retreating:
        RetreatBehavior(playerDistance);
        break;
    }
  }

  private void RoamBehavior(float playerDistance)
  {
    // Leash logic: chase even slightly outside detect radius up to a leash range, then go home
    float leashRange = typedStats.playerDetect * 1.75f; // configurable multiplier
    float distFromSpawn = Vector3.Distance(transform.position, spawnPosition);
    bool withinLeash = playerDistance <= leashRange;

    if (withinLeash)
    {
      if (currentCooldown <= 0f && playerDistance <= typedStats.lungeRange)
      {
        // Only lunge if there is a clear line of sight to the player's feet
        Vector3 feet = GetPlayerFeetPosition();
        if (HasLineOfSightToAttackPoint(feet))
        {
          StartAttackWindup();
        }
        else
        {
          // Keep approaching via pathfinding until LOS is clear
          FollowPathTowards(player.transform.position, typedStats.roamSpeed);
        }
      }
      else if (currentCooldown <= 0f && playerDistance > typedStats.lungeRange)
      {
        // Pathfind toward the player while avoiding obstacles
        FollowPathTowards(player.transform.position, typedStats.roamSpeed);
      }
      else
      {
        desiredVelocity = Vector2.zero;
      }
    }
    else
    {
      // Out of leash; return home
      FollowPathTowards(spawnPosition, typedStats.retreatSpeed);
      if (Vector2.Distance(transform.position, spawnPosition) < 0.5f)
      {
        desiredVelocity = Vector2.zero;
      }
    }
  }

  private void StartAttackWindup()
  {
    if (enemyState == EnemyState.AttackWindup || enemyState == EnemyState.Lunging || enemyState == EnemyState.Retreating) return;
    lungeStartPosition = transform.position;
    playerAttackPoint = GetPlayerFeetPosition();
    windupTimer = 0f;
    enemyState = EnemyState.AttackWindup;
    if (animator != null)
    {
      animator.SetBool(isAttackingParam, true);
      animator.SetBool(isLungingParam, false);
      animator.SetBool(isRetreatingParam, false);
    }
    EnableStinger(true); // stinger visually out during windup

    // ADICIONAR: Telegraph visual
    if (combatFeedback != null)
      combatFeedback.PlayAttackTelegraph();
    
    // ADICIONAR: Linha mostrando trajetória do lunge
    if (lungeTelegraphLine != null)
    {
      lungeTelegraphLine.enabled = true;
      lungeTelegraphLine.SetPosition(0, transform.position);
      lungeTelegraphLine.SetPosition(1, playerAttackPoint);
      lungeTelegraphLine.startColor = telegraphLineColor;
      lungeTelegraphLine.endColor = telegraphLineColor;
    }
  }

  private void AttackWindupBehavior(float playerDistance)
  {
    desiredVelocity = Vector2.zero;
    windupTimer += Time.deltaTime;
    if (windupTimer >= windupDuration)
    {
      BeginLungeAfterWindup();
    }
  }

  private void BeginLungeAfterWindup()
  {
    lungeTimer = 0f;
    enemyState = EnemyState.Lunging;
    if (animator != null)
    {
      animator.SetBool(isAttackingParam, false);
      animator.SetBool(isLungingParam, true);
    }

    // ADICIONAR: Desabilitar linha de telegraph
    if (lungeTelegraphLine != null)
      lungeTelegraphLine.enabled = false;
  }

  private void EnableStinger(bool on)
  {
    if (stingerHitbox != null) stingerHitbox.enabled = on;
    if (stingerVisual != null) stingerVisual.enabled = on;
  }

  private void StartLunge()
  {
    lungeStartPosition = transform.position;
    playerAttackPoint = GetPlayerFeetPosition();
    lungeTimer = 0f;
    enemyState = EnemyState.Lunging;
    Debug.Log("Starting lunge towards player's feet!");
  }

  private Vector3 GetPlayerFeetPosition()
  {
    if (playerCollider != null)
    {
      Bounds bounds = playerCollider.bounds;
      return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }
    return player.transform.position + Vector3.down * 0.5f;
  }

  private void LungeBehavior(float playerDistance)
  {
    lungeTimer += Time.deltaTime;
    // Use pathfinding-aware movement to avoid tunneling into ground during lunge
    FollowPathTowards(playerAttackPoint, typedStats.lungingForce);
    if (animator != null)
    {
      animator.SetBool(isAttackingParam, false);
      animator.SetBool(isLungingParam, true);
      animator.SetBool(isRetreatingParam, false);
    }
    EnableStinger(true);

    if (lungeTimer >= typedStats.lungeDuration || Vector2.Distance(transform.position, playerAttackPoint) < 0.3f)
    {
      EndLunge();
    }
  }

  private void EndLunge()
  {
    Vector2 retreatDir = (lungeStartPosition - transform.position).normalized;
    retreatTargetPosition = transform.position + (Vector3)retreatDir * typedStats.retreatRange;
    currentCooldown = typedStats.lungeCooldown;
    enemyState = EnemyState.Retreating;
    Debug.Log("Lunge ended, retreating!");
    if (animator != null)
    {
      animator.SetBool(isLungingParam, false);
      animator.SetBool(isRetreatingParam, true);
    }
  }

  private void RetreatBehavior(float playerDistance)
  {
    // Use pathfinding to get back toward retreat target without tunneling through walls
    FollowPathTowards(retreatTargetPosition, typedStats.retreatSpeed);
    EnableStinger(false);
    if (animator != null)
    {
      animator.SetBool(isAttackingParam, false);
      animator.SetBool(isLungingParam, false);
      animator.SetBool(isRetreatingParam, true);
    }

    if (Vector2.Distance(transform.position, retreatTargetPosition) < 0.5f)
    {
      rb.linearVelocity = Vector2.zero;
      enemyState = EnemyState.Roaming;
      Debug.Log("Retreat complete, back to roaming!");
      if (animator != null)
      {
        animator.SetBool(isAttackingParam, false);
        animator.SetBool(isLungingParam, false);
        animator.SetBool(isRetreatingParam, false);
      }
    }
  }

  private void FollowPathTowards(Vector3 target, float speed)
  {
    repathTimer -= Time.deltaTime;
    if (repathTimer <= 0f || pathIndex >= currentPath.Count)
    {
      repathTimer = repathInterval;
      System.Collections.Generic.List<Vector2> path = null;
      if (NavGrid2D.Instance != null)
      {
        // Use baked room grid when available
        path = NavGrid2D.Instance.FindPath(transform.position, target);
      }
      else
      {
        // Fallback: dynamic local grid
        pathfinder.Configure(gridWorldSize, nodeRadius, obstacleMask);
        pathfinder.SetClearance(GetClearance());
        path = pathfinder.FindPath(transform.position, target);
      }
      currentPath.Clear();
      pathIndex = 0;
      if (path != null)
      {
        currentPath.AddRange(path);
      }
      else
      {
        // Robust recovery attempts: enlarge grid and try different centers
        bool found = false;
        Vector2 originalSize = gridWorldSize;
        Vector2 dirToTarget = (target - transform.position).normalized;

        Vector2[] centers = new Vector2[]
        {
          (Vector2)((transform.position + target) * 0.5f),
          (Vector2)transform.position,
          (Vector2)target,
        };

        float[] sizeMults = new float[] { 1.5f, 2.0f };

        foreach (float sm in sizeMults)
        {
          if (found) break;
          Vector2 big = originalSize * sm;
          if (NavGrid2D.Instance != null)
          {
            // If baked grid exists, just retry with the same; expanding doesn't apply to baked
          }
          else
          {
            pathfinder.Configure(big, nodeRadius, obstacleMask);
            pathfinder.SetClearance(GetClearance());
          }

          // Try with base centers
          foreach (var c in centers)
          {
            var p = NavGrid2D.Instance != null ? NavGrid2D.Instance.FindPath(transform.position, target) : pathfinder.FindPath(transform.position, target, c);
            if (p != null)
            {
              gridWorldSize = big; // temporarily adopt
              currentPath.AddRange(p);
              found = true;
              break;
            }
          }

          if (found) break;

          // Try offset centers to encourage going around: perpendicular offsets
          Vector2 perp = new Vector2(-dirToTarget.y, dirToTarget.x);
          Vector2 off = perp * (Mathf.Min(big.x, big.y) * 0.25f);
          Vector2[] offsetCenters = new Vector2[]
          {
            (Vector2)((transform.position + target) * 0.5f) + off,
            (Vector2)((transform.position + target) * 0.5f) - off
          };
          foreach (var c in offsetCenters)
          {
            var p = NavGrid2D.Instance != null ? NavGrid2D.Instance.FindPath(transform.position, target) : pathfinder.FindPath(transform.position, target, c);
            if (p != null)
            {
              gridWorldSize = big;
              currentPath.AddRange(p);
              found = true;
              break;
            }
          }
        }
      }
    }

    if (currentPath.Count == 0)
    {
      // No path after all attempts: local obstacle-avoidance steer
      Vector2 toTarget = (target - transform.position);
      float bestScore = float.NegativeInfinity;
      Vector2 bestDir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector2.right;
      // Sample directions in 22.5-degree increments
      int samples = 16;
      float rad = Mathf.Max(nodeRadius, GetClearance());
      for (int i = 0; i < samples; i++)
      {
        float angle = (360f / samples) * i;
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        // Prefer directions roughly toward target
        float align = Vector2.Dot(dir, toTarget.normalized);
        // Check immediate clearance ahead
        float checkDist = 1.0f;
        var hit = Physics2D.CircleCast((Vector2)transform.position, rad, dir, checkDist, obstacleMask);
        float free = hit.collider == null ? 1f : Mathf.Clamp01(hit.distance / checkDist);
        float score = align * 0.7f + free * 0.6f; // balance progress and clearance
        if (score > bestScore)
        {
          bestScore = score;
          bestDir = dir;
        }
      }
      desiredVelocity = bestDir.normalized * speed * 0.9f;
      return;
    }

    // Move toward current waypoint
    Vector2 wp = currentPath[Mathf.Clamp(pathIndex, 0, currentPath.Count - 1)];
    Vector2 toWp = wp - (Vector2)transform.position;
    if (toWp.magnitude <= pathPointThreshold)
    {
      pathIndex++;
      if (pathIndex >= currentPath.Count)
      {
        desiredVelocity = Vector2.zero;
        return;
      }
      wp = currentPath[pathIndex];
      toWp = wp - (Vector2)transform.position;
    }

    Vector2 desired = toWp.normalized * speed;
    desiredVelocity = desired;
  }

  private void OnDrawGizmosSelected()
  {
    // Draw grid bounds and current path for debugging
    Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
    Vector2 center = Application.isPlaying && player != null
        ? (Vector2)((transform.position + player.transform.position) * 0.5f)
        : (Vector2)transform.position;
    Vector2 size = gridWorldSize;
    Gizmos.DrawWireCube(center, size);

    if (currentPath != null && currentPath.Count > 0)
    {
      Gizmos.color = Color.cyan;
      for (int i = 0; i < currentPath.Count - 1; i++)
      {
        Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        Gizmos.DrawWireSphere(currentPath[i], 0.06f);
      }
    }
  }

  // Ensure we only start a lunge if we have a clear LOS to the player's feet
  private bool HasLineOfSightToAttackPoint(Vector3 attackPoint)
  {
    Vector2 origin = transform.position;
    Vector2 dir = (attackPoint - transform.position);
    float dist = dir.magnitude;
    if (dist <= 0.01f) return true;
    dir /= dist;
    // Ignore the bee's own collider when raycasting
    int mask = obstacleMask;
    var hit = Physics2D.Raycast(origin, dir, dist, mask);
    return hit.collider == null; // true if nothing blocks the way
  }

  private float GetClearance()
  {
    if (col2D is CircleCollider2D cc)
    {
      return cc.radius * Mathf.Abs(transform.localScale.x) * 0.6f;
    }
    if (col2D is CapsuleCollider2D cap)
    {
      return Mathf.Max(cap.size.x, cap.size.y) * 0.3f;
    }
    return nodeRadius * 0.75f; // fallback
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (enemyState == EnemyState.Lunging && collision.gameObject == player)
    {
      Debug.Log("Stung the player! Retreating.");
      if (playerHealth != null)
      {
        playerHealth.TakeDamage((int)currentDamage);
        
        // ADICIONAR: Feedback de ataque bem-sucedido
        if (combatFeedback != null)
          combatFeedback.PlayAttackFeedback(collision.contacts[0].point);
      }
      EndLunge();
    }
  }

  private void OnDrawGizmos()
  {
    var ts = typedStats ?? (stats as GiantBee_Stats);
    if (ts == null) return;

    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, ts.playerDetect);

    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, ts.lungeRange);

    Gizmos.color = Color.blue;
    Gizmos.DrawWireSphere(transform.position, ts.retreatRange);

    if (Application.isPlaying && player != null)
    {
      Vector3 feetPos = GetPlayerFeetPosition();
      Gizmos.color = Color.magenta;
      Gizmos.DrawWireSphere(feetPos, 0.2f);
      Gizmos.DrawLine(transform.position, feetPos);
    }

    if (Application.isPlaying)
    {
      Gizmos.color = Color.white;
      Gizmos.DrawWireSphere(lungeStartPosition, 0.3f);
      if (enemyState == EnemyState.Retreating)
      {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, retreatTargetPosition);
        Gizmos.DrawWireSphere(retreatTargetPosition, 0.2f);
      }
      // Show planned lunge target
      Gizmos.color = Color.magenta;
      Gizmos.DrawWireSphere(playerAttackPoint, 0.15f);
      // Show desired velocity vector
      Gizmos.color = new Color(1f,0.5f,0f,0.8f);
      Vector3 velEnd = transform.position + (Vector3)(desiredVelocity * 0.1f);
      Gizmos.DrawLine(transform.position, velEnd);
      // Show agent clearance based on collider
      Gizmos.color = new Color(0.2f,0.8f,1f,0.6f);
      Gizmos.DrawWireSphere(transform.position, GetClearance());
    }
  }

  private void FlipSprite()
  {
    if (player == null || stats == null) return;
    float dir = (player.transform.position.x >= transform.position.x) ? 1f : -1f;
    Vector3 s = stats.baseScale;
    s.x = Mathf.Abs(s.x) * dir;
    transform.localScale = s;
  }
}