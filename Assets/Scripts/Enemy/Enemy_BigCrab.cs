using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Enemy_Health))]
public class BigCrab : EnemyBase
{
  public BigCrab_Stats typedStats;
  private Transform player;
  private Rigidbody2D rb;
  private Animator animator;
  [SerializeField] private string walkBoolName = "isWalking";
  [Header("Animation")]
  [SerializeField] private string attackTriggerName = "Attack";
  [SerializeField] private string attackBoolName = "isAttacking";
  [SerializeField] private bool attackUsesAnimationEvent = true;
  [SerializeField] private float attackCommitDuration = 0.6f;
  private bool hasAttackTrigger = false;
  private bool hasAttackBool = false;
  private bool attackInProgress = false;
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

    // REMOVER ESTA INICIALIZAÇÃO - Será feita pelo SpawnManager via ApplyLevelMultipliers
    // if (enemyHealth != null && stats != null)
    // {
    //   enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.woodDrop, typedStats.stoneDrop, typedStats.ropeDrop, typedStats.meleeResistance, typedStats.rangedResistance);
    // }
    
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
        rb.linearVelocity = new Vector2(moveDirection * typedStats.speed, rb.linearVelocity.y);
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
      if (typedStats.startTimeBtwAttack <= 0)
      {
        timeBtwAttack = 2f;
      }
      else
      {
        timeBtwAttack = typedStats.startTimeBtwAttack;
      }
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
          }
          else
          {
            CheckEnemyAttack();
          }
        }
        else
        {
          CheckEnemyAttack();
        }
      }
      else
      {
        CheckEnemyAttack();
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

  public void AnimEvent_DoAttack()
  {
    CheckEnemyAttack();
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

      // Usar currentDamage ao invés de biteDamage para aplicar o dano escalado
      playerHealth.TakeDamage((int)currentDamage, gameObject);
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