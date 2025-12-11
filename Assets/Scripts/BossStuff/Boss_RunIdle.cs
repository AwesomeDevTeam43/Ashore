using Unity.VisualScripting;
using UnityEngine;

public class Boss_RunIdle : StateMachineBehaviour
{
    Transform player;
    Rigidbody2D rb;
    [SerializeField] float attackRange = 2f;
    Boss boss;
    // Track whether we've already triggered a special action (Combo/Laser) during this state entry
    private bool specialTriggeredThisEntry = false;
    
    [Header("AI Configuration")]
    [Tooltip("Enable AI-based decision making instead of random behavior")]
    [SerializeField] private bool useAI = false;
    
    private BossAIController aiController;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = animator.GetComponent<Rigidbody2D>();
        boss = animator.GetComponent<Boss>();
        
        // Get AI controller if using AI mode
        if (useAI && aiController == null)
        {
            aiController = animator.GetComponent<BossAIController>();
        }

        // reset per-entry flag
        specialTriggeredThisEntry = false;

        // Use AI decision if enabled and available, otherwise use random behavior
        if (useAI && aiController != null && aiController.IsReady())
        {
            // Get AI decision
            string aiDecision = aiController.GetAIDecision();
            
            if (aiDecision != null)
            {
                Debug.Log($"Boss_RunIdle: AI decision -> {aiDecision}");
                ExecuteAction(animator, aiDecision);
            }
            else
            {
                Debug.LogWarning("Boss_RunIdle: AI decision failed, falling back to random behavior");
                ExecuteRandomAction(animator);
            }
        }
        else
        {
            // Use original random behavior
            ExecuteRandomAction(animator);
        }
    }
    
    /// <summary>
    /// Executes the original random action selection
    /// </summary>
    private void ExecuteRandomAction(Animator animator)
    {
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
    
    /// <summary>
    /// Executes an action based on AI decision
    /// </summary>
    private void ExecuteAction(Animator animator, string action)
    {
        switch (action)
        {
            case "Combo":
                animator.ResetTrigger("Combo");
                animator.SetTrigger("Combo");
                specialTriggeredThisEntry = true;
                break;
                
            case "Laser":
                // Trigger Laser if allowed OR if the boss has been idle long enough
                if (boss != null && (boss.canUseLaser || boss.HasBeenIdleLongEnough()))
                {
                    animator.SetTrigger("Laser");
                    boss.canUseLaser = false;
                    boss.NotifyLaserUsed();
                    specialTriggeredThisEntry = true;
                }
                else
                {
                    Debug.Log("Boss_RunIdle: AI requested Laser but it's not available");
                    // Fall back to chase behavior
                    animator.ResetTrigger("Combo");
                    animator.SetTrigger("yes");
                }
                break;
                
            case "Chase":
            case "Idle":
            case "Attack":
                // These are handled by the OnStateUpdate (chase/attack) or just stay in idle
                animator.ResetTrigger("Combo");
                animator.SetTrigger("yes");
                break;
                
            default:
                Debug.LogWarning($"Boss_RunIdle: Unknown AI action '{action}', using default behavior");
                animator.ResetTrigger("Combo");
                animator.SetTrigger("yes");
                break;
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
