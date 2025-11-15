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
    }

    public override bool IsComplete()
    {
        // Must have equipment if required
        if (player == null) return false;
        var eq = player.CurrentEquipment;
        if (eq == null || eq.equipmentData == null) return false;
        if (requiredEquipment != null && eq.equipmentData != requiredEquipment) return false;

        // Detect the specific 'ranged attack' input this frame
        return WasRangeAttackTriggered(player);
    }

    private bool WasRangeAttackTriggered(Component playerComponent)
    {
        if (playerComponent == null) return false;
        var ih = playerComponent.GetComponent<Player_InputHandler>();
        if (ih == null) return false;
        return ih.RangeAttackTriggered;
    }
}
