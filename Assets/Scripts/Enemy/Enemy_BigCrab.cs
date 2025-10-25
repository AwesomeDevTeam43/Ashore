using UnityEngine;

public class Enemy : MonoBehaviour
{
  public BigCrab_Stats stats;
  private Enemy_Health enemyHealth;
  private bool inRange;
  private float timeBtwAttack;
  public Transform player;

  private Rigidbody2D rb;

  private void Start()
  {
    enemyHealth = GetComponent<Enemy_Health>();
    if (enemyHealth != null && stats != null)
    {
      enemyHealth.Initialize(stats.maxHealth, stats.xpOnDeath, stats.dropA, stats.dropB, stats.dropC);
    }

    rb = GetComponent<Rigidbody2D>();
    if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

    if (stats != null) timeBtwAttack = 0f;
  }

  private void Update()
  {
    if (player == null || stats == null)
      return;

    FlipSprite();
    CheckPlayerDistance();
  }

  private void FixedUpdate()
  {
    if (player == null || stats == null)
      return;
    if (inRange)
    {
      transform.position = Vector2.MoveTowards(transform.position, player.position, stats.speed * Time.fixedDeltaTime);
    }
  }

  void CheckPlayerDistance()
  {
    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    if (distanceToPlayer <= stats.followPlayerRange)
    {
      inRange = true;
    }
    else
    {
      inRange = false;
    }

    if (distanceToPlayer <= stats.attackRange)
    {
      if (timeBtwAttack <= 0)
      {
        CheckEnemyAttack();
        timeBtwAttack = stats.startTimeBtwAttack;
      }
      else
      {
        timeBtwAttack -= Time.deltaTime;
      }
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
      Debug.Log("Player Hit!");
      Debug.Log(playerHealth.CurrentHealth);
    }
    else
    {
      Debug.LogWarning("Enemy: Player HealthSystem not found on assigned player Transform.");
    }
  }

  void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.collider.CompareTag("Player"))
    {
      var hp = collision.collider.GetComponent<HealthSystem>() ?? collision.collider.GetComponentInParent<HealthSystem>();
      if (hp != null)
      {
        hp.TakeDamage(stats.biteDamage);
        Debug.Log("Enemy contact: damaged player by " + stats.biteDamage);
      }
      else
      {
        Debug.LogWarning("Enemy contact: Player HealthSystem not found.");
      }
    }
  }

  void OnDrawGizmos()
  {
    if (stats == null) return;
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, stats.followPlayerRange);
    Gizmos.color = Color.blue;
    Gizmos.DrawWireSphere(transform.position, stats.attackRange);
  }

  void FlipSprite()
  {
    if (stats == null || player == null) return;
    // direção: 1 para direita, -1 para esquerda (evita 0)
    float dir = (player.position.x >= transform.position.x) ? 1f : -1f;
    Vector3 s = stats.baseScale;
    s.x = Mathf.Abs(s.x) * dir;
    transform.localScale = s;
  }
}
