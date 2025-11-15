using UnityEngine;

public class TutorialUnequipItemStep : TutorialStep
{
    private Player_Controller player;

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        requiresInventoryFocus = true;
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.GetComponent<Player_Controller>();
        }
    }

    public override bool IsComplete()
    {
        if (player == null) return false;
        return player.CurrentEquipment == null;
    }
}
