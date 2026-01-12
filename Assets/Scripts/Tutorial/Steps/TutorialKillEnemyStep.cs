using UnityEngine;

public class TutorialKillEnemyStep : TutorialStep
{
    [Header("Target")]
    [Tooltip("Enemy to kill for this step. If it has a HealthSystem, we use that; otherwise we consider it complete when destroyed.")]
    public GameObject enemy;

    private HealthSystem hs;

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        hs = enemy != null ? enemy.GetComponent<HealthSystem>() : null;
    }

    public override bool IsComplete()
    {
        if (!HasMinimumFramesPassed()) return false;
        if (enemy == null) return true; // destroyed
        if (hs != null) return hs.CurrentHealth <= 0;
        return enemy == null;
    }
}
