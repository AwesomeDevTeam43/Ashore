using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Centralized UI input mode detection. Mode.Pointer when mouse is in use, Mode.NonPointer when
// keyboard/controller navigation is in use. Raises OnChanged when the mode flips.
public static class UIInputMode
{
    public enum Mode { Pointer, NonPointer }

    public static Mode Current { get; private set; } = Mode.Pointer;
    public static event Action<Mode> OnChanged;

    public static void Set(Mode m)
    {
        if (Current != m)
        {
            Current = m;
            OnChanged?.Invoke(Current);
        }
    }

    // Helper: detect and set mode based on current frame device activity
    public static void DetectThisFrame()
    {
        bool mouseActive = false;
        bool nonPointerActive = false;

        if (Mouse.current != null)
        {
            var delta = Mouse.current.delta.ReadValue();
            mouseActive |= delta.sqrMagnitude > 0.0001f;
            mouseActive |= (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame);
            mouseActive |= Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.01f;
        }
        if (Gamepad.current != null)
        {
            var gp = Gamepad.current;
            nonPointerActive |= gp.buttonSouth.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame
                                || gp.leftShoulder.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame || gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame
                                || gp.leftStick.ReadValue().sqrMagnitude > 0.1f || gp.rightStick.ReadValue().sqrMagnitude > 0.1f || gp.dpad.ReadValue().sqrMagnitude > 0.1f;
        }
        if (Keyboard.current != null)
        {
            nonPointerActive |= Keyboard.current.anyKey.wasPressedThisFrame;
        }

        if (mouseActive)
        {
            Set(Mode.Pointer);
        }
        else if (nonPointerActive)
        {
            Set(Mode.NonPointer);
        }
    }
}
