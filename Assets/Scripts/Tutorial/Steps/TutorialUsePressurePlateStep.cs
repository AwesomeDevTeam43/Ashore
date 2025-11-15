using UnityEngine;

public class TutorialUsePressurePlateStep : TutorialStep
{
    [Header("Pressure Plate Target")]
    [Tooltip("The plate or area Transform the player needs to step on.")]
    public Transform target;

    [Tooltip("Local-space center of the detection area relative to target.")]
    public Vector2 areaCenter = Vector2.zero;
    [Tooltip("Local-space size of the detection area relative to target.")]
    public Vector2 areaSize = new Vector2(2f, 2f);

    [Header("Completion Options")]
    [Tooltip("If > 0, player must remain within the area for this many seconds.")]
    public float dwellSeconds = 0f;

    [Tooltip("If true, require an Animator bool on the target (e.g., 'isPressed') to be true.")]
    public bool requireAnimatorPressed = false;
    [Tooltip("Animator to query for pressed state.")]
    public Animator targetAnimator;
    [Tooltip("Bool parameter that indicates the plate is pressed.")]
    public string pressedBoolParameter = "isPressed";

    [Tooltip("If true, completion waits for an external plate press event instead of (or in addition to) area/anim checks.")]
    public bool completeOnPlateEvent = false;
    private bool plateEventReceived;
    [Tooltip("Assign the Pressure_Plate component to auto-subscribe to its onPressed event when the step begins.")]
    public Pressure_Plate pressurePlateSource;

    private GameObject player;
    private float insideTimer;

    private void Reset()
    {
        freezePlayerMovement = false;
        dimScreen = true; // overlay dim is globally disabled
    }

    public override void Begin(TutorialManager mgr)
    {
        // Force no-freeze behavior for this step regardless of inspector overrides
        this.freezeGameSeconds = 0f;
        this.freezeUntilPlayerInput = false;
        this.freezePlayerMovement = false;
        base.Begin(mgr);
        insideTimer = 0f;
        plateEventReceived = false;
        if (mgr != null && mgr.player != null) player = mgr.player;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        // Auto-subscribe to plate event if configured
        if (completeOnPlateEvent && pressurePlateSource == null && target != null)
        {
            pressurePlateSource = target.GetComponentInChildren<Pressure_Plate>();
        }
        if (completeOnPlateEvent && pressurePlateSource != null)
        {
            pressurePlateSource.onPressed.AddListener(OnPlatePressedEvent);
        }
    }

    public override bool IsComplete()
    {
        if (target == null || player == null) return false;

        // External event shortcut
        if (completeOnPlateEvent && plateEventReceived)
        {
            // If also requiring animator pressed, ensure that state
            if (requireAnimatorPressed)
            {
                if (targetAnimator == null) targetAnimator = target.GetComponentInChildren<Animator>();
                if (targetAnimator != null && !string.IsNullOrEmpty(pressedBoolParameter))
                {
                    bool pressedState = false;
                    try { pressedState = targetAnimator.GetBool(pressedBoolParameter); } catch { pressedState = false; }
                    if (!pressedState) return false;
                }
            }
            return true;
        }

        bool inside = IsPlayerInArea();
        if (!inside)
        {
            insideTimer = 0f;
            return false;
        }

        if (requireAnimatorPressed)
        {
            if (targetAnimator == null) targetAnimator = target.GetComponentInChildren<Animator>();
            if (targetAnimator == null || string.IsNullOrEmpty(pressedBoolParameter)) return false;
            bool pressed = false;
            try { pressed = targetAnimator.GetBool(pressedBoolParameter); } catch { pressed = false; }
            if (!pressed) return false;
        }

        if (dwellSeconds <= 0f) return true;
        insideTimer += Time.deltaTime;
        return insideTimer >= dwellSeconds;
    }

    private bool IsPlayerInArea()
    {
        Vector3 localPos = target.InverseTransformPoint(player.transform.position);
        Vector2 half = areaSize * 0.5f;
        float minX = areaCenter.x - half.x;
        float maxX = areaCenter.x + half.x;
        float minY = areaCenter.y - half.y;
        float maxY = areaCenter.y + half.y;
        return (localPos.x >= minX && localPos.x <= maxX && localPos.y >= minY && localPos.y <= maxY);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.75f);
        Vector3 worldCenter = target.TransformPoint(areaCenter);
        Vector3 worldSize = new Vector3(areaSize.x, areaSize.y, 0.01f);
        Gizmos.DrawWireCube(worldCenter, worldSize);
    }
#endif

    // Called by pressure_plate.cs (or equivalent) when the plate is pressed down.
    public void NotifyPlatePressed()
    {
        plateEventReceived = true;
    }

    private void OnPlatePressedEvent()
    {
        plateEventReceived = true;
    }

    public override void End()
    {
        base.End();
        if (pressurePlateSource != null)
        {
            pressurePlateSource.onPressed.RemoveListener(OnPlatePressedEvent);
        }
    }
}
