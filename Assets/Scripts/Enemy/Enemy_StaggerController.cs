using UnityEngine;

/// <summary>
/// Handles temporary stagger (flinch) on enemies. When staggered, horizontal movement and AI logic
/// should be effectively neutralized by zeroing velocity each frame. Attach to any enemy root.
/// Trigger via ApplyStagger(duration). Critical hits can call ApplyStagger using a configured
/// default duration.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_StaggerController : MonoBehaviour
{
    [Header("Stagger Settings")] 
    [Tooltip("Default stagger duration applied on critical hit if no override is passed.")] 
    [SerializeField] private float criticalStaggerDuration = 0.6f;
    [Tooltip("If true, vertical velocity is also frozen while staggered.")] 
    [SerializeField] private bool freezeVerticalVelocity = false;
    [Tooltip("Optional animator to temporarily pause by setting speed to 0 while staggered.")] 
    [SerializeField] private Animator animator;
    [Tooltip("Animator speed while staggered (0 pauses animation).")] 
    [SerializeField] private float staggerAnimatorSpeed = 0f;
    [Tooltip("Animator speed restored after stagger.")] 
    [SerializeField] private float normalAnimatorSpeed = 1f;

    [Header("Debug")] 
    [SerializeField] private bool debugLogs = true;

    private float staggerTimer;
    private Rigidbody2D rb;
    private float originalAnimatorSpeed;

    public bool IsStaggered => staggerTimer > 0f;
    public float CriticalStaggerDuration => criticalStaggerDuration;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            originalAnimatorSpeed = animator.speed;
        }
    }

    private void Update()
    {
        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.deltaTime;
            if (rb != null)
            {
                var v = rb.linearVelocity;
                v.x = 0f;
                if (freezeVerticalVelocity) v.y = 0f;
                rb.linearVelocity = v;
            }
            if (animator != null)
            {
                animator.speed = staggerAnimatorSpeed;
            }
            if (staggerTimer <= 0f)
            {
                EndStagger();
            }
        }
    }

    /// <summary>
    /// Apply a stagger for the specified duration (seconds).
    /// </summary>
    public void ApplyStagger(float duration)
    {
        if (duration <= 0f) return;
        staggerTimer = duration;
        if (debugLogs) Debug.Log($"[Enemy_StaggerController] ApplyStagger duration={duration:F2} on {name}");
    }

    /// <summary>
    /// Apply default critical stagger duration.
    /// </summary>
    public void ApplyCriticalStagger()
    {
        ApplyStagger(criticalStaggerDuration);
    }

    private void EndStagger()
    {
        staggerTimer = 0f;
        if (animator != null)
        {
            animator.speed = normalAnimatorSpeed > 0f ? normalAnimatorSpeed : originalAnimatorSpeed;
        }
        if (debugLogs) Debug.Log($"[Enemy_StaggerController] EndStagger on {name}");
    }
}
