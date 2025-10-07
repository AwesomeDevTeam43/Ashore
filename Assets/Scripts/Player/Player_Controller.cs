using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  private HealthSystem healthSystem;
  private XP_System xP_System;

  [Header("Player Stats")]
  [SerializeField] private PlayerStats playerStats;

  [Header("Settings")]
  [SerializeField] private float fallDeathY = -10f;

  [Header("Temporary Inventory")]
  [SerializeField] private Equipment currentEquipment;
  private int currentHealth;
  private int currentAttackPower;
  private float currentMoveSpeed;
  private float currentJumpForce;

  public int Health => currentHealth;
  public int AttackPower => currentAttackPower;
  public float MoveSpeed => currentMoveSpeed;
  public float JumpForce => currentJumpForce;
  public int LVL1XpAmount => playerStats != null ? playerStats.Level1XpAmount : 0;
  public int LvlGap => playerStats != null ? playerStats.LevelGap : 0;

  private void OnEnable()
  {
    if (playerStats != null)
    {
      UpdateStats(1);
    }
  }

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnPlayerHealthChanged;
    xP_System = GetComponent<XP_System>();

    if (xP_System != null)
    {
      xP_System.OnLevelUp += UpdateStats;
    }
  }

  private void Start()
  {
    Debug.Log($"Initializing HealthSystem with {currentHealth} HP");
    healthSystem.Initialize(currentHealth);
    xP_System.Initialize(LVL1XpAmount, LvlGap);

  }


  private void OnDisable()
  {
    healthSystem.OnHealthChanged -= OnPlayerHealthChanged;

    if (xP_System != null)
    {
      xP_System.OnLevelUp -= UpdateStats;
    }
  }

  private void Update()
  {
    useEquipment();
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

  void useEquipment()
  {
    if (currentEquipment != null)
    {
      if (Input.GetKeyDown(KeyCode.F) && currentEquipment.isEquipped)
      {
        currentEquipment.Unequip();
      }
      else if (Input.GetKeyDown(KeyCode.F) && !currentEquipment.isEquipped)
      {
        currentEquipment.Equip();
      }

      if (Input.GetKeyDown(KeyCode.N) && currentEquipment.isEquipped)
      {
        currentEquipment.Use();
        Debug.Log("Used Equipment");
        currentEquipment = null;
      }
    }
  }


  private void UpdateStats(int level)
  {
    int previousMaxHealth = currentHealth;
    int currentHealthPoints = healthSystem != null ? healthSystem.CurrentHealth : 0;

    currentHealth = playerStats.GetHealth(level);
    currentAttackPower = playerStats.GetAttackPower(level);
    currentMoveSpeed = playerStats.GetMoveSpeed(level);
    currentJumpForce = playerStats.GetJumpForce(level);

    if (healthSystem != null)
    {
      int healthDifference = currentHealth - previousMaxHealth;

      int newCurrentHealth = currentHealthPoints + healthDifference;

      healthSystem.Initialize(currentHealth);

      if (healthDifference > 0)
      {
        healthSystem.Heal(healthDifference);
      }
    }

    Debug.Log($"Stats updated! Level {level}: HP={currentHealth}, ATK={currentAttackPower}, Speed={currentMoveSpeed}, Jump={currentJumpForce}");
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

  private void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.layer == LayerMask.NameToLayer("Equipment"))
    {
      currentEquipment = collision.gameObject.GetComponent<Equipment>();
      Debug.Log($"Picked up {currentEquipment.equipmentName} - {currentEquipment.description}");
    }
  }

  private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
  {

  }
}