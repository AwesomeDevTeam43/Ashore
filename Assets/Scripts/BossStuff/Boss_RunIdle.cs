using Unity.VisualScripting;
using UnityEngine;

public class Boss_RunIdle : StateMachineBehaviour
{
    Transform player;
    Rigidbody2D rb;
    [SerializeField] float speed = 3f;
    [SerializeField] float attackRange = 2f;
    Boss boss;
    // Track whether we've already triggered a special action (Combo/Laser) during this state entry
    private bool specialTriggeredThisEntry = false;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = animator.GetComponent<Rigidbody2D>();
        boss = animator.GetComponent<Boss>();

        // reset per-entry flag
        specialTriggeredThisEntry = false;

        // Random chance to trigger special actions when entering Run/Idle state
        float r = Random.value; // 0..1
        // ~33% for Combo, ~33% for Laser, rest do nothing
        if (r < 0.33f)
        {
            Debug.Log($"Boss_RunIdle: rolled {r:F2} -> Combo trigger");
            animator.ResetTrigger("Combo");
            animator.SetTrigger("Combo");
            specialTriggeredThisEntry = true;
        }
        else if (r < 0.66f)
        {
            Debug.Log($"Boss_RunIdle: rolled {r:F2} -> Laser trigger");
            // Trigger Laser if allowed OR if the boss has been idle (hasn't attacked player) long enough
            if (boss != null && (boss.canUseLaser || boss.HasBeenIdleLongEnough()))
            {
                animator.SetTrigger("Laser");
                // prevent subsequent lasers until a Combo resets it
                boss.canUseLaser = false;
                // record laser usage so idle allowance is refreshed
                boss.NotifyLaserUsed();
                specialTriggeredThisEntry = true;
            }
            else
            {
                Debug.Log("Boss_RunIdle: Laser skipped because canUseLaser is false and not idle long enough");
            }
        }
        else
        {
            // normal behavior
            animator.ResetTrigger("Combo"); 
            animator.SetTrigger("yes");
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        boss.LookAtPlayer(player);

        Vector2 target = new Vector2(player.position.x, rb.position.y);
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, 3 * Time.fixedDeltaTime);
        

        rb.MovePosition(newPos);

        if (Vector2.Distance(player.position, rb.position) <= attackRange)
        {
            animator.SetTrigger("Attack");
        }

        // If we haven't already triggered a special action this state entry, allow the idle timer
        // to trigger a Laser while still in Run/Idle.
        if (!specialTriggeredThisEntry && boss != null && boss.HasBeenIdleLongEnough())
        {
            Debug.Log("Boss_RunIdle: idle threshold reached during state -> Laser trigger");
            animator.SetTrigger("Laser");
            boss.canUseLaser = false;
            boss.NotifyLaserUsed();
            specialTriggeredThisEntry = true;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        animator.ResetTrigger("Attack");
    }
    


}
