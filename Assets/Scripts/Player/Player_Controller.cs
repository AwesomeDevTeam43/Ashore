using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  private HealthSystem healthSystem;
  private XP_System xP_System;
  private Player_Atributes player_Atributes;
  [SerializeField] private float fallDeathY = -10f;
  
  [Header("Equipment")]
  [SerializeField] private SpearEquipment spearEquipment;
  private bool hasSpearInInventory = true;
  private bool isSpearEquipped = false;


  private void Awake()
  {
    player_Atributes = GetComponent<Player_Atributes>();

    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnPlayerHealthChanged;
    xP_System = GetComponent<XP_System>();


  }

  private void Start()
  {
    Debug.Log($"Initializing HealthSystem with {player_Atributes.Health} HP");
    healthSystem.Initialize(player_Atributes.Health);    
    xP_System.Initialize(player_Atributes.LVL1XpAmount, player_Atributes.LvlGap);
  }

  private void OnDisable()
  {
    healthSystem.OnHealthChanged -= OnPlayerHealthChanged;
  }

  private void Update()
  {

    if (Input.GetKeyDown(KeyCode.M))
    {
      healthSystem.Heal(1);
      Debug.Log($"{healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
    }
    if (transform.position.y < fallDeathY)
    {
      ReturnToLastPoint();
    }
  }

  public void ReturnToLastPoint()
  {
    transform.position = ReturnPointManager.GetReturnPoint();

    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb != null)
    {
      rb.linearVelocity = Vector2.zero;
    }
  }

  private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
  {
    if (currentHealth <= 0)
    {
      Destroy(gameObject);
      Debug.Log("Player Died!");
    }
  }
}

