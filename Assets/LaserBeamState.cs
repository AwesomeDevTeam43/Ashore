using UnityEngine;

public class LaserBeamState : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"LaserBeamState: OnStateEnter for '{animator.gameObject.name}'. stateInfo.shortNameHash={stateInfo.shortNameHash}");
        animator.ResetTrigger("Laser");
        // Optionally, you could call SpawnLaserAndHold here if you prefer state-driven spawning:
        // var boss = animator.GetComponent<Boss>(); if (boss != null) boss.SpawnLaserAndHold();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"LaserBeamState: OnStateExit for '{animator.gameObject.name}'.");
        
    }


}
