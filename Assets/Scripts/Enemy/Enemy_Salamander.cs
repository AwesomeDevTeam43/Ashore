using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Enemy_Health))]
public class Enemy_Salamander : EnemyBase
{
  // Core refs
  public Salamander_Stats typedStats; // cast from stats asset
  private Enemy_Health enemyHealth;
  private Transform player;
  private Rigidbody2D rb;
  private Animator animator;

  [Header("Animation")]
  [SerializeField] private string walkBoolName = "isWalking"; // only animation parameter used

  [Header("Overrides (optional)")]
  [SerializeField] private float shootRangeOverride = -1f; // if >0 overrides stats.shootRange
  [SerializeField] private float biteRangeOverride = -1f;  // if >0 overrides stats.biteRange

  [Header("Bite")]
  [SerializeField] private Transform bitePoint; // optional mouth point
  [SerializeField] private float biteRadius = 0.6f;
  [SerializeField] private LayerMask biteHitMask; // layers that can be damaged by bite
  [SerializeField] private float timeBetweenBites = 1.2f;

  [Header("Shooting")]
  [SerializeField] private GameObject spineProjectilePrefab;
  [SerializeField] private Transform projectileSpawnPoint;
  [SerializeField] private float projectileSpeed = 10f;
  [SerializeField] private float timeBetweenShots = 0.6f;

  [Header("Retreat Hop")]
  [SerializeField] private float hopForceX = 4f;
  [SerializeField] private float hopForceY = 5f;
  [SerializeField] private float retreatTargetDistance = 4f; // distance to reach before resume approach

  [Header("Retreat Behavior")]
  [Tooltip("Maximum time (seconds) the salamander will spend retreating before stopping the run and re-engaging.")]
  [SerializeField] private float maxRetreatDuration = 1.2f;
  [Tooltip("If the player is this factor * BiteRange or closer while retreating, the salamander may stop retreating and attempt a bite (if not on cooldown).")]
  [SerializeField] private float biteDuringRetreatDistanceFactor = 1.1f;

  [Header("Aggression / Shoot-to-Bite Switch")]
  [SerializeField] private float shootAggroTime = 2.5f; // time continuously shooting before considering forced bite
  [Range(0f, 1f)][SerializeField] private float shootToBiteChance = 0.6f; // chance to convert shooting into an aggressive rush + bite
  [SerializeField] private float aggressiveSpeedMultiplier = 2.5f; // speed multiplier when rushing for bite after shooting
  [SerializeField] private float aggressiveLockDuration = 0.35f; // minimum time to stay in Approach when rushing

  [Header("Retreat Threshold (Override)")]
  [Tooltip("If > 0, overrides the distance at which Salamander will retreat while shooting when player is too close.")]
  [SerializeField] private float retreatCloseThresholdOverride = -1f;

  [Header("Home Return")]
  [Tooltip("If > 0, when the player is farther than this horizontal distance, the Salamander returns to its spawn position.")]
  [SerializeField] private float returnToHomeDistance = 12f;
  [Tooltip("Distance from home X to consider arrived when returning.")]
  [SerializeField] private float homeArriveThreshold = 0.2f;

  private bool aggressiveRush; // when true, movement speed boosted until bite attempt
  private float aggressiveLockTimer; // counts down while in aggressive rush to avoid instant flip back to Shoot
  private float timeInShoot;
  private Vector3 homePosition;
  private float retreatTimer = 0f;

  // Timers
  private float shotCooldown;
  private float biteCooldown;

  // Simple states
  private enum State { Idle, Approach, Shoot, Bite, Retreat, ReturnHome }
  private State state = State.Idle;

  private float ShootRange => (shootRangeOverride > 0f ? shootRangeOverride : typedStats.shootRange);
  private float BiteRange => (biteRangeOverride > 0f ? biteRangeOverride : typedStats.biteRange);

