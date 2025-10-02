using UnityEngine;

public class BeeEnemy : MonoBehaviour
{
  [SerializeField] private float speed;
  private GameObject player;
  private HealthSystem healthSystem;
  [SerializeField] private int enemyHealth;

  [Header("Movement")]
  [SerializeField]  private float roamSpeed = 4f;
  [SerializeField]  private float lungingForce = 7f;
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

  private enum EnemyState {Roaming, Lunging, Retreating}
  private EnemyState enemyState;
  private Vector2 lungeStartPos;
  private Rigidbody2D rb;
  private Vector3 retreatTargetPosition;
  private Vector3 retreatDirection;
  private Collider2D playerCollider;

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
  }

  private void StateMachine(float playerDistance)
  {
    switch(enemyState)
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
        // For now, just wait for player to enter detection range
        if (playerDistance <= playerDetect)
        {
            // Move towards player until in lunge range
            Vector2 direction = (player.transform.position - transform.position).normalized;
            rb.linearVelocity = direction * roamSpeed;
            
            if (playerDistance <= lungeRange)
            {
                // Player is close enough, initiate lunge
                retreatStartPosition = transform.position;
                enemyState = EnemyState.Lunging;
            }
        }
        else
        {
            // Player is out of range, stay idle
            rb.linearVelocity = Vector2.zero;
        }
    }

    void LungeBehavior(float playerDistance)
    {
        // Lunge directly at the player
        Vector2 lungeDirection = (player.transform.position - transform.position).normalized;
        rb.linearVelocity = lungeDirection * lungingForce;
        
    }

    void RetreatBehavior()
    {
              // Move towards the retreat target
        Vector2 directionToTarget = (retreatTargetPosition - transform.position).normalized;
        rb.velocity = directionToTarget * retreatSpeed;

        // Check if we've reached the retreat position
        if (Vector2.Distance(transform.position, retreatTargetPosition) < 0.1f)
        {
            rb.velocity = Vector2.zero;
            enemyState = EnemyState.Roaming;
        }
    }

    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collision is with the player and we are in the Lunging state
        if (enemyState == EnemyState.Lunging && collision.gameObject == player)
        {
            Debug.Log("Stung the player! Retreating.");
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
        
        // Draw retreat direction when retreating
        if (Application.isPlaying && enemyState == EnemyState.Retreating)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, retreatTargetPosition);
            Gizmos.DrawWireSphere(retreatTargetPosition, 0.2f);
        }
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


  bool PlayeInRange(float range)
  {

   return false;
  }
}
