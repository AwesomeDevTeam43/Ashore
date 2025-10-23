using UnityEngine;
using UnityEngine.SceneManagement;

public class Player_Controller : MonoBehaviour
{
  private XP_System xP_System;

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
      UpdateStats(1);
    }
  }

  private void Awake()
  {
    xP_System = GetComponent<XP_System>();

    if (xP_System != null)
    {
      xP_System.OnLevelUp += UpdateStats;
    }
  }

  private void Start()
  {
    xP_System.Initialize(LVL1XpAmount, LvlGap);
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