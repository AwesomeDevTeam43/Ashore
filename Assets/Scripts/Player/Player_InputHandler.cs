using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class Player_InputHandler : MonoBehaviour
{
    public static bool InventoryOpen => MenuController.InventoryOpen;
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
    [Header("Control Scheme Detection")]
    [SerializeField] private string keyboardMouseSchemeName = "Keyboard&Mouse";
    [SerializeField] private string gamepadSchemeName = "Gamepad";
    [SerializeField] private string defaultSchemeName = "Keyboard&Mouse";

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
    public InputActionAsset ControlsAsset => playerControls;
    public string CurrentControlScheme => string.IsNullOrEmpty(currentControlScheme) ? defaultSchemeName : currentControlScheme;

    // Public event to signal inventory open/close action instantly (avoids polling races)
    public event Action OnInventoryPressed;
    public event Action<string> OnControlSchemeChanged;

    // Expose methods to enable/disable the entire player action map when UI is open
    public void DisablePlayerActions()
    {
        if (playerControls == null) { Debug.LogWarning("[Player_InputHandler] DisablePlayerActions: playerControls is null"); return; }
        var map = playerControls.FindActionMap(actionMapName);
        if (map != null) { map.Disable(); Debug.Log($"[Player_InputHandler] Disabled action map: {actionMapName}"); }
        else { Debug.LogWarning($"[Player_InputHandler] Could not find action map: {actionMapName}"); }
    }

    public void EnablePlayerActions()
    {
        if (playerControls == null) { Debug.LogWarning("[Player_InputHandler] EnablePlayerActions: playerControls is null"); return; }
        var map = playerControls.FindActionMap(actionMapName);
        if (map != null) { map.Enable(); Debug.Log($"[Player_InputHandler] Enabled action map: {actionMapName}"); }
        else { Debug.LogWarning($"[Player_InputHandler] Could not find action map: {actionMapName}"); }
    }

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

    [Header("Aim Deadzone Settings")]
    [Tooltip("Ignore stick magnitude below this value entirely.")]
    [Range(0f, 1f)]
    [SerializeField] private float radialDeadzone = 0.2f;

    [Tooltip("Per-axis deadzone. Components below this are treated as 0 to avoid accidental diagonals.")]
    [Range(0f, 1f)]
    [SerializeField] private float perAxisDeadzone = 0.2f;

    [Tooltip("How much stronger one axis must be (relative) than the other to snap to a cardinal. 0 = always diagonal when both axes active, 1 = never diagonal.")]
    [Range(0f, 1f)]
    [SerializeField] private float axisSnapBias = 0.3f;

    private string currentControlScheme;


    private void OnEnable()
    {
        var map = playerControls.FindActionMap(actionMapName);
        if (map != null) { map.Enable(); Debug.Log($"[Player_InputHandler] OnEnable: Enabled action map {actionMapName}"); }
        else { Debug.LogWarning($"[Player_InputHandler] OnEnable: Could not find action map {actionMapName}"); }
        currentControlScheme = defaultSchemeName;
        UIInputMode.OnSchemeChanged += HandleUISchemeChanged;
        HandleUISchemeChanged(UIInputMode.CurrentScheme);
    }

    private void OnDisable()
    {
        var map = playerControls.FindActionMap(actionMapName);
        if (map != null) { map.Disable(); Debug.Log($"[Player_InputHandler] OnDisable: Disabled action map {actionMapName}"); }
        else { Debug.LogWarning($"[Player_InputHandler] OnDisable: Could not find action map {actionMapName}"); }
        UIInputMode.OnSchemeChanged -= HandleUISchemeChanged;
    }

    private void Awake()
    {
        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);
        if (mapReference == null) Debug.LogError($"[Player_InputHandler] Awake: Could not find action map {actionMapName}");
        else Debug.Log($"[Player_InputHandler] Awake: Found action map {actionMapName}");

        movementAction = mapReference?.FindAction(movement);
        lookAction = mapReference?.FindAction(look);
        jumpAction = mapReference?.FindAction(jump);
        attackAction = mapReference?.FindAction(attack);
        rangeAttackAction = mapReference?.FindAction(rangeAttack);
        interactAction = mapReference?.FindAction(interact);
        inventoryAction = mapReference?.FindAction(inventory);

        Debug.Log($"[Player_InputHandler] Awake: Actions - Move: {movementAction != null}, Look: {lookAction != null}, Jump: {jumpAction != null}, Attack: {attackAction != null}, RangeAttack: {rangeAttackAction != null}, Interact: {interactAction != null}, Inventory: {inventoryAction != null}");
        MakeInputEvents();
    }

    public void Update()
    {
        Handle8Directions();
        if (InventoryActionTriggered)
        {
            Debug.Log("[Player_InputHandler] InventoryActionTriggered detected in Update");
            InventoryActionTriggered = false;
            EnforceInputMap();
        }
    }

    private void EnforceInputMap()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) { Debug.LogWarning("[Player_InputHandler] EnforceInputMap: PlayerInput is null"); return; }
        if (InventoryOpen)
        {
            Debug.Log("[Player_InputHandler] EnforceInputMap: InventoryOpen, switching to UI map");
            try { playerInput.actions.FindActionMap("Player").Disable(); Debug.Log("[Player_InputHandler] Disabled Player map"); } catch (Exception ex) { Debug.LogWarning($"[Player_InputHandler] Failed to disable Player map: {ex.Message}"); }
            try { playerInput.actions.FindActionMap("UI").Enable(); Debug.Log("[Player_InputHandler] Enabled UI map"); } catch (Exception ex) { Debug.LogWarning($"[Player_InputHandler] Failed to enable UI map: {ex.Message}"); }
            try { playerInput.SwitchCurrentActionMap("UI"); Debug.Log("[Player_InputHandler] Switched to UI map"); } catch (Exception ex) { Debug.LogWarning($"[Player_InputHandler] Failed to switch to UI map: {ex.Message}"); }
        }
        else
        {
            Debug.Log("[Player_InputHandler] EnforceInputMap: Inventory closed, switching to Player map");
            try { playerInput.actions.FindActionMap("UI").Disable(); Debug.Log("[Player_InputHandler] Disabled UI map"); } catch (Exception ex) { Debug.LogWarning($"[Player_InputHandler] Failed to disable UI map: {ex.Message}"); }
            try { playerInput.actions.FindActionMap("Player").Enable(); Debug.Log("[Player_InputHandler] Enabled Player map"); } catch (Exception ex) { Debug.LogWarning($"[Player_InputHandler] Failed to enable Player map: {ex.Message}"); }
            try { playerInput.SwitchCurrentActionMap("Player"); Debug.Log("[Player_InputHandler] Switched to Player map"); } catch (Exception ex) { Debug.LogWarning($"[Player_InputHandler] Failed to switch to Player map: {ex.Message}"); }
        }
    }

    public void LateUpdate()
    {
        if (InteractActionTriggered)
        {
            InteractActionTriggered = false;
        }
    }

    private void MakeInputEvents()
    {
        movementAction.performed += inputInfo => { MovementInput = inputInfo.ReadValue<Vector2>(); Debug.Log($"[Player_InputHandler] Movement performed: {MovementInput}"); UpdateControlScheme(inputInfo); };
        movementAction.canceled += inputInfo => { MovementInput = Vector2.zero; Debug.Log("[Player_InputHandler] Movement canceled"); };

        lookAction.performed += inputInfo => { LookInput = inputInfo.ReadValue<Vector2>(); Debug.Log($"[Player_InputHandler] Look performed: {LookInput}"); UpdateControlScheme(inputInfo); };
        lookAction.canceled += inputInfo => { LookInput = Vector2.zero; Debug.Log("[Player_InputHandler] Look canceled"); };

        jumpAction.performed += inputInfo => { JumpTriggered = true; Debug.Log("[Player_InputHandler] Jump performed"); UpdateControlScheme(inputInfo); };
        jumpAction.canceled += inputInfo => { JumpTriggered = false; Debug.Log("[Player_InputHandler] Jump canceled"); };

        attackAction.performed += inputInfo => { AttackTriggered = true; Debug.Log("[Player_InputHandler] Attack performed"); UpdateControlScheme(inputInfo); };
        attackAction.canceled += inputInfo => { AttackTriggered = false; Debug.Log("[Player_InputHandler] Attack canceled"); };

        rangeAttackAction.performed += inputInfo => { RangeAttackTriggered = true; Debug.Log("[Player_InputHandler] RangeAttack performed"); UpdateControlScheme(inputInfo); };
        rangeAttackAction.canceled += inputInfo => { RangeAttackTriggered = false; Debug.Log("[Player_InputHandler] RangeAttack canceled"); };

        interactAction.performed += inputInfo => { InteractActionTriggered = true; Debug.Log("[Player_InputHandler] Interact performed"); UpdateControlScheme(inputInfo); };
        interactAction.canceled += inputInfo => { InteractActionTriggered = false; Debug.Log("[Player_InputHandler] Interact canceled"); };

        inventoryAction.performed += inputInfo => { InventoryActionTriggered = true; Debug.Log("[Player_InputHandler] Inventory performed"); OnInventoryPressed?.Invoke(); UpdateControlScheme(inputInfo); };
        inventoryAction.canceled += inputInfo => { InventoryActionTriggered = false; Debug.Log("[Player_InputHandler] Inventory canceled"); };
    }

    private void Handle8Directions()
    {
        Vector2 dir8;
        int dirIndex;
        Get8DirectionSmart(MovementInput, out dir8, out dirIndex);

        if (dirIndex != lastDirIndex)
        {
            lastDirIndex = dirIndex;
            On8DirectionChanged?.Invoke(dir8);
        }
    }

    /// <summary>
    /// Applies radial and per-axis deadzones plus a cardinal/diagonal bias to produce a stable 8-way direction.
    /// Returns a vector guaranteed to be one of directions8 (or zero) and its index (-1 if zero).
    /// </summary>
    private void Get8DirectionSmart(Vector2 input, out Vector2 dir8, out int dirIndex)
    {
        dir8 = Vector2.zero;
        dirIndex = -1;

        // Radial deadzone: ignore very small stick input
        float mag = input.magnitude;
        if (mag < radialDeadzone)
        {
            return;
        }

        // Normalize for comparison but keep original signs
        Vector2 v = input.normalized;
        float ax = Mathf.Abs(v.x);
        float ay = Mathf.Abs(v.y);

        // Per-axis deadzone: zero-out tiny components to avoid accidental diagonals
        float sx = v.x;
        float sy = v.y;
        if (ax < perAxisDeadzone) sx = 0f;
        if (ay < perAxisDeadzone) sy = 0f;

        // If both got zeroed, fall back to dominant axis from original
        if (Mathf.Approximately(sx, 0f) && Mathf.Approximately(sy, 0f))
        {
            if (ax >= ay)
                sx = Mathf.Sign(v.x);
            else
                sy = Mathf.Sign(v.y);
        }

        // Decide cardinal vs diagonal using relative bias
        ax = Mathf.Abs(sx);
        ay = Mathf.Abs(sy);
        if (ax > 0f && ay > 0f)
        {
            // Both axes present: choose diagonal only if axes are similar within bias window
            float maxA = Mathf.Max(ax, ay);
            float minA = Mathf.Min(ax, ay);
            bool similar = minA >= maxA * (1f - axisSnapBias);
            if (similar)
            {
                // Diagonal
                int diagIndex;
                if (sx > 0 && sy > 0) diagIndex = 1; // UpRight
                else if (sx < 0 && sy > 0) diagIndex = 3; // UpLeft
                else if (sx < 0 && sy < 0) diagIndex = 5; // DownLeft
                else diagIndex = 7; // DownRight
                dir8 = directions8[diagIndex];
                dirIndex = diagIndex;
                return;
            }
            else
            {
                // Snap to the dominant axis (cardinal)
                if (ax > ay)
                {
                    dir8 = sx > 0 ? Vector2.right : Vector2.left;
                    dirIndex = sx > 0 ? 0 : 4;
                    return;
                }
                else
                {
                    dir8 = sy > 0 ? Vector2.up : Vector2.down;
                    dirIndex = sy > 0 ? 2 : 6;
                    return;
                }
            }
        }
        else if (ax > 0f || ay > 0f)
        {
            // Pure cardinal (one axis)
            if (ax >= ay)
            {
                dir8 = sx >= 0 ? Vector2.right : Vector2.left;
                dirIndex = sx >= 0 ? 0 : 4;
                return;
            }
            else
            {
                dir8 = sy >= 0 ? Vector2.up : Vector2.down;
                dirIndex = sy >= 0 ? 2 : 6;
                return;
            }
        }

        // Fallback by angle (shouldn't be hit)
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        int index = Mathf.RoundToInt(angle / 45f) % 8;
        dir8 = directions8[index];
        dirIndex = index;
    }

    private void UpdateControlScheme(InputAction.CallbackContext ctx)
    {
        var device = TryGetDeviceFromContext(ctx);
        if (device == null)
        {
            return;
        }

        string newScheme = DetermineSchemeForDevice(device);
        ApplyControlScheme(newScheme);
    }

    private InputDevice TryGetDeviceFromContext(InputAction.CallbackContext ctx)
    {
        if (ctx.action == null)
        {
            return null;
        }
        InputControl control = null;
        try
        {
            control = ctx.control;
        }
        catch (IndexOutOfRangeException)
        {
            control = null;
        }

        if (control != null)
        {
            return control.device;
        }

        if (ctx.action.activeControl != null)
        {
            return ctx.action.activeControl.device;
        }

        return null;
    }

    private void HandleUISchemeChanged(UIInputMode.Scheme scheme)
    {
        string targetScheme = scheme == UIInputMode.Scheme.Gamepad ? gamepadSchemeName : keyboardMouseSchemeName;
        ApplyControlScheme(targetScheme);
    }

    private void ApplyControlScheme(string newScheme)
    {
        if (string.IsNullOrEmpty(newScheme))
        {
            newScheme = defaultSchemeName;
        }

        if (string.Equals(newScheme, currentControlScheme, StringComparison.Ordinal))
        {
            return;
        }

        Debug.Log($"[Player_InputHandler] Control scheme changed: {currentControlScheme} -> {newScheme}");
        currentControlScheme = newScheme;
        OnControlSchemeChanged?.Invoke(currentControlScheme);
    }

    private string DetermineSchemeForDevice(InputDevice device)
    {
        if (device == null) return defaultSchemeName;
        if (playerControls != null)
        {
            foreach (var scheme in playerControls.controlSchemes)
            {
                if (scheme.SupportsDevice(device))
                {
                    return scheme.name;
                }
            }
        }

        if (device is Gamepad)
        {
            return gamepadSchemeName;
        }

        if (device is Keyboard || device is Mouse)
        {
            return keyboardMouseSchemeName;
        }

        return defaultSchemeName;
    }

    /// <summary>
    /// Returns a 4-way (cardinal only) direction with the same deadzone handling
    /// used for aiming. Outputs Vector2.zero if under radial deadzone.
    /// </summary>
    public Vector2 Get4Direction(Vector2 input)
    {
        float mag = input.magnitude;
        if (mag < radialDeadzone)
            return Vector2.zero;

        Vector2 v = input.normalized;
        float ax = Mathf.Abs(v.x);
        float ay = Mathf.Abs(v.y);

        float sx = v.x;
        float sy = v.y;
        if (ax < perAxisDeadzone) sx = 0f;
        if (ay < perAxisDeadzone) sy = 0f;

        // If both got zeroed, fall back to dominant axis from original
        if (Mathf.Approximately(sx, 0f) && Mathf.Approximately(sy, 0f))
        {
            if (ax >= ay)
                sx = Mathf.Sign(v.x);
            else
                sy = Mathf.Sign(v.y);
        }

        ax = Mathf.Abs(sx);
        ay = Mathf.Abs(sy);
        // Prefer vertical on tie to avoid accidental right/left when aiming at perfect diagonals
        if (ay >= ax)
        {
            return sy >= 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            return sx >= 0 ? Vector2.right : Vector2.left;
        }
    }
}