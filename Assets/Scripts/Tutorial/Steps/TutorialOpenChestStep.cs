using UnityEngine;

public class TutorialOpenChestStep : TutorialStep
{
    [Header("Target Chest")]
    [Tooltip("World target the player should approach and interact with (e.g., the chest root transform).")]
    public Transform target;

    [Tooltip("Local-space center of the allowed interaction area, relative to target.")]
    public Vector2 areaCenter = Vector2.zero;
    [Tooltip("Local-space size of the allowed interaction area, relative to target.")]
    public Vector2 areaSize = new Vector2(2f, 2f);

    [Header("Open State (optional)")]
    [Tooltip("If true, the step only completes after the target's Animator reports an 'open' bool.")]
    public bool requireAnimatorOpen = false;
    [Tooltip("Animator to query for an 'open' state.")]
    public Animator targetAnimator;
    [Tooltip("Bool parameter name on the Animator that becomes true when opened.")]
    public string openBoolParameter = "isOpen";

    private GameObject player;
    private Player_InputHandler inputHandler;
    private bool interactDetected;

    private void Reset()
    {
        // For an interact-and-move step, do not freeze movement by default
        freezePlayerMovement = false;
        dimScreen = true;
        requiresInventoryFocus = false;
    }

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        if (mgr != null && mgr.player != null)
        {
            player = mgr.player;
            inputHandler = player.GetComponent<Player_InputHandler>();
        }
        else
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) inputHandler = player.GetComponent<Player_InputHandler>();
        }
        interactDetected = false;
    }

    public override bool IsComplete()
    {
        if (!HasMinimumFramesPassed()) return false;
        if (target == null || player == null) return false;

        // If we already detected interact, optionally wait for animator open state
        if (interactDetected)
        {
            if (!requireAnimatorOpen) return true;
            if (targetAnimator == null) targetAnimator = target.GetComponentInChildren<Animator>();
            if (targetAnimator != null && !string.IsNullOrEmpty(openBoolParameter))
            {
                bool opened = false;
                try { opened = targetAnimator.GetBool(openBoolParameter); }
                catch { opened = false; }
                if (opened) return true;
            }
            return false;
        }

        // Check if the player is inside the defined local-space area around the target
        if (!IsPlayerInArea()) return false;

        // Detect interact input this frame
        if (IsInteractPressedThisFrame())
        {
            interactDetected = true;
            if (!requireAnimatorOpen) return true;
        }

        return false;
    }

    private bool IsPlayerInArea()
    {
        if (player == null || target == null) return false;
        Vector3 localPos = target.InverseTransformPoint(player.transform.position);
        Vector2 half = areaSize * 0.5f;
        float minX = areaCenter.x - half.x;
        float maxX = areaCenter.x + half.x;
        float minY = areaCenter.y - half.y;
        float maxY = areaCenter.y + half.y;
        return (localPos.x >= minX && localPos.x <= maxX && localPos.y >= minY && localPos.y <= maxY);
    }

    private bool IsInteractPressedThisFrame()
    {
        // Prefer the player's input handler if available (uses the project's Interact binding)
        if (inputHandler != null && inputHandler.InteractActionTriggered) return true;

        // Fallback polling: keyboard/gamepad
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            // Only allow explicit interact keys; avoid UI submit keys (Enter/Space)
            if (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)
                return true;
        }
        var gp = UnityEngine.InputSystem.Gamepad.current;
        if (gp != null)
        {
            // Proper interact fallback: ButtonEast
            if (gp.buttonEast.wasPressedThisFrame) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.75f);
        Vector3 worldCenter = target.TransformPoint(areaCenter);
        Vector3 worldSize = new Vector3(areaSize.x, areaSize.y, 0.01f);
        Gizmos.DrawWireCube(worldCenter, worldSize);
    }
#endif
}
