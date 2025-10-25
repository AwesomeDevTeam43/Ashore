using UnityEngine;

public class BeeEnemy : MonoBehaviour
{
  [SerializeField] private float speed;
  private GameObject player;
  private HealthSystem healthSystem;
  private XP_System xP_System;

  [Header("Movement")]
  [SerializeField] private float roamSpeed = 4f;
  [SerializeField] private float lungingForce = 7f;
  [SerializeField] private float retreatSpeed = 6f;

  [Header("Detection")]
  [SerializeField] private float playerDetect = 8f;
  [SerializeField] private float lungeRange = 4f;
  [SerializeField] private float retreatRange = 5f;
  [SerializeField] private LayerMask playerLayer;

  [Header("Timers")]
  [SerializeField] private float lungeCooldown = 2f;
  [SerializeField] private float currentCooldown;
  [SerializeField] private float lungeDuration = 1f;
  private float lungeTimer;

  private enum EnemyState { Roaming, Lunging, Retreating }
  private EnemyState enemyState;
  private Rigidbody2D rb;
  private Vector3 retreatTargetPosition;
  private Vector3 playerAttackPoint;
  private Vector3 lungeStartPosition;
  private Collider2D playerCollider; // NEW: Reference to player's 
  private HealthSystem playerHealth;
  private Drop_Materials drop_Materials;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
  }

  private void Start()
  {
    rb = GetComponent<Rigidbody2D>();
    player = GameObject.FindGameObjectWithTag("Player");
    playerHealth = player.GetComponent<HealthSystem>();
    drop_Materials = GetComponent<Drop_Materials>();

    // NEW: Get player's collider for feet targeting
    if (player != null)
    {
      playerCollider = player.GetComponent<Collider2D>();
      xP_System = player.GetComponent<XP_System>();
    }

    enemyState = EnemyState.Roaming;
    currentCooldown = 0f;
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

  private void Update()
  {
    if (player == null)
      return;

    if (currentCooldown > 0)
      currentCooldown -= Time.deltaTime;
    FlipSprite();
    float playerDistance = Vector3.Distance(transform.position, player.transform.position);
    StateMachine(playerDistance);
  }

  void RoamBehavior(float playerDistance)
  {
    if (playerDistance <= playerDetect)
    {
      if (currentCooldown <= 0 && playerDistance <= lungeRange)
      {
        StartLunge();
      }
      else if (currentCooldown <= 0 && playerDistance > lungeRange)
      {
        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        rb.linearVelocity = directionToPlayer * roamSpeed;
      }
      else if (currentCooldown > 0)
      {
        rb.linearVelocity = Vector2.zero;
      }
    }
    else
    {
      rb.linearVelocity = Vector2.zero;
    }
  }

  void StartLunge()
  {
    lungeStartPosition = transform.position;

    // NEW: Calculate attack point at player's feet
    playerAttackPoint = GetPlayerFeetPosition();

    lungeTimer = 0f;
    enemyState = EnemyState.Lunging;
    Debug.Log("Starting lunge towards player's feet!");
  }

  // NEW: Method to get player's feet position
  Vector3 GetPlayerFeetPosition()
  {
    if (playerCollider != null)
    {
      // Get the bottom of the player's collider
      Bounds bounds = playerCollider.bounds;
      return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }
    else
    {
      // Fallback: position slightly below player's transform
      return player.transform.position + Vector3.down * 0.5f;
    }
  }

  void LungeBehavior(float playerDistance)
  {
    lungeTimer += Time.deltaTime;

    Vector2 lungeDirection = (playerAttackPoint - transform.position).normalized;
    rb.linearVelocity = lungeDirection * lungingForce;

    if (lungeTimer >= lungeDuration || Vector2.Distance(transform.position, playerAttackPoint) < 0.3f)
    {
      EndLunge();
    }
  }

  void EndLunge()
  {
    Vector2 retreatDirection = (lungeStartPosition - transform.position).normalized;
    retreatTargetPosition = transform.position + (Vector3)retreatDirection * retreatRange;

    currentCooldown = lungeCooldown;
    enemyState = EnemyState.Retreating;
    Debug.Log("Lunge ended, retreating!");
  }

  void RetreatBehavior(float playerDistance)
  {
    Vector2 directionToTarget = (retreatTargetPosition - transform.position).normalized;
    rb.linearVelocity = directionToTarget * retreatSpeed;

    if (Vector2.Distance(transform.position, retreatTargetPosition) < 0.5f)
    {
      rb.linearVelocity = Vector2.zero;
      enemyState = EnemyState.Roaming;
      Debug.Log("Retreat complete, back to roaming!");
    }
  }

  void OnCollisionEnter2D(Collision2D collision)
  {
    if (enemyState == EnemyState.Lunging && collision.gameObject == player)
    {
      Debug.Log("Stung the player! Retreating.");
      playerHealth.TakeDamage(2);
      EndLunge();
    }
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, playerDetect);

    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, lungeRange);

    Gizmos.color = Color.blue;
    Gizmos.DrawWireSphere(transform.position, retreatRange);

    // Show player's feet attack point
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

  void FlipSprite()
  {
    if (player.transform.position.x <= 0.01f)
    {
      transform.localScale = new Vector3(-2, 2, 1);
    }
    else if (player.transform.position.x >= -0.01f)
    {
      transform.localScale = new Vector3(2, 2, 1);
    }
  }
}