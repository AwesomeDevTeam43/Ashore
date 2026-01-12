using UnityEngine;

public class TutorialOpenInventoryStep : TutorialStep
{
    [Header("Completion Conditions")]
    [Tooltip("Require the player to press the Inventory key (I / bound action) during this step.")]
    public bool requireKeyPress = true;
    [Tooltip("Require that the inventory UI is open (any InventoryPage active).")]
    public bool requireInventoryOpen = true;

    private bool keyPressed;

    private void Reset()
    {
        freezePlayerMovement = false;
        dimScreen = true; // dim disabled globally in overlay
        requiresInventoryFocus = true;
    }

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        keyPressed = false;
    }

    public override bool IsComplete()
    {
        if (!HasMinimumFramesPassed()) return false;
        
        bool keyOk = !requireKeyPress || WasInventoryKeyPressedThisFrame();
        if (WasInventoryKeyPressedThisFrame()) keyPressed = true;

        bool invOk = !requireInventoryOpen || IsInventoryOpen();

        // If both are required, ensure the key press happened during the step
        if (requireKeyPress && requireInventoryOpen)
            return keyPressed && invOk;

        return keyOk && invOk;
    }

    private bool WasInventoryKeyPressedThisFrame()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.iKey.wasPressedThisFrame) return true;
            if (kb.tabKey.wasPressedThisFrame) return true; // common alt binding
        }
        var gp = UnityEngine.InputSystem.Gamepad.current;
        if (gp != null)
        {
            if (gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame) return true;
        }
        // If Player_InputHandler exposes an Inventory action, it would be checked here.
        return false;
    }

    private bool IsInventoryOpen()
    {
        var pages = FindObjectsByType<InventoryPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in pages)
        {
            if (p != null && p.isActiveAndEnabled && p.gameObject.activeInHierarchy)
                return true;
        }
        return false;
    }
}
