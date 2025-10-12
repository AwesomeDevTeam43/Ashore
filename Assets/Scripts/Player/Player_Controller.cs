using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  private HealthSystem healthSystem;
  private XP_System xP_System;

  [Header("Player Stats")]
  [SerializeField] private PlayerStats playerStats;
  public bool IsAlive = true;

  [Header("Temporary Inventory")]
  [SerializeField] private Equipment currentEquipment;
  [SerializeField] private GameObject spearPrefab;
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
  public Equipment CurrentEquipment => currentEquipment;

  // Called by EquipmentManager (or other systems) to set the player's current equipment
  public void SetCurrentEquipment(Equipment eq)
  {
    currentEquipment = eq;
    if (currentEquipment != null)
    {
      currentEquipment.isEquipped = true;
    }
  }

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
        currentEquipment.isEquipped = false;
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

      // Update max health and set current health
      healthSystem.MaxHealth = currentHealth;
      healthSystem.SetHealth(newCurrentHealth);
    }

    Debug.Log($"Stats updated! Level {level}: HP={healthSystem.CurrentHealth}/{currentHealth}, ATK={currentAttackPower}, Speed={currentMoveSpeed}, Jump={currentJumpForce}");
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
    Equipment equipment = collision.GetComponent<Equipment>();
    if (equipment != null && !equipment.isEquipped)
    {
      // If this is a thrown spear that's not ready, ignore the trigger (prevents instant re-pickup after throw)
      Spear thrownSpearCheck = equipment as Spear;
      if (thrownSpearCheck != null && !thrownSpearCheck.CanBePickedUp())
      {
        Debug.Log("Spear not ready to be picked up yet (early exit)");
        return;
      }
      // If this world equipment has inventory data, try to add it to the inventory first
      if (equipment.equipmentData != null)
      {
        bool added = Inventory.instance.Add(equipment.equipmentData);
        if (added)
        {
          Debug.Log("Picked up item: " + equipment.equipmentData.itemName);
          Destroy(collision.gameObject);
          return;
        }
        else
        {
          Debug.Log("Inventory full, cannot pick up: " + equipment.equipmentData.itemName);
        }
      }
      // Check if it's a spear and if it can be picked up
      Spear spear = equipment as Spear;
      if (spear != null)
      {
        if (spear.CanBePickedUp())
        {
          Debug.Log("Picked up " + equipment.name);

          // If we don't have a spear prefab reference, store it
          if (spearPrefab == null)
          {
            spearPrefab = equipment.gameObject;
          }

          currentEquipment = spearPrefab.GetComponent<Equipment>();
          currentEquipment.isEquipped = true;

          Destroy(collision.gameObject);
        }
        else
        {
          Debug.Log("Spear not ready to be picked up yet");
        }
      }
      else if (equipment.hasLanded) // Other equipment
      {
        Debug.Log("Picked up " + equipment.name);
        currentEquipment = equipment;
        Destroy(collision.gameObject);
      }
    }

    if (collision.gameObject.layer == LayerMask.NameToLayer("FallLevel"))
    {
      ReturnToLastPoint();
      healthSystem.TakeDamage(1);
    }
  }



  private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
  {
    if (currentHealth <= 0)
    {
    IsAlive = false;
    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
  }
}