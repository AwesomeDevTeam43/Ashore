using UnityEngine;

/// <summary>
/// Simple StateMachineBehaviour that notifies BossAIBrain when animations complete.
/// Attach this to ALL boss animator states (Attack, Combo, Idle, Laser, etc.)
/// 
/// This tells the AI brain when animations finish so it can make new decisions.
/// </summary>
public class BossAIStateNotifier : StateMachineBehaviour
{
    private BossAIBrain aiBrain;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get reference to AI brain if not already cached
        if (aiBrain == null)
        {
            aiBrain = animator.GetComponent<BossAIBrain>();
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Notify AI brain that this state/action has completed
        if (aiBrain != null)
        {
            aiBrain.OnActionComplete();
        }
    }
}
