using UnityEngine;

public class BeeEnemy : EnemyBase
{
  private GiantBee_Stats typedStats;
  private GameObject player;
  private Rigidbody2D rb;
  private Collider2D col2D;
  private Collider2D playerCollider;
  private HealthSystem playerHealth;

  [Header("Audio")]
  private AudioSource sfxSource;
  private AudioSource windupSource;
  private AudioSource lungeSource;
  private AudioSource retreatSource;
  [SerializeField] private AudioClip windupSfx;
  [SerializeField] private AudioClip lungeSfx;
  [SerializeField] private AudioClip hitSfx;
  [SerializeField] private AudioClip retreatSfx;
  [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
  [SerializeField] private bool randomizePitch = true;
  [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

  [Header("Audio - Idle")]
  [SerializeField] private AudioSource idleSource;
  [SerializeField] private AudioClip idleLoopSfx;
  [SerializeField, Range(0f, 1f)] private float idleVolume = 0.5f;

  [Header("Audio - Distance")]
  [SerializeField] private Transform listenerOverride;
  [SerializeField] private float minAudibleDistance = 2f;
  [SerializeField] private float maxAudibleDistance = 12f;
  private Transform listenerTransform;

  private enum EnemyState { Roaming, AttackWindup, Lunging, Retreating }
  private EnemyState enemyState;

  private float currentCooldown;
  private float lungeTimer;
  [Header("Recovery")]
  [SerializeField] private float stuckVelocityThreshold = 0.05f;
  [SerializeField] private float stuckTimeThreshold = 0.8f;
  [SerializeField] private int maxRecoveryAttempts = 5;
  private int recoveryAttemptCount = 0;
  private float stuckTimer = 0f;

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
    }

    player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerHealth = player.GetComponent<HealthSystem>();
      playerCollider = player.GetComponent<Collider2D>();
    }

    if (stats == null)
    {
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

    if (sfxSource == null)
    {
      sfxSource = GetComponent<AudioSource>();
    }
    if (sfxSource == null)
    {
      sfxSource = gameObject.AddComponent<AudioSource>();
    }
    if (sfxSource != null)
    {
      sfxSource.playOnAwake = false;
      sfxSource.loop = false;
      // 2D by default. If you want 3D attenuation, set Spatial Blend in the Inspector.
      sfxSource.spatialBlend = 0f;
      if (AudioManager.Instance != null)
      {
        var g = AudioManager.Instance.GetSFXGroup();
        if (g != null) sfxSource.outputAudioMixerGroup = g;
      }
    }
    
    if (idleSource == null)
    {
      // Prefer a different AudioSource so one-shots (attack SFX) don't cut the idle loop.
      var sources = GetComponents<AudioSource>();
      if (sources != null)
      {
        foreach (var src in sources)
        {
          if (src != null && src != sfxSource)
          {
            idleSource = src;
            break;
          }
        }
      }
    }
    if (idleSource == null)
    {
      idleSource = gameObject.AddComponent<AudioSource>();
    }
    if (idleSource != null)
    {
      idleSource.playOnAwake = false;
      idleSource.loop = true;
      idleSource.spatialBlend = 0f;
      idleSource.volume = idleVolume;
      if (idleLoopSfx != null) idleSource.clip = idleLoopSfx;
      if (AudioManager.Instance != null)
      {
        var g = AudioManager.Instance.GetSFXGroup();
        if (g != null) idleSource.outputAudioMixerGroup = g;
      }
    }

    if (retreatSource == null)
    {
      // Dedicated source so we can Stop() the retreat clip as soon as retreat ends.
      var sources = GetComponents<AudioSource>();
      if (sources != null)
      {
        foreach (var src in sources)
        {
          if (src != null && src != sfxSource && src != idleSource)
          {
            retreatSource = src;
            break;
          }
        }
      }
    }
    if (retreatSource == null)
    {
      retreatSource = gameObject.AddComponent<AudioSource>();
    }
    if (retreatSource != null)
    {
      retreatSource.playOnAwake = false;
      retreatSource.loop = false;
      retreatSource.spatialBlend = 0f;
      if (AudioManager.Instance != null)
      {
        var g = AudioManager.Instance.GetSFXGroup();
        if (g != null) retreatSource.outputAudioMixerGroup = g;
      }
    }

    if (windupSource == null)
    {
      var sources = GetComponents<AudioSource>();
      if (sources != null)
      {
        foreach (var src in sources)
        {
          if (src != null && src != sfxSource && src != idleSource && src != retreatSource)
          {
            windupSource = src;
            break;
          }
        }
      }
    }
    if (windupSource == null)
    {
      windupSource = gameObject.AddComponent<AudioSource>();
    }
    if (windupSource != null)
    {
      windupSource.playOnAwake = false;
      windupSource.loop = false;
      windupSource.spatialBlend = 0f;
      if (AudioManager.Instance != null)
      {
        var g = AudioManager.Instance.GetSFXGroup();
        if (g != null) windupSource.outputAudioMixerGroup = g;
      }
    }

    if (lungeSource == null)
    {
      var sources = GetComponents<AudioSource>();
      if (sources != null)
      {
        foreach (var src in sources)
        {
          if (src != null && src != sfxSource && src != idleSource && src != retreatSource && src != windupSource)
          {
            lungeSource = src;
            break;
          }
        }
      }
    }
    if (lungeSource == null)
    {
      lungeSource = gameObject.AddComponent<AudioSource>();
    }
    if (lungeSource != null)
    {
      lungeSource.playOnAwake = false;
      lungeSource.loop = false;
      lungeSource.spatialBlend = 0f;
      if (AudioManager.Instance != null)
      {
        var g = AudioManager.Instance.GetSFXGroup();
        if (g != null) lungeSource.outputAudioMixerGroup = g;
      }
    }

    ResolveListenerTransform();
  }

