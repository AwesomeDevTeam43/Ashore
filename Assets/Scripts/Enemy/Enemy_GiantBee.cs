using UnityEngine;

public class BeeEnemy : MonoBehaviour
{
  public GiantBee_Stats stats;
  private Enemy_Health enemyHealth;

  private GameObject player;
  private Rigidbody2D rb;
  private Collider2D playerCollider;
  private HealthSystem playerHealth;

  private enum EnemyState { Roaming, Lunging, Retreating }
  private EnemyState enemyState;

  private float currentCooldown;
  private float lungeTimer;

  private Vector3 retreatTargetPosition;
  private Vector3 playerAttackPoint;
  private Vector3 lungeStartPosition;

  private void Start()
  {
    enemyHealth = GetComponent<Enemy_Health>();
    if (enemyHealth != null && stats != null)
    {
      enemyHealth.Initialize(stats.maxHealth, stats.xpOnDeath, stats.dropA, stats.dropB, stats.dropC);
    }

    rb = GetComponent<Rigidbody2D>();
    if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

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
  }

  private void Update()
  {
    if (player == null || stats == null) return;

    if (currentCooldown > 0f) currentCooldown -= Time.deltaTime;
    FlipSprite();

    float playerDistance = Vector3.Distance(transform.position, player.transform.position);
    StateMachine(playerDistance);
  }

  private void StateMachine(float playerDistance)
  {
    switch (enemyState)
    {
      case EnemyState.Roaming:
        RoamBehavior(playerDistance);
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
    if (playerDistance <= stats.playerDetect)
    {
      if (currentCooldown <= 0f && playerDistance <= stats.lungeRange)
      {
        StartLunge();
      }
      else if (currentCooldown <= 0f && playerDistance > stats.lungeRange)
      {
        Vector2 dir = (player.transform.position - transform.position).normalized;
        rb.linearVelocity = dir * stats.roamSpeed;
      }
      else
      {
        rb.linearVelocity = Vector2.zero;
      }
    }
    else
    {
      rb.linearVelocity = Vector2.zero;
    }
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
    Vector2 lungeDir = (playerAttackPoint - transform.position).normalized;
    rb.linearVelocity = lungeDir * stats.lungingForce;

    if (lungeTimer >= stats.lungeDuration || Vector2.Distance(transform.position, playerAttackPoint) < 0.3f)
    {
      EndLunge();
    }
  }

  private void EndLunge()
  {
    Vector2 retreatDir = (lungeStartPosition - transform.position).normalized;
    retreatTargetPosition = transform.position + (Vector3)retreatDir * stats.retreatRange;
    currentCooldown = stats.lungeCooldown;
    enemyState = EnemyState.Retreating;
    Debug.Log("Lunge ended, retreating!");
  }

  private void RetreatBehavior(float playerDistance)
  {
    Vector2 dir = (retreatTargetPosition - transform.position).normalized;
    rb.linearVelocity = dir * stats.retreatSpeed;

    if (Vector2.Distance(transform.position, retreatTargetPosition) < 0.5f)
    {
      rb.linearVelocity = Vector2.zero;
      enemyState = EnemyState.Roaming;
      Debug.Log("Retreat complete, back to roaming!");
    }
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (enemyState == EnemyState.Lunging && collision.gameObject == player)
    {
      Debug.Log("Stung the player! Retreating.");
      if (playerHealth != null)
      {
        playerHealth.TakeDamage(stats.stingDamage);
      }
      EndLunge();
    }
  }

  private void OnDrawGizmos()
  {
    if (stats == null) return;

    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, stats.playerDetect);

    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, stats.lungeRange);

    Gizmos.color = Color.blue;
    Gizmos.DrawWireSphere(transform.position, stats.retreatRange);

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