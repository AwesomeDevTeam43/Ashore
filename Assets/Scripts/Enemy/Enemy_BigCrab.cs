using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Enemy_Health))]
public class Enemy : MonoBehaviour
{
  public BigCrab_Stats stats;
  private Enemy_Health enemyHealth;
  private Transform player;
  private Rigidbody2D rb;
  public enum EnemyState { Idle, Chasing, Attacking }
  private EnemyState currentState;

  private float timeBtwAttack; 

  private void Start()
  {
    enemyHealth = GetComponent<Enemy_Health>();
    rb = GetComponent<Rigidbody2D>();

    if (enemyHealth != null && stats != null)
    {
      enemyHealth.Initialize(stats.maxHealth, stats.xpOnDeath, stats.dropA, stats.dropB, stats.dropC);
    }
    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
    if (playerObject != null)
    {
      player = playerObject.transform;
    }
    else
    {
      Debug.LogError("Enemy: Jogador não encontrado. Verifique se ele tem a tag 'Player'.");
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
        if (distanceToPlayer <= stats.followPlayerRange)
        {
          currentState = EnemyState.Chasing;
        }
        break;

      case EnemyState.Chasing:
        // Transição: Chasing -> Attacking
        if (distanceToPlayer <= stats.attackRange)
        {
          currentState = EnemyState.Attacking;
        }
        // Transição: Chasing -> Idle
        else if (distanceToPlayer > stats.followPlayerRange)
        {
          currentState = EnemyState.Idle;
        }
        break;

      case EnemyState.Attacking:
        if (distanceToPlayer > stats.attackRange)
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
        // Ação: Parar
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        break;

      case EnemyState.Chasing:
        // Ação: Mover em direção ao jogador
        float moveDirection = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDirection * stats.speed, rb.linearVelocity.y);
        FlipSpriteOnMove(); // Vira o sprite com base na direção do movimento
        break;

      case EnemyState.Attacking:
        // Ação: Parar de se mover e tentar atacar
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        FacePlayer(); // Garante que está virado para o jogador
        AttemptAttack(); // Tenta executar o ataque (controlado pelo timer)
        break;
    }
  }

  private void AttemptAttack()
  {
    if (timeBtwAttack <= 0)
    {
      if (stats.startTimeBtwAttack <= 0)
      {
        timeBtwAttack = 2f; // Define um padrão de 2s para evitar loop infinito
      }
      else
      {
        timeBtwAttack = stats.startTimeBtwAttack; // Reinicia o cooldown
      }
      CheckEnemyAttack();
    }
  }

  void CheckEnemyAttack()
  {
    if (player == null) return;

    HealthSystem playerHealth = null;
    if (!player.TryGetComponent<HealthSystem>(out playerHealth))
    {
      playerHealth = player.GetComponentInParent<HealthSystem>();
    }

    if (playerHealth != null)
    {
      if (stats.biteDamage <= 0) Debug.LogWarning("Enemy: biteDamage <= 0");
      playerHealth.TakeDamage(stats.biteDamage);
      Debug.Log("Player Hit! (Ataque Ativo)");
      Debug.Log(playerHealth.CurrentHealth);
    }
    else
    {
      Debug.LogWarning("Enemy: Player HealthSystem not found on assigned player Transform.");
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
    Gizmos.DrawWireSphere(transform.position, stats.followPlayerRange);
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, stats.attackRange);
  }
}