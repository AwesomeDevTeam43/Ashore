using UnityEngine;

public class Enemy : MonoBehaviour
{

  public int health;
  public float speed;
  public float followPlayerRange;
  private bool inRange;
  public float attackRange;
  public float startTimeBtwAttack;
  private float timeBtwAttack;
  public int biteDamage;
  public Transform player;

  HealthSystem healthSystem;

  private Rigidbody2D rb;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnHealthChanged;
  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  private void Start()
  {
    healthSystem.Initialize(health);
    rb = GetComponent<Rigidbody2D>();
    if (rb == null)
    {
      rb = gameObject.AddComponent<Rigidbody2D>();
    }
  }

  private void Update()
  {
    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    if (distanceToPlayer <= followPlayerRange)
    {
      inRange = true;
    }
    else
    {
      inRange = false;
    }

    if (distanceToPlayer <= attackRange)
    {
      if (timeBtwAttack <= 0)
      {
        Attack();
        Debug.Log("Attack!");
        timeBtwAttack = startTimeBtwAttack;
      }
      else
      {
        timeBtwAttack -= Time.deltaTime;
      }
    }
  }
  private void FixedUpdate()
  {
    if (inRange)
    {
      transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.fixedDeltaTime);
    }
  }

  void OnHealthChanged(int current, int max)
  {
    Debug.Log($"Health changed {current} {max}");
    if (current <= 0)
    {
      Destroy(gameObject);
    }
  }

  void Attack()
  {
    if (player != null)
    {
      HealthSystem playerHealthSystem = player.GetComponent<HealthSystem>();
      if (playerHealthSystem != null)
      {
        playerHealthSystem.TakeDamage(biteDamage, gameObject);
        Debug.Log("Player takes damage: " + biteDamage);
        Debug.Log("Player current health: " + playerHealthSystem.CurrentHealth);
      }
    }
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, followPlayerRange);
    Gizmos.color = Color.blue;
    Gizmos.DrawWireSphere(transform.position, attackRange);
  }
}