  private void ResolveListenerTransform()
  {
    if (listenerOverride != null)
    {
      listenerTransform = listenerOverride;
      return;
    }

    // Prefer the player as reference (camera usually follows player).
    if (player != null)
    {
      listenerTransform = player.transform;
      return;
    }

    if (Camera.main != null)
    {
      listenerTransform = Camera.main.transform;
      return;
    }

    var al = FindFirstObjectByType<AudioListener>();
    if (al != null) listenerTransform = al.transform;
  }

  private float GetDistanceAttenuation01()
  {
    if (maxAudibleDistance <= 0f) return 1f;
    if (listenerTransform == null) ResolveListenerTransform();
    if (listenerTransform == null) return 1f;

    float minD = Mathf.Max(0f, minAudibleDistance);
    float maxD = Mathf.Max(minD + 0.0001f, maxAudibleDistance);
    float d = Vector2.Distance((Vector2)transform.position, (Vector2)listenerTransform.position);
    if (d >= maxD) return 0f;
    if (d <= minD) return 1f;
    return 1f - Mathf.InverseLerp(minD, maxD, d);
  }

  private void PlaySfx(AudioClip clip)
  {
    if (clip == null || sfxSource == null) return;

    float att = GetDistanceAttenuation01();
    if (att <= 0.0001f) return;

    float originalPitch = sfxSource.pitch;
    if (randomizePitch)
    {
      float min = Mathf.Min(pitchRange.x, pitchRange.y);
      float max = Mathf.Max(pitchRange.x, pitchRange.y);
      sfxSource.pitch = Random.Range(min, max);
    }

    sfxSource.PlayOneShot(clip, sfxVolume * att);
    sfxSource.pitch = originalPitch;
  }