  void Start()
  {
    typedStats = stats as Salamander_Stats;
    enemyHealth = GetComponent<Enemy_Health>();
    rb = GetComponent<Rigidbody2D>();
    animator = GetComponentInChildren<Animator>();
    homePosition = transform.position;
    if (enemyHealth != null && typedStats != null)
    {
      enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.woodDrop, typedStats.stoneDrop, typedStats.ropeDrop);
    }
    var playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj) player = playerObj.transform;
    shotCooldown = 0f;
    biteCooldown = 0f;
  }

  void Update()
  {
    if (player == null || typedStats == null)
    {
      state = State.Idle;
      return;
    }

    if (shotCooldown > 0f) shotCooldown -= Time.deltaTime;
    if (biteCooldown > 0f) biteCooldown -= Time.deltaTime;

    // countdown retreat timer if active so retreat can't last forever
    if (retreatTimer > 0f) retreatTimer -= Time.deltaTime;

    // Use horizontal distance for 2D side-scroller decisions
    float dist = Vector2.Distance(player.position, transform.position);

    // Global: if player is too far, return to home
    if (returnToHomeDistance > 0f && dist > returnToHomeDistance)
    {
      state = State.ReturnHome;
      aggressiveRush = false;
      timeInShoot = 0f;
    }

    switch (state)
    {
      case State.Idle:
        if (dist <= ShootRange * 1.8f) state = State.Approach;
        break;

      case State.Approach:
        if (dist <= BiteRange && biteCooldown <= 0f) state = State.Bite;
        else if (!aggressiveRush && dist <= ShootRange) state = State.Shoot;
        else if (dist > ShootRange * 2.2f) state = State.Idle;
        break;

      case State.Shoot:
        // If player pushes too close while shooting, retreat instead of biting
        {
          float retreatThreshold = (retreatCloseThresholdOverride > 0f)
            ? retreatCloseThresholdOverride
            : Mathf.Max(BiteRange, ShootRange * 0.25f);
          if (dist <= retreatThreshold)
          {
            DoRetreatHop();
            state = State.Retreat;
            timeInShoot = 0f;
            aggressiveRush = false;
          }
          else if (dist > ShootRange * 1.3f)
          {
            state = State.Approach;
            timeInShoot = 0f;
            aggressiveRush = false;
          }
          else
          {
            // build up aggro time; once exceeded, roll chance to convert to aggressive rush
            timeInShoot += Time.deltaTime;
            if (!aggressiveRush && timeInShoot >= shootAggroTime)
            {
              if (Random.value < shootToBiteChance)
              {
                aggressiveRush = true; // will move faster in Approach until within bite range
                aggressiveLockTimer = aggressiveLockDuration; // lock approach briefly to ensure movement
                state = State.Approach; // switch to approach to close distance quickly
                timeInShoot = 0f; // reset so we don't immediately re-trigger logic after rush ends
              }
              else
              {
                timeInShoot = 0f; // reset and keep shooting
              }
            }
          }
        }
        break;

      case State.Bite:
        // Transition handled inside PerformBite
        break;

      case State.Retreat:
        // When far enough horizontally or retreat timer expired, go back to approach
        if (dist >= retreatTargetDistance || retreatTimer <= 0f)
        {
          state = State.Approach;
        }
        else
        {
          // allow an interrupt: if player chases closely while retreating, attempt a bite if off cooldown
          float biteInterruptDist = BiteRange * biteDuringRetreatDistanceFactor;
          if (dist <= biteInterruptDist && biteCooldown <= 0f)
          {
            state = State.Bite;
          }
        }
        break;

      case State.ReturnHome:
        // If player comes back within detection, resume normal behavior
        if (dist <= ShootRange * 1.5f)
        {
          state = State.Approach;
        }
        else
        {
          // If reached home X, idle
          if (Mathf.Abs(transform.position.x - homePosition.x) <= homeArriveThreshold)
          {
            state = State.Idle;
          }
        }
        break;
    }
  }

  void FixedUpdate()
  {
    if (player == null)
    {
      rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
      if (animator) animator.SetBool(walkBoolName, false);
      return;
    }

    switch (state)
    {
      case State.Idle:
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (animator) animator.SetBool(walkBoolName, false);
        break;

      case State.Approach:
        {
          float distX = Mathf.Abs(player.position.x - transform.position.x);
          if (distX <= BiteRange && biteCooldown <= 0f)
          {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (animator) animator.SetBool(walkBoolName, false);
            FacePlayer();
            PerformBite();
            aggressiveRush = false; // consumed rush
          }
          else
          {
            float speed = typedStats.moveSpeed * (aggressiveRush ? aggressiveSpeedMultiplier : 1f);
            MoveTowardPlayer(speed);
            if (animator) animator.SetBool(walkBoolName, true);
          }
        }
        break;

      case State.Shoot:
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (animator) animator.SetBool(walkBoolName, false);
        FacePlayer();
        TryShoot();
        break;

      case State.Bite:
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (animator) animator.SetBool(walkBoolName, false);
        FacePlayer();
        PerformBite();
        break;

      case State.Retreat:
        RetreatFromPlayer();
        if (animator) animator.SetBool(walkBoolName, true);
        break;

      case State.ReturnHome:
        MoveTowardX(homePosition.x, typedStats.moveSpeed);
        if (animator) animator.SetBool(walkBoolName, true);
        break;
    }
  }

  private void MoveTowardPlayer(float speed)
  {
    float dir = Mathf.Sign(player.position.x - transform.position.x);
    rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    FlipOnVelocity();
  }
  private void RetreatFromPlayer()
  {
    float dir = Mathf.Sign(transform.position.x - player.position.x);
    rb.linearVelocity = new Vector2(dir * typedStats.retreatSpeed, rb.linearVelocity.y);
    FlipOnVelocity();
  }

  private void FlipOnVelocity()
  {
    if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
    {
      Vector3 s = stats.baseScale;
      s.x = Mathf.Abs(s.x) * Mathf.Sign(rb.linearVelocity.x);
      transform.localScale = s;
    }
  }

  private void FacePlayer()
  {
    Vector3 s = stats.baseScale;
    s.x = Mathf.Abs(s.x) * (player.position.x >= transform.position.x ? 1f : -1f);
    transform.localScale = s;
  }

  private void TryShoot()
  {
    if (shotCooldown > 0f) return;
    if (spineProjectilePrefab != null)
    {
      Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
      var proj = Instantiate(spineProjectilePrefab, spawn.position, Quaternion.identity);
      if (proj != null && player != null)
      {
        // If the prefab has a SalamanderSpine component, use its LaunchAtTarget API so it properly ignores the owner and handles sticking.
        var spineComp = proj.GetComponent<SalamanderSpine>();
        if (spineComp != null)
        {
          // configure the spine's parameters from the salamander overrides
          spineComp.launchSpeed = projectileSpeed;
          // Launch and pass the salamander GameObject as the owner so it won't collide with itself
          spineComp.LaunchAtTarget(player, this.gameObject);
        }
        else
        {
          // fallback: simple linear velocity
          var rbp = proj.GetComponent<Rigidbody2D>();
          if (rbp != null)
          {
            Vector2 dir = (player.position - spawn.position).normalized;
            rbp.linearVelocity = dir * projectileSpeed;
          }
        }
      }
    }
    shotCooldown = timeBetweenShots;
  }

  private void PerformBite()
  {
    if (biteCooldown > 0f)
    {
      state = State.Approach;
      return;
    }
    biteCooldown = timeBetweenBites;
    ApplyBiteDamage();
    DoRetreatHop();
    state = State.Retreat;
    timeInShoot = 0f; // reset shoot tracking after a bite
    aggressiveRush = false; // rush finished
    if (retreatTimer < 0f) retreatTimer = 0f;
  }

  private void DoRetreatHop()
  {
    if (!rb || !player) return;
    float dir = Mathf.Sign(transform.position.x - player.position.x);
    rb.linearVelocity = Vector2.zero;
    rb.AddForce(new Vector2(dir * hopForceX, hopForceY), ForceMode2D.Impulse);
    // start retreat timer so the salamander won't run away indefinitely
    retreatTimer = maxRetreatDuration;
  }

  private void ApplyBiteDamage()
  {
    if (!typedStats) return;

    bool damaged = false;
    if (bitePoint != null)
    {
      int mask = biteHitMask.value != 0 ? biteHitMask.value : LayerMask.GetMask("Player");
      var hits = Physics2D.OverlapCircleAll(bitePoint.position, biteRadius, mask);
      foreach (var h in hits)
      {
        if (!IsPlayerCollider(h)) continue;
        var hs = FindHealth(h.transform);
        if (hs != null)
        {
          hs.TakeDamage(typedStats.biteDamage);
          damaged = true;
          break; // one target per bite
        }
      }
    }
    if (!damaged && player != null)
    {
      var hs = FindHealth(player);
      if (hs != null)
      {
        Vector2 source = bitePoint != null ? (Vector2)bitePoint.position : (Vector2)transform.position;
        float dist2D = Vector2.Distance(source, player.position);
        if (dist2D <= BiteRange * 1.1f) hs.TakeDamage(typedStats.biteDamage);
      }
    }
  }

  private void MoveTowardX(float targetX, float speed)
  {
    float dir = Mathf.Sign(targetX - transform.position.x);
    rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    FlipOnVelocity();
  }

  private HealthSystem FindHealth(Transform t)
  {
    if (!t) return null;
    if (t.TryGetComponent<HealthSystem>(out var hs)) return hs;
    hs = t.GetComponentInParent<HealthSystem>();
    if (hs) return hs;
    return t.GetComponentInChildren<HealthSystem>();
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
    // Always-on gizmos for authoring/debugging
    // Ranges
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(transform.position, typedStats != null ? (shootRangeOverride > 0f ? shootRangeOverride : typedStats.shootRange) : 0f);
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, typedStats != null ? (biteRangeOverride > 0f ? biteRangeOverride : typedStats.biteRange) : 0f);

    // Bite point
    if (bitePoint)
    {
      Gizmos.color = new Color(1f, 0.4f, 0f, 0.85f);
      Gizmos.DrawWireSphere(bitePoint.position, biteRadius);
      Gizmos.DrawLine(transform.position, bitePoint.position);
    }

    // Projectile spawn indicator
    if (projectileSpawnPoint)
    {
      Gizmos.color = Color.green;
      Gizmos.DrawSphere(projectileSpawnPoint.position, 0.1f);
      // simple aim line to player if present
      if (player)
      {
        Gizmos.DrawLine(projectileSpawnPoint.position, player.position);
      }
    }

    // Retreat target distance markers (left/right bounds)
    Gizmos.color = Color.yellow;
    Vector3 pos = transform.position;
    Vector3 left = pos + Vector3.left * retreatTargetDistance;
    Vector3 right = pos + Vector3.right * retreatTargetDistance;
    Gizmos.DrawLine(left + Vector3.down * 0.5f, left + Vector3.up * 0.5f);
    Gizmos.DrawLine(right + Vector3.down * 0.5f, right + Vector3.up * 0.5f);

    // Home position marker
    Gizmos.color = Color.blue;
    Vector3 home = Application.isPlaying ? homePosition : transform.position;
    Gizmos.DrawSphere(home, 0.08f);
    Gizmos.DrawLine(pos, home);

    // Facing arrow
    Gizmos.color = Color.white;
    float dir = Mathf.Sign(transform.localScale.x);
    Gizmos.DrawLine(pos, pos + Vector3.right * dir * 0.75f);
  }
}
