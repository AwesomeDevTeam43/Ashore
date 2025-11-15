using UnityEngine;

public class TutorialSwitchWeaponStep : TutorialStep
{
    public Player_Controller.MainWeaponType requiredType = Player_Controller.MainWeaponType.Melee;

    private Player_Controller player;

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.GetComponent<Player_Controller>();
        }
    }

    public override bool IsComplete()
    {
        if (player == null) return false;
        return player.CurrentMainWeapon == requiredType;
    }
}
