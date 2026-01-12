using UnityEngine;

public class TutorialEquipItemStep : TutorialStep
{
    [Header("Equip Requirement")]
    public ItemData equipmentItem;

    private Player_Controller player;

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        requiresInventoryFocus = true;
        if (mgr != null && mgr.Overlay != null && string.IsNullOrEmpty(instructionText))
        {
            instructionText = "Open inventory and equip the highlighted item";
        }
        if (mgr != null && mgr.gameObject != null)
        {
            if (mgr != null && mgr.GetComponent<TutorialManager>() != null)
            {
                // find player via manager
                player = mgr.GetComponent<TutorialManager>().player?.GetComponent<Player_Controller>();
            }
        }
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.GetComponent<Player_Controller>();
        }
    }

    public override bool IsComplete()
    {
        if (!HasMinimumFramesPassed()) return false;
        if (player == null) return false;
        var eq = player.CurrentEquipment;
        if (eq == null || eq.equipmentData == null) return false;
        if (equipmentItem == null) return eq != null; // any equip satisfies
        return eq.equipmentData == equipmentItem;
    }
}
