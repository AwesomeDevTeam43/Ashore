using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class Player_InputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Name References")]
    [SerializeField] private string movement = "Move";
    [SerializeField] private string look = "Look";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string attack = "Attack";
    [SerializeField] private string rangeAttack = "RangeAttack";
    [SerializeField] private string interact = "Interact";
    [SerializeField] private string inventory = "Inventory";

    private InputAction movementAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction rangeAttackAction;
    private InputAction interactAction;
    private InputAction inventoryAction;

    public Vector2 MovementInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool AttackTriggered { get; private set; }
    public bool RangeAttackTriggered { get; private set; }
    public bool InteractActionTriggered { get; private set; }
    public bool InventoryActionTriggered { get; private set; }

    [Header("8-Direction Inputs")]
    private Vector2[] directions8 = new Vector2[]
    {
        Vector2.right,
        new Vector2(1,1).normalized,
        Vector2.up,
        new Vector2(-1,1).normalized,
        Vector2.left,
        new Vector2(-1,-1).normalized,
        Vector2.down,
        new Vector2(1,-1).normalized
    };
    public event Action<Vector2> On8DirectionChanged;
    private int lastDirIndex = -1;


    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }

    private void Awake()
    {
        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);

        movementAction = mapReference.FindAction(movement);
        lookAction = mapReference.FindAction(look);
        jumpAction = mapReference.FindAction(jump);
        attackAction = mapReference.FindAction(attack);
        rangeAttackAction = mapReference.FindAction(rangeAttack);
        interactAction = mapReference.FindAction(interact);
        inventoryAction = mapReference.FindAction(inventory);

        MakeInputEvents();
    }

    public void Update()
    {
        Handle8Directions();
        if (InventoryActionTriggered)
        {
            InventoryActionTriggered = false;
        }
        if (InteractActionTriggered)
        {
            InteractActionTriggered = false;
        }
    }

    private void MakeInputEvents()
    {
        movementAction.performed += inputInfo => MovementInput = inputInfo.ReadValue<Vector2>();
        movementAction.canceled += inputInfo => MovementInput = Vector2.zero;

        lookAction.performed += inputInfo => LookInput = inputInfo.ReadValue<Vector2>();
        lookAction.canceled += inputInfo => LookInput = Vector2.zero;

        jumpAction.performed += inputInfo => JumpTriggered = true;
        jumpAction.canceled += inputInfo => JumpTriggered = false;

        attackAction.performed += inputInfo => AttackTriggered = true;
        attackAction.canceled += inputInfo => AttackTriggered = false;

        rangeAttackAction.performed += inputInfo => RangeAttackTriggered = true;
        rangeAttackAction.canceled += inputInfo => RangeAttackTriggered = false;

        interactAction.performed += inputInfo => InteractActionTriggered = true;
        interactAction.canceled += inputInfo => InteractActionTriggered = false;

        inventoryAction.performed += inputInfo => InventoryActionTriggered = true;
        inventoryAction.canceled += inputInfo => InventoryActionTriggered = false;
    }

    private void Handle8Directions()
    {
        Vector2 dir8 = Get8Direction(MovementInput);
        int dirIndex = Array.IndexOf(directions8, dir8);

        if (dirIndex != lastDirIndex)
        {
            lastDirIndex = dirIndex;
            On8DirectionChanged?.Invoke(dir8);
        }
    }

    private Vector2 Get8Direction(Vector2 input)
    {
        if (input == Vector2.zero)
            return Vector2.zero;

        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        int index = Mathf.RoundToInt(angle / 45f) % 8;
        return directions8[index];
    }
}

