using UnityEngine;

public class Combo : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger("yes");

        // When a Combo finishes, re-enable the boss's ability to use the Laser
        Boss boss = animator.GetComponent<Boss>();
        if (boss != null)
        {
            boss.canUseLaser = true;

            // Also reset any Laser components under the boss so they can damage again
            Laser[] lasers = boss.GetComponentsInChildren<Laser>(true);
            foreach (var l in lasers)
            {
                if (l != null)
                {
                    l.ResetDamageFlag();
                }
            }
        }
    }


}
