using UnityEngine;

public class BeeEnemy : MonoBehaviour
{
  [SerializeField] private float speed;
  private GameObject player;
  private HealthSystem healthSystem;
  private XP_System xP_System;
  [SerializeField] private int enemyHealth;

  [Header("Movement")]
  [SerializeField] private float roamSpeed = 4f;
  [SerializeField] private float lungingForce = 7f;
  [SerializeField] private float retreatSpeed = 6f;
  private Vector2 roamTarget;


  [Header("Detection")]
  [SerializeField] private float playerDetect = 8f;
  [SerializeField] private float lungeRange = 4f;
  [SerializeField] private float retreatRange = 6f;
  [SerializeField] private LayerMask playerLayer;

  [Header("Timers")]
  [SerializeField] private float roamTime = 3f;
  [SerializeField] private float lungeCooldown = 2f;
  [SerializeField] private float currentTime;
  [SerializeField] private float currentCooldown;

  private enum EnemyState { Roaming, Lunging, Retreating }
  private EnemyState enemyState;
  private Vector2 lungeStartPos;
  private Rigidbody2D rb;
  private Vector3 retreatTargetPosition;
  private Vector3 retreatDirection;
  private Collider2D playerCollider;
  private Vector3 playerAttackPoint;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnHealthChanged;
  }

  private void Start()
  {
    rb = GetComponent<Rigidbody2D>();
    player = GameObject.FindGameObjectWithTag("Player");
    playerCollider = player.GetComponent<Collider2D>();
    healthSystem.Initialize(enemyHealth);
    enemyState = EnemyState.Roaming;

    if (player != null)
    {
      xP_System = player.GetComponent<XP_System>();
    }
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
        RetreatBehavior();
        break;
    }
  }


  private void Update()
  {
    if (player == null)
      return;

    float playerDistance = Vector3.Distance(transform.position, player.transform.position);


    StateMachine(playerDistance);

  }

  void RoamBehavior(float playerDistance)
  {
    if (playerDistance <= playerDetect)
    {
      Vector2 direction = (player.transform.position - transform.position).normalized;
      rb.linearVelocity = direction * roamSpeed;

      if (playerDistance <= lungeRange)
      {
        retreatTargetPosition = transform.position;
        playerAttackPoint = player.transform.position;
        enemyState = EnemyState.Lunging;
      }
    }
    else
    {
      rb.linearVelocity = Vector2.zero;
    }
  }

  void LungeBehavior(float playerDistance)
  {
    Vector2 lungeDirection = (playerAttackPoint - transform.position).normalized;
    rb.linearVelocity = lungeDirection * lungingForce;

    if (playerDistance <= lungeRange + 1)
    {
      enemyState = EnemyState.Roaming;
    }

  }

  void RetreatBehavior()
  {
    Vector2 directionToTarget = (retreatTargetPosition - transform.position).normalized;
    rb.linearVelocity = directionToTarget * retreatSpeed;

    if (Vector2.Distance(transform.position, retreatTargetPosition) < 0.1f)
    {
      rb.linearVelocity = Vector2.zero;
      enemyState = EnemyState.Roaming;
    }
  }


  void OnCollisionEnter2D(Collision2D collision)
  {
    if (enemyState == EnemyState.Lunging && collision.gameObject == player)
    {
      Debug.Log("Stung the player! Retreating.");

      retreatDirection = (transform.position - playerAttackPoint).normalized;

      retreatTargetPosition = transform.position + (retreatDirection * retreatRange);

      enemyState = EnemyState.Retreating;

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

    if (Application.isPlaying && enemyState == EnemyState.Retreating)
    {
      Gizmos.color = Color.green;
      Gizmos.DrawLine(transform.position, retreatTargetPosition);
      Gizmos.DrawWireSphere(retreatTargetPosition, 0.2f);
    }
  }


  void OnHealthChanged(int current, int max)
  {
    Debug.Log($"Health changed {current} {max}");
    if (current <= 0)
    {
      Destroy(gameObject);
      if (xP_System != null)
      {
        xP_System.DropXP(transform.position, 3);
      }
    }
  }


  bool PlayeInRange(float range)
  {

    return false;
  }
}
