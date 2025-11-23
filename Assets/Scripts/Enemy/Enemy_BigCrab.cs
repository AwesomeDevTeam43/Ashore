using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Enemy_Health))]
public class BigCrab : EnemyBase
{
  public BigCrab_Stats typedStats;
  private Enemy_Health enemyHealth;
  private Transform player;
  private Rigidbody2D rb;
  private Animator animator;
  [SerializeField] private string walkBoolName = "isWalking"; // Animator bool
  [Header("Animation")]
  [SerializeField] private string attackTriggerName = "Attack"; // Animator trigger to start attack anim
  [SerializeField] private string attackBoolName = "isAttacking"; // Animator bool alternative
  [SerializeField] private bool attackUsesAnimationEvent = true; // Damage applied via AnimEvent_DoAttack
  [SerializeField] private float attackCommitDuration = 0.6f; // Fallback commit time when no events
  private bool hasAttackTrigger = false;
  private bool hasAttackBool = false;
  private bool attackInProgress = false; // Prevents leaving Attacking until finished
  private Coroutine attackCommitRoutine;
  public enum EnemyState { Idle, Chasing, Attacking }
  private EnemyState currentState;

  private float timeBtwAttack;

  private void Start()
  {
    typedStats = stats as BigCrab_Stats;
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
    EnemyState oldState = currentState;
    switch (currentState)
    {
      case EnemyState.Idle:
        // Transição: Idle -> Chasing
        if (distanceToPlayer <= typedStats.followPlayerRange)
        {
          currentState = EnemyState.Chasing;
        }
        break;

      case EnemyState.Chasing:
        // Transição: Chasing -> Attacking
        if (distanceToPlayer <= typedStats.attackRange)
        {
          currentState = EnemyState.Attacking;
        }
        // Transição: Chasing -> Idle
        else if (distanceToPlayer > typedStats.followPlayerRange)
        {
          currentState = EnemyState.Idle;
        }
        break;

      case EnemyState.Attacking:
        // Do not leave Attacking while committed to this attack
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
        // Move toward player like before and drive the animation from movement
        float moveDirection = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDirection * typedStats.speed, rb.linearVelocity.y);
        FlipSpriteOnMove(); // flip based on move dir
        if (animator != null) animator.SetBool(walkBoolName, true);
        if (animator != null && hasAttackBool) animator.SetBool(attackBoolName, false);
        break;

      case EnemyState.Attacking:
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        FacePlayer(); // Garante que está virado para o jogador
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
      if (typedStats.startTimeBtwAttack <= 0)
      {
        timeBtwAttack = 2f; // Define um padrão de 2s para evitar loop infinito
      }
      else
      {
        timeBtwAttack = typedStats.startTimeBtwAttack; // Reinicia o cooldown
      }
      // Begin an attack and commit to it until it completes
      BeginAttackCommit(maybeTimedFallback: true);
      if (attackUsesAnimationEvent)
      {
        // Start the attack animation; actual damage should be fired by AnimEvent_DoAttack
        if (animator != null)
        {
          if (hasAttackTrigger)
          {
            animator.SetTrigger(attackTriggerName);
          }
          else if (hasAttackBool)
          {
            // Bool is already being set in HandleStateActions
          }
          else
          {
            // Animator exists but no known params to drive attack; fallback to immediate
            CheckEnemyAttack();
          }
        }
        else
        {
          // No animator available; fallback to immediate damage
          CheckEnemyAttack();
        }
      }
      else
      {
        // Legacy behavior: apply damage immediately
        CheckEnemyAttack();
      }
    }
  }

  // Marks attack as committed, optionally also starting a timed fallback window
  private void BeginAttackCommit(bool maybeTimedFallback)
  {
    attackInProgress = true;
    // If an animation event will call AnimEvent_AttackEnd, we won't need a timer.
    bool expectAnimToEnd = attackUsesAnimationEvent && animator != null && (hasAttackTrigger || hasAttackBool);
    if (!expectAnimToEnd && maybeTimedFallback)
    {
      // Use a simple timed commit fallback
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

  // Public function to be called from an Animation Event at the hit frame
  public void AnimEvent_DoAttack()
  {
    CheckEnemyAttack();
  }

  // Optional: call at the start of the attack animation to ensure commitment
  public void AnimEvent_AttackStart()
  {
    // Cancel any fallback timer and mark commit on
    if (attackCommitRoutine != null)
    {
      StopCoroutine(attackCommitRoutine);
      attackCommitRoutine = null;
    }
    attackInProgress = true;
  }

  // Call at the end of the attack animation to release commitment
  public void AnimEvent_AttackEnd()
  {
    attackInProgress = false;
    if (attackCommitRoutine != null)
    {
      StopCoroutine(attackCommitRoutine);
      attackCommitRoutine = null;
    }
  }

  void CheckEnemyAttack()
  {
    if (player == null) return;

    HealthSystem playerHealth = null;
    Rigidbody2D playerRb = null;
    if (!player.TryGetComponent<HealthSystem>(out playerHealth))
    {
      playerHealth = player.GetComponentInParent<HealthSystem>();
      if (playerHealth != null)
      {
        playerRb = playerHealth.GetComponent<Rigidbody2D>();
      }
    }
    else
    {
      playerRb = player.GetComponent<Rigidbody2D>();
    }

    if (playerHealth != null && Physics2D.OverlapCircle(transform.position, typedStats.attackRange, LayerMask.GetMask("Player")) != null)
    {
      if (typedStats.biteDamage <= 0) Debug.LogWarning("Enemy: biteDamage <= 0");

      playerHealth.TakeDamage(typedStats.biteDamage);
      if (playerRb != null)
      {
        StartCoroutine(ApplyPlayerKnockback(playerRb, player));
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

  void OnDrawGizmos()
  {
    if (stats == null) return;

    var tStats = typedStats != null ? typedStats : stats as BigCrab_Stats;
    if (tStats == null) return;

    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, tStats.followPlayerRange);
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, tStats.attackRange);
  }
}