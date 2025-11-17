using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class Player_Controller : MonoBehaviour
{
  public static event System.Action<Equipment> OnEquipmentUsed;
  public static event Action OnPlayerLoad;
  private XP_System xP_System;
  private Player_Health playerHealth;
  private Rigidbody2D rb2d;
  private Rigidbody rb3d;
  private Player_Movement playerMovement;

  [Header("Animation")]
  [SerializeField] private Animator animator; // Assign in Inspector or auto-fetch
  
  [Tooltip("Float parameter to drive movement speed (optional). Leave empty to skip.")]
  [SerializeField] private string speedParam = "Speed";
  [Tooltip("Bool parameter to toggle running state (optional). Leave empty to skip.")]
  [SerializeField] private string isRunningParam = "IsRunning";
  [Tooltip("Bool parameter to indicate jumping/airborne state (optional).")]
  [SerializeField] private string isJumpingParam = "isJumping";
  [Tooltip("Vertical distance (in units) at which we consider the player about to land and should play landing frames.")]
  [SerializeField] private float landingDetectDistance = 0.6f;
  [Tooltip("Float parameter name to pass vertical velocity to the Animator (optional).")]
  [SerializeField] private string yVelParam = "YVel";
  [Tooltip("Trigger parameter name to play landing animation (optional).")]
  [SerializeField] private string landTrigger = "Land";
  [Tooltip("Trigger parameter name to play jump start animation (optional).")]
  [SerializeField] private string jumpStartTrigger = "JumpStart";
  [Tooltip("Speed threshold above which we consider the player running.")]
  [SerializeField] private float runThreshold = 0.1f;
  private bool hasSpeedParam = false;
  private bool hasIsRunningParam = false;
  private bool hasIsJumpingParam = false;
  private bool hasYVelParam = false;
  private bool hasLandTrigger = false;
  private bool hasJumpStartTrigger = false;
  private bool landingTriggered = false;
  private bool prevGrounded = true;
  // true when the player has performed a jump (JumpStart fired) and hasn't landed yet
  private bool hasJumped = false;
  [Tooltip("Minimum time (seconds) between landing trigger firings to avoid repeats.")]
  [SerializeField] private float landingCooldown = 0.35f;
  private float landingCooldownTimer = 0f;

  [Header("Input")]
  [Tooltip("Optional: reference to the Player_InputHandler to use input-driven animation toggles.")]
  [SerializeField] private Player_InputHandler playerInputHandler;
  [Tooltip("Input magnitude deadzone used to decide whether player is giving movement input.")]
  [SerializeField] private float inputDeadzone = 0.01f;

  [Header("Player Stats")]
  [SerializeField] private PlayerStats playerStats;

  [Header("Temporary Inventory")]
  [SerializeField] private Equipment currentEquipment;
  [SerializeField] private GameObject spearPrefab;
  // Remember the last equipment the player had equipped so we can auto re-equip on pickup
  [SerializeField] private EquipmentData lastEquippedData;
  
  public enum MainWeaponType { Melee, Ranged }
  [Header("Combat")]
  [SerializeField] private MainWeaponType currentMainWeapon = MainWeaponType.Melee;
  public MainWeaponType CurrentMainWeapon => currentMainWeapon;
  public void SetMainWeapon(MainWeaponType type) { currentMainWeapon = type; }

  private int currentAttackPower;
  private float currentMoveSpeed;
  private float currentJumpForce;

  public int AttackPower => currentAttackPower;
  public float MoveSpeed => currentMoveSpeed;
  public float JumpForce => currentJumpForce;
  public int LVL1XpAmount => playerStats != null ? playerStats.Level1XpAmount : 0;
  public int LvlGap => playerStats != null ? playerStats.LevelGap : 0;
  public Equipment CurrentEquipment => currentEquipment;
  public EquipmentData LastEquippedData => lastEquippedData;

  // Called by EquipmentManager (or other systems) to set the player's current equipment
  public void SetCurrentEquipment(Equipment eq)
  {
    currentEquipment = eq;
    if (currentEquipment != null)
    {
      currentEquipment.isEquipped = true;
      // Track this as the last equipped item for future auto-equip on pickup
      if (currentEquipment.equipmentData != null)
      {
        lastEquippedData = currentEquipment.equipmentData;
      }
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
  playerInputHandler = GetComponent<Player_InputHandler>();
  playerMovement = GetComponent<Player_Movement>();
  if (animator == null) animator = GetComponent<Animator>();
  if (animator != null)
  {
    foreach (var p in animator.parameters)
    {
      if (!hasSpeedParam && !string.IsNullOrEmpty(speedParam) && p.name == speedParam && p.type == AnimatorControllerParameterType.Float)
        hasSpeedParam = true;
      if (!hasIsRunningParam && !string.IsNullOrEmpty(isRunningParam) && p.name == isRunningParam && p.type == AnimatorControllerParameterType.Bool)
        hasIsRunningParam = true;
      if (!hasIsJumpingParam && !string.IsNullOrEmpty(isJumpingParam) && p.name == isJumpingParam && p.type == AnimatorControllerParameterType.Bool)
        hasIsJumpingParam = true;
      if (!hasYVelParam && !string.IsNullOrEmpty(yVelParam) && p.name == yVelParam && p.type == AnimatorControllerParameterType.Float)
        hasYVelParam = true;
      if (!hasLandTrigger && !string.IsNullOrEmpty(landTrigger) && p.name == landTrigger && p.type == AnimatorControllerParameterType.Trigger)
        hasLandTrigger = true;
      if (!hasJumpStartTrigger && !string.IsNullOrEmpty(jumpStartTrigger) && p.name == jumpStartTrigger && p.type == AnimatorControllerParameterType.Trigger)
        hasJumpStartTrigger = true;
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
    {
      // Prefer input-driven running/walking intent if a Player_InputHandler is available.
      bool hasMovementInput = false;
      if (playerInputHandler != null)
      {
        Vector2 mv = playerInputHandler.MovementInput;
        hasMovementInput = mv.sqrMagnitude > (inputDeadzone * inputDeadzone);
      }
      else
      {
        // Fallback to legacy axes if no input handler is provided; keeps previous behavior.
        float inputX = 0f;
        float inputY = 0f;
        try { inputX = Input.GetAxisRaw("Horizontal"); inputY = Input.GetAxisRaw("Vertical"); } catch { }
        hasMovementInput = Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f;
      }
      animator.SetBool(isRunningParam, hasMovementInput);
    }

    // Handle jumping/landing animation and multi-stage jump animation triggers.
    if (hasIsJumpingParam || hasYVelParam || hasLandTrigger || hasJumpStartTrigger)
    {
      bool grounded = false;
      if (playerMovement != null)
        grounded = playerMovement.IsGrounded();
      else if (rb2d != null)
        grounded = Mathf.Abs(rb2d.linearVelocity.y) < 0.01f; // fallback

      float vertVel = 0f;
      if (rb2d != null) vertVel = rb2d.linearVelocity.y;

      // Set vertical velocity float (optional)
      if (hasYVelParam)
        animator.SetFloat(yVelParam, vertVel);

      // update landing cooldown timer
      if (landingCooldownTimer > 0f)
        landingCooldownTimer -= Time.deltaTime;

      // Detect jump start (takeoff) — when we were grounded and now not grounded and moving upward
      if (prevGrounded && !grounded && vertVel > 0.1f)
      {
        if (hasJumpStartTrigger)
        {
          animator.SetTrigger(jumpStartTrigger);
          // mark that the player initiated a jump so landing will only trigger after a real jump
          hasJumped = true;
        }
        landingTriggered = false; // reset landing trigger for this airtime
      }

      // During airtime, determine falling state. We intentionally DO NOT perform a raycast-based
      // landing detection here; landing will only be triggered when the player's collider
      // actually collides with ground (see OnCollisionEnter2D). This avoids the mid animation
      // being skipped by an early proximity check.
      if (!grounded)
      {
        // optionally set isJumping bool while airborne
        if (hasIsJumpingParam)
        {
          animator.SetBool(isJumpingParam, true);
        }
        // No raycast landing here — collision will trigger landing.
      }
      else
      {
        // grounded: clear states
        if (hasIsJumpingParam)
        {
          animator.SetBool(isJumpingParam, false);
        }
  landingTriggered = false;
        // Ensure JumpStart trigger can't remain latched while on ground
        if (hasJumpStartTrigger)
        {
          animator.ResetTrigger(jumpStartTrigger);
          // Also clear any lingering jump state if physics reports grounded
          if (hasJumped && grounded)
            hasJumped = false;
        }
      }

      prevGrounded = grounded;
    }
  }

  // Also trigger landing when the player's collider actually collides with ground layers.
  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (!hasLandTrigger) return;

    // If we have a Player_Movement combined mask, check it; otherwise check default ground (layer 0)
    int mask = (playerMovement != null) ? playerMovement.CombinedGroundLayers : (1 << collision.gameObject.layer);
    bool isGroundLayer = (((1 << collision.gameObject.layer) & mask) != 0);
    // Only trigger landing if collision is with ground and we are falling (negative vertical velocity)
    float vertVel = (rb2d != null) ? rb2d.linearVelocity.y : 0f;
    if (isGroundLayer && vertVel <= 0f && !landingTriggered && landingCooldownTimer <= 0f)
    {
      // only fire Land when the player has actually jumped (avoid triggering on walk collisions)
      if (hasJumped)
      {
        animator.SetTrigger(landTrigger);
        hasJumped = false;
      }
      landingTriggered = true;
      landingCooldownTimer = landingCooldown;
    }

    // Ensure isJumping is cleared on collision
    if (hasIsJumpingParam)
    {
      animator.SetBool(isJumpingParam, false);
    }
  }
  

  // Reuse RangeAttack input as the unified "Use Equipment" action.
  // F toggles equip/unequip as before; using equipment consumes it.
  private bool rangeUseHoldConsumed = false;
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

      // Use equipment on RangeAttack input press (performed). Process once per hold.
      if (playerInputHandler != null)
      {
        if (playerInputHandler.RangeAttackTriggered && !rangeUseHoldConsumed && currentEquipment.isEquipped)
        {
          var used = currentEquipment;
          used.Use();
          Debug.Log("Used Equipment (RangeAttack)");
          OnEquipmentUsed?.Invoke(used);
          used.isEquipped = false;
          if (used == currentEquipment) currentEquipment = null;
          rangeUseHoldConsumed = true;
        }
        else if (!playerInputHandler.RangeAttackTriggered)
        {
          // Reset when input released
          rangeUseHoldConsumed = false;
        }
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

          // Restore main weapon selection (default to Melee if missing)
          if (!string.IsNullOrEmpty(data.mainWeaponType))
          {
            if (Enum.TryParse<MainWeaponType>(data.mainWeaponType, out var parsed))
            {
              SetMainWeapon(parsed);
            }
            else
            {
              SetMainWeapon(MainWeaponType.Melee);
            }
          }
          else
          {
            SetMainWeapon(MainWeaponType.Melee);
          }

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
          // Auto re-equip if this was the last equipped item and we currently have nothing equipped
          if (currentEquipment == null && lastEquippedData == equipment.equipmentData)
          {
            if (EquipmentManager.instance != null)
            {
              EquipmentManager.instance.EquipFromInventory(equipment.equipmentData);
            }
          }
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
          // Only auto-equip the spear if nothing is currently equipped
          if (currentEquipment == null)
          {
            currentEquipment = spearPrefab.GetComponent<Equipment>();
            currentEquipment.isEquipped = true;
            if (currentEquipment != null && currentEquipment.equipmentData != null)
              lastEquippedData = currentEquipment.equipmentData;
          }

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
        // If something is already equipped, try to add to inventory instead of equipping
        if (currentEquipment != null)
        {
          if (equipment.equipmentData != null)
          {
            bool added = Inventory.instance.Add(equipment.equipmentData);
            if (added)
            {
              Destroy(collision.gameObject);
            }
          }
        }
        else
        {
          currentEquipment = equipment;
          currentEquipment.isEquipped = true;
          if (currentEquipment.equipmentData != null) lastEquippedData = currentEquipment.equipmentData;
          Destroy(collision.gameObject);
        }
      }
    }
  }
}