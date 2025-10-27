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
  public enum EnemyState { Idle, Chasing, Attacking }
  private EnemyState currentState;

  private float timeBtwAttack;

  private void Start()
  {
    typedStats = stats as BigCrab_Stats;
    enemyHealth = GetComponent<Enemy_Health>();
    rb = GetComponent<Rigidbody2D>();

    if (enemyHealth != null && stats != null)
    {
      enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.dropA, typedStats.dropB, typedStats.dropC);
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
        if (distanceToPlayer > typedStats.attackRange)
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
      return;
    }

    switch (currentState)
    {
      case EnemyState.Idle:
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        break;

      case EnemyState.Chasing:
        float moveDirection = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDirection * typedStats.speed, rb.linearVelocity.y);
        FlipSpriteOnMove(); // Vira o sprite com base na direção do movimento
        break;

      case EnemyState.Attacking:
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        FacePlayer(); // Garante que está virado para o jogador
        AttemptAttack();
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
      CheckEnemyAttack();
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

    if (playerHealth != null)
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
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, typedStats.followPlayerRange);
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, typedStats.attackRange);
  }
}