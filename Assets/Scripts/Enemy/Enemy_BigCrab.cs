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
  XP_System xP_System;
  private Rigidbody2D rb;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnHealthChanged;
  }

  private void Start()
  {
    healthSystem.Initialize(health);
    rb = GetComponent<Rigidbody2D>();
    if (rb == null)
    {
      rb = gameObject.AddComponent<Rigidbody2D>();
    }
    if (player != null)
    {
      xP_System = player.GetComponent<XP_System>();
    }
  }

  private void Update()
  {
    if (player == null)
      return;

    FlipSprite();
    CheckPlayerDistance();
  }


  private void FixedUpdate()
  {
    if (player == null)
    {
      return;
    }
    if (inRange)
    {
      transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.fixedDeltaTime);
    }
  }

  void CheckPlayerDistance()
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
        CheckEnemyAttack();
        Debug.Log("Levaste nos dentes do caranguejo");
        timeBtwAttack = startTimeBtwAttack;
      }
      else
      {
        timeBtwAttack -= Time.deltaTime;
      }
    }
  }

  void CheckEnemyAttack()
  {

    if (player != null)
    {
      HealthSystem playerHealth = player.GetComponent<HealthSystem>();
      if (playerHealth != null)
      {
        playerHealth.TakeDamage(biteDamage);
        Debug.Log("Player Hit!");
        Debug.Log(playerHealth.CurrentHealth);
      }
    }
  }

  void OnHealthChanged(int current, int max)
  {
    Debug.Log($"Enemy Health changed {current} {max}");
    if (current <= 0)
    {
      Destroy(gameObject);
      if (xP_System != null)
      {
        xP_System.DropXP(transform.position, 3);
      }
    }
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.layer == LayerMask.NameToLayer("FallLevel"))
    {
      healthSystem.TakeDamage(healthSystem.CurrentHealth);
      if (xP_System != null)
      {
        xP_System.DropXP(transform.position, 3);
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
  void FlipSprite()
  {
    if (player.transform.position.x <= 0.01f)
    {
      transform.localScale = new Vector3(-3.34f, 3.34f, 1);
    }
    else if (player.transform.position.x >= -0.01f)
    {
      transform.localScale = new Vector3(3.34f, 3.34f, 1);
    }
  }
}
