using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class Player_Controller : MonoBehaviour
{
  public static event System.Action<Equipment> OnEquipmentUsed;
  public static event Action OnPlayerLoad;
  private XP_System xP_System;
  private Player_Health playerHealth;
  private HealthSystem healthSystem;
  private Rigidbody2D rb2d;
  private Rigidbody rb3d;
  private Player_Movement playerMovement;

  [Header("Animation")]
  [SerializeField] private Animator animator;
  
  [Tooltip("Float parameter to drive movement speed (optional). Leave empty to skip.")]
  [SerializeField] private string speedParam = "Speed";
  [Tooltip("Bool parameter to toggle running state (optional). Leave empty to skip.")]
  [SerializeField] private string isRunningParam = "IsRunning";
  [Tooltip("Bool parameter to indicate jumping/airborne state (optional).")]
  [SerializeField] private string isJumpingParam = "isJumping";
  [Tooltip("Float parameter name to pass vertical velocity to the Animator (optional).")]
  [SerializeField] private string yVelParam = "YVel";
  [Tooltip("Trigger parameter name to play landing animation (optional).")]
  [SerializeField] private string landTrigger = "Land";
  [Tooltip("Trigger parameter name to play jump start animation (optional).")]
  [SerializeField] private string jumpStartTrigger = "JumpStart";
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

  [Header("Current Stats (Runtime)")]
  public int MaxHealth { get; private set; }
  public int AttackPower { get; private set; }
  public float MoveSpeed { get; private set; }
  public float JumpForce { get; private set; }
  public float CriticalChance { get; private set; }

  [Header("Temporary Inventory")]
  [SerializeField] private Equipment currentEquipment;
  [SerializeField] private GameObject spearPrefab;
  [SerializeField] private EquipmentData lastEquippedData;
  
  public enum MainWeaponType { Melee, Ranged }
  [Header("Combat")]
  [SerializeField] private MainWeaponType currentMainWeapon = MainWeaponType.Melee;
  public MainWeaponType CurrentMainWeapon => currentMainWeapon;
  public void SetMainWeapon(MainWeaponType type) { currentMainWeapon = type; }

  public int LVL1XpAmount => playerStats != null ? playerStats.Level1XpAmount : 0;
  public float XpGrowthMultiplier => playerStats != null ? Mathf.Max(1f, playerStats.XpGrowthMultiplier) : 1f;
  public Equipment CurrentEquipment => currentEquipment;
  public EquipmentData LastEquippedData => lastEquippedData;
  public int CurrentLevel => xP_System != null ? xP_System.CurrentLevel : 1;

  public void SetCurrentEquipment(Equipment eq)
  {
    currentEquipment = eq;
    if (currentEquipment != null)
    {
      currentEquipment.isEquipped = true;
      if (currentEquipment.equipmentData != null)
      {
        lastEquippedData = currentEquipment.equipmentData;
      }
    }
  }

  public void ResetProgressToFreshStart()
  {
    if (currentEquipment != null)
    {
      Destroy(currentEquipment.gameObject);
      currentEquipment = null;
    }
    lastEquippedData = null;
    rangeUseHoldConsumed = false;

    if (xP_System != null)
    {
      xP_System.Initialize(LVL1XpAmount, XpGrowthMultiplier);
    }

    UpdateStatsFromLevel();

    if (playerHealth != null && healthSystem != null)
    {
      playerHealth.UpdateHealthStat(1);
      if (playerStats != null)
      {
        healthSystem.SetHealth(playerStats.GetHealth(1));
      }
    }
  }

  private void OnEnable()
  {
    if (playerStats != null)
    {
      if (!GameFlowState.IsLoading)
      {
        UpdateStatsFromLevel();
      }
    }
  }

  private void Awake()
  {
    xP_System = GetComponent<XP_System>();
    playerHealth = GetComponent<Player_Health>();
    healthSystem = GetComponent<HealthSystem>();
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
      xP_System.OnLevelUp += OnLevelUp;
    }
  }

  private void Start()
  {
    if (GameFlowState.LoadGameOnStart)
    {
      GameFlowState.IsLoading = true;
      LoadGame();
      GameFlowState.LoadGameOnStart = false;
      GameFlowState.IsLoading = false;
    }
    else
    {
      xP_System.Initialize(LVL1XpAmount, XpGrowthMultiplier);
    }
    UpdateStatsFromLevel();
  }

  private void UpdateStatsFromLevel()
  {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not assigned!");
            return;
        }

        int level = CurrentLevel;
        MaxHealth = playerStats.GetHealth(level);
        AttackPower = playerStats.GetAttackPower(level);
        MoveSpeed = playerStats.GetMoveSpeed(level);
        JumpForce = playerStats.GetJumpForce(level);
        CriticalChance = playerStats.GetCriticalChance(level);

        if (healthSystem != null)
        {
            healthSystem.MaxHealth = MaxHealth;
        }

        Debug.Log($"Stats Updated - Level: {level}, HP: {MaxHealth}, ATK: {AttackPower}, SPD: {MoveSpeed}, JUMP: {JumpForce}, CRIT: {CriticalChance}%");
    }

    // Ranged weapon critical chance: base 3% + 0.5% per level (capped at 100%)
    public float GetRangedCriticalChance()
    {
        if (playerStats == null) return 3f;
        int level = CurrentLevel;
        float chance = 3f + 0.5f * (level - 1);
        return Mathf.Min(chance, 100f);
    }

  public void OnLevelUp(int newLevel)
  {
    UpdateStatsFromLevel();
    Debug.Log($"LEVEL UP! Now level {newLevel}");
  }

  private void OnDisable()
  {
    if (xP_System != null)
    {
      xP_System.OnLevelUp -= OnLevelUp;
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
      bool hasMovementInput = false;
      if (playerInputHandler != null)
      {
        Vector2 mv = playerInputHandler.MovementInput;
        hasMovementInput = mv.sqrMagnitude > (inputDeadzone * inputDeadzone);
      }
      else
      {
        float inputX = 0f;
        float inputY = 0f;
        try { inputX = Input.GetAxisRaw("Horizontal"); inputY = Input.GetAxisRaw("Vertical"); } catch { }
        hasMovementInput = Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f;
      }
      animator.SetBool(isRunningParam, hasMovementInput);
    }

    if (hasIsJumpingParam || hasYVelParam || hasLandTrigger || hasJumpStartTrigger)
    {
      bool grounded = false;
      if (playerMovement != null)
        grounded = playerMovement.IsGrounded();
      else if (rb2d != null)
        grounded = Mathf.Abs(rb2d.linearVelocity.y) < 0.01f;

      float vertVel = 0f;
      if (rb2d != null) vertVel = rb2d.linearVelocity.y;

      if (hasYVelParam)
        animator.SetFloat(yVelParam, vertVel);

      if (landingCooldownTimer > 0f)
        landingCooldownTimer -= Time.deltaTime;

      if (prevGrounded && !grounded && vertVel > 0.1f)
      {
        if (hasJumpStartTrigger)
        {
          animator.SetTrigger(jumpStartTrigger);
          hasJumped = true;
        }
        landingTriggered = false;
      }

      if (!grounded)
      {
        if (hasIsJumpingParam)
        {
          animator.SetBool(isJumpingParam, true);
        }
      }
      else
      {
        if (hasIsJumpingParam)
        {
          animator.SetBool(isJumpingParam, false);
        }
        landingTriggered = false;
        if (hasJumpStartTrigger)
        {
          animator.ResetTrigger(jumpStartTrigger);
          if (hasJumped && grounded)
            hasJumped = false;
        }
      }

      prevGrounded = grounded;
    }
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (!hasLandTrigger) return;

    int mask = (playerMovement != null) ? playerMovement.CombinedGroundLayers : (1 << collision.gameObject.layer);
    bool isGroundLayer = (((1 << collision.gameObject.layer) & mask) != 0);
    float vertVel = (rb2d != null) ? rb2d.linearVelocity.y : 0f;
    
    if (isGroundLayer && vertVel <= 0f && !landingTriggered && landingCooldownTimer <= 0f)
    {
      if (hasJumped)
      {
        animator.SetTrigger(landTrigger);
        hasJumped = false;
      }
      landingTriggered = true;
      landingCooldownTimer = landingCooldown;
    }

    if (hasIsJumpingParam)
    {
      animator.SetBool(isJumpingParam, false);
    }
  }

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
          rangeUseHoldConsumed = false;
        }
      }
    }
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
      if (healthSystem != null)
      {
        healthSystem.MaxHealth = data.maxHealth;
        healthSystem.SetHealth(data.currentHealth);
      }
      
      xP_System.Initialize(data.level, data.currentXp, data.maxXp, LVL1XpAmount, XpGrowthMultiplier);
      UpdateStatsFromLevel();

      transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);

      Inventory.instance.Clear();
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
      }

      currentEquipment = null;
      string equipKey = !string.IsNullOrEmpty(data.equippedResourceName) ? data.equippedResourceName : data.equippedItemName;
      if (!string.IsNullOrEmpty(equipKey))
      {
        EquipmentData eqData = Resources.Load<EquipmentData>("Items/" + equipKey);
        if (eqData != null)
        {
          if (EquipmentManager.instance != null)
          {
            EquipmentManager.instance.EquipFromInventory(eqData);
          }
        }
      }

      SaveSystem.RestoreWorldState(data);

      if (!string.IsNullOrEmpty(data.mainWeaponType))
      {
        if (Enum.TryParse<MainWeaponType>(data.mainWeaponType, out var parsed))
        {
          SetMainWeapon(parsed);
        }
      }

      OnPlayerLoad?.Invoke();
    }
  }

  public void ReturnToLastPoint()
  {
    transform.position = ReturnPointManager.GetReturnPoint();

    if (rb2d != null)
    {
      rb2d.linearVelocity = Vector2.zero;
    }
  }

  private void OnTriggerStay2D(Collider2D collision)
  {
    if (LayerMask.LayerToName(collision.gameObject.layer) == "RestPoint")
    {
      if (Input.GetKeyDown(KeyCode.B))
      {
        if (healthSystem != null)
        {
          healthSystem.SetHealth(healthSystem.MaxHealth);
        }
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
        if (healthSystem != null)
        {
          healthSystem.SetHealth(healthSystem.MaxHealth);
        }
        SaveGame();
      }
    }

    Equipment equipment = collision.GetComponent<Equipment>();
    if (equipment != null && !equipment.isEquipped)
    {
      Spear thrownSpearCheck = equipment as Spear;
      if (thrownSpearCheck != null && !thrownSpearCheck.CanBePickedUp())
      {
        return;
      }
      
      if (equipment.equipmentData != null)
      {
        bool added = Inventory.instance.Add(equipment.equipmentData);
        if (added)
        {
          Destroy(collision.gameObject);
          if (currentEquipment == null && lastEquippedData == equipment.equipmentData)
          {
            if (EquipmentManager.instance != null)
            {
              EquipmentManager.instance.EquipFromInventory(equipment.equipmentData);
            }
          }
          return;
        }
      }
      
      Spear spear = equipment as Spear;
      if (spear != null)
      {
        if (spear.CanBePickedUp())
        {
          if (spearPrefab == null)
          {
            spearPrefab = equipment.gameObject;
          }
          
          if (currentEquipment == null)
          {
            currentEquipment = spearPrefab.GetComponent<Equipment>();
            currentEquipment.isEquipped = true;
            if (currentEquipment != null && currentEquipment.equipmentData != null)
              lastEquippedData = currentEquipment.equipmentData;
          }

          Destroy(collision.gameObject);
        }
      }
      else if (equipment.hasLanded)
      {
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
          if (currentEquipment.equipmentData != null) 
            lastEquippedData = currentEquipment.equipmentData;
          Destroy(collision.gameObject);
        }
      }
    }
  }
}