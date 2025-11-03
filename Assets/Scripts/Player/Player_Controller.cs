using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class Player_Controller : MonoBehaviour
{
  public static event Action OnPlayerLoad;
  private XP_System xP_System;
  private Player_Health playerHealth;
  private Rigidbody2D rb2d;
  private Rigidbody rb3d;

  [Header("Animation")]
  [SerializeField] private Animator animator; // Assign in Inspector or auto-fetch
  [Tooltip("Float parameter to drive movement speed (optional). Leave empty to skip.")]
  [SerializeField] private string speedParam = "Speed";
  [Tooltip("Bool parameter to toggle running state (optional). Leave empty to skip.")]
  [SerializeField] private string isRunningParam = "IsRunning";
  [Tooltip("Speed threshold above which we consider the player running.")]
  [SerializeField] private float runThreshold = 0.1f;
  private bool hasSpeedParam = false;
  private bool hasIsRunningParam = false;

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
  rb2d = GetComponent<Rigidbody2D>();
  rb3d = GetComponent<Rigidbody>();
  if (animator == null) animator = GetComponent<Animator>();
  if (animator != null)
  {
    foreach (var p in animator.parameters)
    {
      if (!hasSpeedParam && !string.IsNullOrEmpty(speedParam) && p.name == speedParam && p.type == AnimatorControllerParameterType.Float)
        hasSpeedParam = true;
      if (!hasIsRunningParam && !string.IsNullOrEmpty(isRunningParam) && p.name == isRunningParam && p.type == AnimatorControllerParameterType.Bool)
        hasIsRunningParam = true;
    }
  }

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

    UpdateAnimationParameters();

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

  private void UpdateAnimationParameters()
  {
    if (animator == null) return;

    float speed = 0f;
    if (rb2d != null)
      speed = rb2d.linearVelocity.magnitude;
    else if (rb3d != null)
      speed = rb3d.linearVelocity.magnitude;

    if (hasSpeedParam)
      animator.SetFloat(speedParam, speed);
    if (hasIsRunningParam)
      animator.SetBool(isRunningParam, speed > runThreshold);
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
    Debug.Log("Saving game...");
    SaveSystem.SavePlayer(this, xP_System, playerHealth, Inventory.instance);
  }



  public void LoadGame()
  {
      Debug.Log("Loading game...");

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
      // Prefer resource names if available; fall back to item names
      var namesList = (data.inventoryResourceNames != null && data.inventoryResourceNames.Count > 0)
        ? data.inventoryResourceNames
        : data.inventoryItemNames;

      for (int i = 0; i < namesList.Count && i < data.inventoryItemQuantities.Count; i++)
      {
        string key = namesList[i];
        ItemData item = Resources.Load<ItemData>("Items/" + key);
        if (item != null)
        {
          Inventory.instance.Add(item, data.inventoryItemQuantities[i]);
        }
        else
        {
          Debug.LogWarning($"LoadGame: Could not find ItemData at Resources/Items/{key}. Skipping.");
        }
      }

          // Restore Equipment
          currentEquipment = null;
      // Prefer resource key for equipment
      string equipKey = !string.IsNullOrEmpty(data.equippedResourceName) ? data.equippedResourceName : data.equippedItemName;
      if (!string.IsNullOrEmpty(equipKey))
      {
        // Try to load EquipmentData by resource key and equip it
        EquipmentData eqData = Resources.Load<EquipmentData>("Items/" + equipKey);
        if (eqData != null)
        {
          if (EquipmentManager.instance != null)
          {
            EquipmentManager.instance.EquipFromInventory(eqData);
          }
          else
          {
            // Fallback: direct instantiate and equip
            if (eqData.equipmentPrefab != null)
            {
              GameObject equipObj = Instantiate(eqData.equipmentPrefab, transform);
              equipObj.name = eqData.itemName + "_InventoryHolder";
              equipObj.SetActive(false);
              Equipment eq = equipObj.GetComponent<Equipment>();
              if (eq != null)
              {
                SetCurrentEquipment(eq);
                eq.isEquipped = true;
                eq.Equip();
                // Attempt removing from inventory if present (no-op if not there)
                Inventory.instance.Remove(eqData);
              }
            }
          }
        }
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

  private void OnTriggerStay2D(Collider2D collision)
  {
    if (LayerMask.LayerToName(collision.gameObject.layer) == "RestPoint")
    {
      if (Input.GetKeyDown(KeyCode.B))
      {
        playerHealth.SetHealth(playerHealth.GetComponent<HealthSystem>().MaxHealth);
        SaveGame();
      }
    }
  }
   
  private void OnTriggerEnter2D(Collider2D collision)
  {
    if (LayerMask.LayerToName(collision.gameObject.layer) == "RestPoint")
    {
      if (Input.GetKeyDown(KeyCode.B))
      {
      playerHealth.SetHealth(playerHealth.GetComponent<HealthSystem>().MaxHealth);
      SaveGame();
    }
  }


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