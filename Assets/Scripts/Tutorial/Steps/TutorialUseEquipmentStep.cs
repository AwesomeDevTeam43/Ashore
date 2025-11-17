using UnityEngine;

public class TutorialUseEquipmentStep : TutorialStep
{
    [Header("Equipment Requirement")]
    [Tooltip("If set, require this specific equipment to be equipped before use is accepted. If null, any equipment is allowed.")]
    public ItemData requiredEquipment;

    [Header("Input Detection")]
    [Tooltip("This step completes only when the player's RangeAttack input is triggered via Player_InputHandler.")]
    public bool onlyUseRangeAttack = true;

    private Player_Controller player;
    private bool usedEventReceived = false;

    private void Reset()
    {
        freezePlayerMovement = false;
        dimScreen = true; // overlay dim disabled globally
        requiresInventoryFocus = false;
    }

    public override void Begin(TutorialManager mgr)
    {
        // Force no-freeze behavior for this step regardless of inspector overrides
        this.freezeGameSeconds = 0f;
        this.freezeUntilPlayerInput = false;
        this.freezePlayerMovement = false;
        base.Begin(mgr);
        if (mgr != null && mgr.player != null)
            player = mgr.player.GetComponent<Player_Controller>();
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.GetComponent<Player_Controller>();
        }
        // Subscribe to actual equipment use events
        Player_Controller.OnEquipmentUsed += OnEquipmentUsed;
    }

    public override bool IsComplete()
    {
        if (player == null) return false;

        // If we already received the validated use event, finish regardless of current equipment (it may have been consumed/cleared).
        if (usedEventReceived) return true;

        // Fallback path: require currently equipped item to match requirement, then detect input trigger edge.
        var eq = player.CurrentEquipment;
        if (eq == null || eq.equipmentData == null) return false;
        if (requiredEquipment != null && eq.equipmentData != requiredEquipment) return false;

        if (onlyUseRangeAttack)
            return WasRangeAttackTriggered(player);
        return false;
    }

    private bool WasRangeAttackTriggered(Component playerComponent)
    {
        if (playerComponent == null) return false;
        var ih = playerComponent.GetComponent<Player_InputHandler>();
        if (ih == null) return false;
        return ih.RangeAttackTriggered;
    }

    private void OnEquipmentUsed(Equipment eq)
    {
        if (eq == null || eq.equipmentData == null) return;
        if (requiredEquipment != null && eq.equipmentData != requiredEquipment) return;
        usedEventReceived = true;
    }

    public override void End()
    {
        Player_Controller.OnEquipmentUsed -= OnEquipmentUsed;
        base.End();
    }
}
