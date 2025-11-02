using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Centralized UI input mode detection. Mode.Pointer when mouse is in use, Mode.NonPointer when
// keyboard/controller navigation is in use. Raises OnChanged when the mode flips.
public static class UIInputMode
{
    // Visual preference for highlights: pointer (hover) vs non-pointer (selection/focus)
    public enum Mode { Pointer, NonPointer }
    // Control scheme used globally: groups keyboard with mouse vs dedicated gamepad
    public enum Scheme { MouseKeyboard, Gamepad }

    public static Mode Current { get; private set; } = Mode.Pointer;
    public static Scheme CurrentScheme { get; private set; } = Scheme.MouseKeyboard;

    public static event Action<Mode> OnChanged;
    public static event Action<Scheme> OnSchemeChanged;

    public static void Set(Mode m)
    {
        if (Current != m)
        {
            Current = m;
            OnChanged?.Invoke(Current);
        }
    }

    public static void SetScheme(Scheme s)
    {
        if (CurrentScheme != s)
        {
            CurrentScheme = s;
            OnSchemeChanged?.Invoke(CurrentScheme);
        }
    }

    // Helper: detect and set mode and scheme based on current frame device activity
    public static void DetectThisFrame()
    {
        bool mouseActive = false;
        bool gamepadActive = false;
        bool keyboardActive = false;

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
            gamepadActive |= gp.buttonSouth.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame
                              || gp.leftShoulder.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame || gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame
                              || gp.leftStick.ReadValue().sqrMagnitude > 0.1f || gp.rightStick.ReadValue().sqrMagnitude > 0.1f || gp.dpad.ReadValue().sqrMagnitude > 0.1f;
        }
        if (Keyboard.current != null)
        {
            keyboardActive |= Keyboard.current.anyKey.wasPressedThisFrame;
        }

        if (mouseActive)
        {
            SetScheme(Scheme.MouseKeyboard);
            Set(Mode.Pointer);
        }
        else if (keyboardActive)
        {
            SetScheme(Scheme.MouseKeyboard);
            Set(Mode.NonPointer);
        }
        else if (gamepadActive)
        {
            SetScheme(Scheme.Gamepad);
            Set(Mode.NonPointer);
        }
    }
}