  private void Update()
  {
    if (player == null || stats == null) return;

    if (currentCooldown > 0f) currentCooldown -= Time.deltaTime;
    FlipSprite();

    float playerDistance = Vector3.Distance(transform.position, player.transform.position);
    StateMachine(playerDistance);
    
    UpdateIdleAudio();
    UpdateRetreatAudio();
    UpdateStateSfxAudio();
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
  
  private void UpdateIdleAudio()
  {
    if (idleSource == null) return;

    float att = GetDistanceAttenuation01();

    // Keep clip/volume in sync with Inspector changes.
    if (idleSource.clip != idleLoopSfx) idleSource.clip = idleLoopSfx;
    idleSource.volume = idleVolume * att;

    bool shouldPlay = (enemyState == EnemyState.Roaming);
    if (idleLoopSfx == null) shouldPlay = false;
    if (att <= 0.0001f) shouldPlay = false;

    if (shouldPlay)
    {
      if (!idleSource.isPlaying)
      {
        idleSource.Play();
      }
    }
    else
    {
      if (idleSource.isPlaying)
      {
        idleSource.Stop();
      }
    }
  }

  private void UpdateRetreatAudio()
  {
    if (retreatSource == null) return;
    if (retreatSource.clip != retreatSfx) retreatSource.clip = retreatSfx;

    float att = GetDistanceAttenuation01();
    retreatSource.volume = sfxVolume * att;

    // If it's too far to hear, stop to avoid a long clip continuing silently.
    if (retreatSource.isPlaying && att <= 0.0001f)
    {
      retreatSource.Stop();
    }
  }

  private void PlayRetreatSfx()
  {
    if (retreatSfx == null || retreatSource == null) return;

    float att = GetDistanceAttenuation01();
    if (att <= 0.0001f) return;

    retreatSource.Stop();
    retreatSource.clip = retreatSfx;
    retreatSource.volume = sfxVolume * att;
    retreatSource.Play();
  }

  private void StopRetreatSfx()
  {
    if (retreatSource == null) return;
    if (retreatSource.isPlaying) retreatSource.Stop();
  }

  private void UpdateStateSfxAudio()
  {
    float att = GetDistanceAttenuation01();

    if (windupSource != null)
    {
      if (windupSource.clip != windupSfx) windupSource.clip = windupSfx;
      windupSource.volume = sfxVolume * att;
      if (windupSource.isPlaying && att <= 0.0001f) windupSource.Stop();
    }

    if (lungeSource != null)
    {
      if (lungeSource.clip != lungeSfx) lungeSource.clip = lungeSfx;
      lungeSource.volume = sfxVolume * att;
      if (lungeSource.isPlaying && att <= 0.0001f) lungeSource.Stop();
    }
  }

  private void PlayWindupSfx()
  {
    if (windupSfx == null || windupSource == null) return;

    float att = GetDistanceAttenuation01();
    if (att <= 0.0001f) return;

    windupSource.Stop();
    windupSource.clip = windupSfx;
    windupSource.volume = sfxVolume * att;
    windupSource.Play();
  }

  private void StopWindupSfx()
  {
    if (windupSource == null) return;
    if (windupSource.isPlaying) windupSource.Stop();
  }

  private void PlayLungeSfx()
  {
    if (lungeSfx == null || lungeSource == null) return;

    float att = GetDistanceAttenuation01();
    if (att <= 0.0001f) return;

    lungeSource.Stop();
    lungeSource.clip = lungeSfx;
    lungeSource.volume = sfxVolume * att;
    lungeSource.Play();
  }

  private void StopLungeSfx()
  {
    if (lungeSource == null) return;
    if (lungeSource.isPlaying) lungeSource.Stop();
  }

  private void OnDisable()
  {
    StopWindupSfx();
    StopLungeSfx();
    StopRetreatSfx();
  }

  private void FixedUpdate()
  {
    // Pre-move collision query to prevent tunneling through Ground/MovingPlatform even at high speeds.
    // If blocked we try a small nudge away from the contact normal (or from the overlapping collider)
    // instead of always zeroing velocity — this helps avoid getting permanently stuck during retreat.
    float stepDist = desiredVelocity.magnitude * Time.fixedDeltaTime;
    Vector2 origin = transform.position;
    if (stepDist > 0.0001f)
    {
      Vector2 dir = desiredVelocity.normalized;
      float rad = Mathf.Max(nodeRadius, GetClearance());
      var hit = Physics2D.CircleCast(origin, rad, dir, stepDist, obstacleMask);
      if (hit.collider != null)
      {
        float desiredSpeed = desiredVelocity.magnitude;
        // If collision is extremely close (we're essentially overlapping), try to nudge away
        if (hit.distance <= 0.02f)
        {
          Vector2 nudge = hit.normal;
          float nudgeSpeed = Mathf.Max(desiredSpeed * 0.6f, 0.5f);
          desiredVelocity = nudge.normalized * nudgeSpeed;
          repathTimer = 0f;
        }
        else
        {
          // Blocked ahead: stop and force a quick repath
          desiredVelocity = Vector2.zero;
          repathTimer = 0f;
        }
      }
    }
    else
    {
      // If we have no intended motion but are overlapping an obstacle (stuck), push outwards
      float rad = Mathf.Max(nodeRadius, GetClearance());
      var oc = Physics2D.OverlapCircle(origin, rad, obstacleMask);
      if (oc != null)
      {
        Vector2 closest = oc.ClosestPoint(origin);
        Vector2 push = (origin - closest);
        if (push.sqrMagnitude > 0.0001f)
        {
          float pushSpeed = 0.5f;
          if (typedStats != null) pushSpeed = Mathf.Max(pushSpeed, typedStats.retreatSpeed * 0.4f);
          desiredVelocity = push.normalized * pushSpeed;
          repathTimer = 0f;
        }
      }
    }

    // Apply velocity in physics step
    rb.linearVelocity = desiredVelocity;

    // Stuck detection/recovery: if we're retreating and barely moving for a while,
    // attempt a recovery by choosing an alternate retreat target and forcing a repath.
    if (enemyState == EnemyState.Retreating)
    {
      float speed = rb.linearVelocity.magnitude;
      if (speed < stuckVelocityThreshold && desiredVelocity.magnitude < stuckVelocityThreshold)
      {
        stuckTimer += Time.fixedDeltaTime;
        if (stuckTimer >= stuckTimeThreshold)
        {
          stuckTimer = 0f;
          repathTimer = 0f;
          // If we've already tried too many recovery attempts, give up and go back to roaming
          if (recoveryAttemptCount >= maxRecoveryAttempts)
          {
            recoveryAttemptCount = 0;
            desiredVelocity = Vector2.zero;
            enemyState = EnemyState.Roaming;
            StopWindupSfx();
            StopLungeSfx();
            StopRetreatSfx();
          }
          else
          {
            // pick an alternative retreat target in a random direction at retreatRange
            recoveryAttemptCount++;
            Vector2 altDir = Random.insideUnitCircle.normalized;
            retreatTargetPosition = transform.position + (Vector3)(altDir * (typedStats != null ? typedStats.retreatRange : 2f));
            FollowPathTowards(retreatTargetPosition, typedStats != null ? typedStats.retreatSpeed : 1f);
          }
        }
      }
      else
      {
        stuckTimer = 0f;
        recoveryAttemptCount = 0;
      }
    }
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
    float leashRange = typedStats.playerDetect * 1.0f; // configurable multiplier (now 1.0 to match gizmo)
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
    StopLungeSfx();
    StopRetreatSfx();
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

    PlayWindupSfx();

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

    StopWindupSfx();
    PlayLungeSfx();
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
    StopWindupSfx();
    StopLungeSfx();
    Vector2 retreatDir = (lungeStartPosition - transform.position).normalized;
    retreatTargetPosition = transform.position + (Vector3)retreatDir * typedStats.retreatRange;
    currentCooldown = typedStats.lungeCooldown;
    enemyState = EnemyState.Retreating;
    if (animator != null)
    {
      animator.SetBool(isLungingParam, false);
      animator.SetBool(isRetreatingParam, true);
    }

    PlayRetreatSfx();
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
      // Finished retreat: return to roaming and allow immediate reactions to the player.
      // Clear the lunge cooldown so the Bee can respond if the player re-approaches.
      currentCooldown = 0f;
      repathTimer = 0f; // force a fresh path next frame
      desiredVelocity = Vector2.zero;
      enemyState = EnemyState.Roaming;
      StopRetreatSfx();
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
        Vector2 dirToTarget = (target - transform.position).sqrMagnitude > 0.0001f ? (target - transform.position).normalized : Vector2.right;

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
          if (NavGrid2D.Instance == null)
          {
            pathfinder.Configure(big, nodeRadius, obstacleMask);
            pathfinder.SetClearance(GetClearance());
          }

          // Try with base centers
          foreach (var c in centers)
          {
            var p = NavGrid2D.Instance != null
              ? NavGrid2D.Instance.FindPath(transform.position, target)
              : pathfinder.FindPath(transform.position, target, c);

            if (p != null)
            {
              gridWorldSize = big;
              currentPath.AddRange(p);
              found = true;
              break;
            }
          }
          if (found) break;

          // Try a few offset centers around the midpoint to handle edge cases
          float offsetDist = Mathf.Max(1f, Mathf.Min(big.x, big.y) * 0.25f);
          Vector2 perp = new Vector2(-dirToTarget.y, dirToTarget.x);
          Vector2 mid = (Vector2)((transform.position + target) * 0.5f);
          Vector2[] offsetCenters = new Vector2[]
          {
            mid + dirToTarget * offsetDist,
            mid - dirToTarget * offsetDist,
            mid + perp * offsetDist,
            mid - perp * offsetDist,
          };

          foreach (var c in offsetCenters)
          {
            var p = NavGrid2D.Instance != null
              ? NavGrid2D.Instance.FindPath(transform.position, target)
              : pathfinder.FindPath(transform.position, target, c);

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
    // Prefer precise per-collider estimates for clearance so the pathfinder and
    // collision checks treat the Bee's physical footprint correctly.
    if (col2D is CircleCollider2D cc)
    {
      float scale = Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
      return cc.radius * scale * 0.6f;
    }

    if (col2D is CapsuleCollider2D cap)
    {
      float scale = Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
      return Mathf.Max(cap.size.x, cap.size.y) * 0.3f * scale;
    }

    // Fallback: compute a conservative radius from the collider's world bounds
    // which works for BoxCollider2D, PolygonCollider2D and other collider types.
    float clearance = nodeRadius * 0.75f;
    if (col2D != null)
    {
      Bounds b = col2D.bounds; // world-space bounds
      float halfDiagonal = 0.5f * Mathf.Sqrt(b.size.x * b.size.x + b.size.y * b.size.y);
      clearance = Mathf.Max(clearance, halfDiagonal);
    }

    // If a stinger hitbox exists and extends beyond the body, ensure clearance
    // accounts for it as well (prevents the stinger from clipping geometry).
    if (stingerHitbox != null)
    {
      Bounds sb = stingerHitbox.bounds;
      float stingerHalfDiag = 0.5f * Mathf.Sqrt(sb.size.x * sb.size.x + sb.size.y * sb.size.y);
      clearance = Mathf.Max(clearance, stingerHalfDiag);
    }

    // Don't allow an absurdly small clearance; keep a reasonable minimum.
    clearance = Mathf.Max(clearance, nodeRadius * 0.5f);

    return clearance;
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (enemyState == EnemyState.Lunging && collision.gameObject == player)
    {
      if (playerHealth != null)
      {
        playerHealth.TakeDamage((int)currentDamage);

        PlaySfx(hitSfx);
        
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
    // Draw the player detect radius (explicit) and the leash radius the AI actually uses.
    Gizmos.DrawWireSphere(transform.position, ts.playerDetect);
    float leashRange = ts.playerDetect * 1.0f; // multiplier kept in sync with AI logic
    if (!Mathf.Approximately(leashRange, ts.playerDetect))
    {
      Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
      Gizmos.DrawWireSphere(transform.position, leashRange);
    }

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