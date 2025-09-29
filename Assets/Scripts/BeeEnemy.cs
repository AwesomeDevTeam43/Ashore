using UnityEngine;

public class BeeEnemy : MonoBehaviour
{
  [SerializeField] private float speed;
  private GameObject player;
  private HealthSystem healthSystem;
  [SerializeField] private int enemyHealth;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnHealthChanged;
  }

  private void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player");
    healthSystem.Initialize(enemyHealth);
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

  void OnHealthChanged(float current, float max)
  {
    Debug.Log($"Health changed {current} {max}");
    if (current <= 0)
    {
      Destroy(gameObject);
    }
  }
}
