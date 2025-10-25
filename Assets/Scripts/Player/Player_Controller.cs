using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class Player_Controller : MonoBehaviour
{
  public static event Action OnPlayerLoad;
  private XP_System xP_System;
  private Player_Health playerHealth;

  [Header("Player Stats")]
  [SerializeField] private PlayerStats playerStats;

  [Header("Temporary Inventory")]
  [SerializeField] private Equipment currentEquipment;
  [SerializeField] private GameObject spearPrefab;
  private int currentAttackPower;
  private float currentMoveSpeed;
  private float currentJumpForce;

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
      if (!GameFlowState.IsLoading)
      {
        UpdateStats(1);
      }
    }
  }


private void Awake()
{
    xP_System = GetComponent<XP_System>();
    playerHealth = GetComponent<Player_Health>();

    if (xP_System != null)
    {
        xP_System.OnLevelUp += UpdateStats;
    }

    // Loading is handled in Start to avoid double init/reset order issues
}
// In Player_Controller.cs

private void Start()
{
    if (GameFlowState.LoadGameOnStart)
    {
      // A save file should be loaded.
      GameFlowState.IsLoading = true;
      LoadGame();
      GameFlowState.LoadGameOnStart = false; // Reset the flag
      GameFlowState.IsLoading = false;
    }
    else
    {
      // No save file to load, so start a fresh game.
      xP_System.Initialize(LVL1XpAmount, LvlGap);
    }
}


  private void OnDisable()
  {
    if (xP_System != null)
    {
      xP_System.OnLevelUp -= UpdateStats;
    }
  }

  private void Update()
  {
    useEquipment();

    if (Input.GetKeyDown(KeyCode.F5))
    {
        SaveGame();
    }
    if (Input.GetKeyDown(KeyCode.F9))
    {
        // We are now handling loading through the main menu and GameFlowState
        // so this key press is no longer needed for loading.
        // You could re-enable it for debugging if you wish.
        // LoadGame();
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
    currentAttackPower = playerStats.GetAttackPower(level);
    currentMoveSpeed = playerStats.GetMoveSpeed(level);
    currentJumpForce = playerStats.GetJumpForce(level);

    Debug.Log($"Stats updated! Level {level}: ATK={currentAttackPower}, Speed={currentMoveSpeed}, Jump={currentJumpForce}");
  }

  public void SaveGame()
  {
      SaveSystem.SavePlayer(this, xP_System, playerHealth, Inventory.instance);
  }

  public void LoadGame()
  {
      PlayerData data = SaveSystem.LoadPlayer();

      if (data != null)
      {
          // Re-initialize systems with saved state
          GetComponent<HealthSystem>().MaxHealth = data.maxHealth;
          playerHealth.SetHealth(data.currentHealth);
          xP_System.Initialize(data.level, data.currentXp, data.maxXp, LvlGap);

          // Restore other stats and notify other systems of the level change
          UpdateStats(data.level);
          // Restore other stats (attack/move/jump) based on level only

          // Restore Position
          transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);

          // Restore Inventory
          Inventory.instance.Clear();
          for (int i = 0; i < data.inventoryItemNames.Count; i++)
          {
              // NOTE: This requires a way to find ItemData by name.
              ItemData item = Resources.Load<ItemData>("Items/" + data.inventoryItemNames[i]);
              if (item != null)
              {
                  Inventory.instance.Add(item, data.inventoryItemQuantities[i]);
              }
          }

          // Restore Equipment
          currentEquipment = null;
          if (!string.IsNullOrEmpty(data.equippedItemName))
          {
              // You would need a way to find the equipment in the inventory and equip it.
              // This is a complex step that depends on your inventory and equipment system.
          }

          SaveSystem.RestoreWorldState(data);

          OnPlayerLoad?.Invoke();
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
  }
}