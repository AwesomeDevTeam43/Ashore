using UnityEngine;

public abstract class TutorialStep : MonoBehaviour
{
    [TextArea]
    public string instructionText;

    [Header("Overlay Highlight (optional)")]
    [Tooltip("Highlight a UI element while this step is active. Leave null for no highlight.")]
    public RectTransform highlightTarget;

    [Header("Gating")]
    [Tooltip("Disable player movement during this step.")]
    public bool freezePlayerMovement = true;
    [Tooltip("Dim the screen while step runs, except for the highlighted area.")]
    public bool dimScreen = true;
    [Tooltip("If true, this step is focused on inventory interactions (equip/use/etc). Prevents dim reduction when inventory is open.")]
    public bool requiresInventoryFocus = false;
    [Tooltip("If > 0, freeze the entire game (Time.timeScale = 0) for this many seconds when the step begins.")]
    public float freezeGameSeconds = 0f;
    [Tooltip("If true, freeze the game until the player interacts (Interact key/button or movement) after a short realtime cooldown.")]
    public bool freezeUntilPlayerInput = false;
    [Tooltip("Realtime cooldown before input can unfreeze, when using 'freezeUntilPlayerInput'.")]
    public float inputUnfreezeCooldown = 0.25f;

    protected TutorialManager manager;

    public virtual void Begin(TutorialManager mgr)
    {
        manager = mgr;
        if (dimScreen) manager.Overlay?.SetDim(true);
        manager.ApplyInstructionText(instructionText);
        manager.Overlay?.SetHighlight(highlightTarget);
        if (freezePlayerMovement) manager.SetPlayerMovementEnabled(false);
        if (freezeGameSeconds > 0f) manager.FreezeGameForSeconds(freezeGameSeconds);
        else if (freezeUntilPlayerInput) manager.FreezeUntilInput(inputUnfreezeCooldown);
    }

    public abstract bool IsComplete();

    public virtual void End()
    {
        manager.Overlay?.SetHighlight(null);
        manager.ApplyInstructionText(string.Empty);
        manager.Overlay?.SetDim(false);
        if (freezePlayerMovement) manager.SetPlayerMovementEnabled(true);
    }
}
