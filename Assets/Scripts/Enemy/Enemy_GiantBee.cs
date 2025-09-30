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


  [Header("Detection")]
  [SerializeField] private float playerDetect = 8f;
  [SerializeField] private float lungeRange = 4f;
  [SerializeField] private float retreatRange = 6f;

  [Header("Timers")]
  [SerializeField] private float roamTime = 3f;
  [SerializeField] private float lungeCooldown = 2f;
  [SerializeField] private float currentTime;
  [SerializeField] private float currentCooldown;

  private enum EnemyState {Roaming, Lunging, Retreating}
  private EnemyState enemyState;
  private Vector2 lungeStartPos;
  private Rigidbody2D rb;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnHealthChanged;
  }

  private void Start()
  {
    rb = GetComponent<Rigidbody2D>();
    player = GameObject.FindGameObjectWithTag("Player");
    healthSystem.Initialize(enemyHealth);
    enemyState = EnemyState.Roaming;
  }

  private void StateMachine()
  {
    switch(enemyState)
    {
      case EnemyState.Roaming:
        Debug.Log("Roaming State for debug");
        break;

      case EnemyState.Lunging:
        Debug.Log("Lunging State for Debug");
        break;

      case EnemyState.Retreating:
        Debug.Log("Retreating State for Debug");
        break;
    }
  }


  private void Update()
  {
    if (player == null)
      return;


    Chase();
  }

  private void Chase()
  {
    transform.position=Vector2.MoveTowards(transform.position, player.transform.position, speed*Time.deltaTime);
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
