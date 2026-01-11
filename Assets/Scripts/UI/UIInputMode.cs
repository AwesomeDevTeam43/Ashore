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
    
    // State tracking for button presses (needed when Time.timeScale = 0)
    private static bool _prevMouseLeft, _prevMouseRight, _prevMouseMiddle;
    private static bool _prevGamepadSouth, _prevGamepadNorth, _prevGamepadEast, _prevGamepadWest;
    private static bool _prevGamepadLB, _prevGamepadRB, _prevGamepadStart, _prevGamepadSelect;
    private static bool _prevAnyKey;
    private static Vector2 _prevMousePos;

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
    // Uses manual state tracking to work when Time.timeScale = 0
    public static void DetectThisFrame()
    {
        bool mouseActive = false;
        bool gamepadActive = false;
        bool keyboardActive = false;

        if (Mouse.current != null)
        {
            var mouse = Mouse.current;
            var pos = mouse.position.ReadValue();
            
            // Check mouse movement
            if ((_prevMousePos - pos).sqrMagnitude > 1f)
            {
                mouseActive = true;
            }
            _prevMousePos = pos;
            
            // Check mouse buttons with state tracking
            bool leftPressed = mouse.leftButton.isPressed;
            bool rightPressed = mouse.rightButton.isPressed;
            bool middlePressed = mouse.middleButton.isPressed;
            
            if ((leftPressed && !_prevMouseLeft) || (rightPressed && !_prevMouseRight) || (middlePressed && !_prevMouseMiddle))
            {
                mouseActive = true;
            }
            
            _prevMouseLeft = leftPressed;
            _prevMouseRight = rightPressed;
            _prevMouseMiddle = middlePressed;
            
            // Check scroll
            if (Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f)
            {
                mouseActive = true;
            }
        }
        
        if (Gamepad.current != null)
        {
            var gp = Gamepad.current;
            
            // Check buttons with state tracking
            bool south = gp.buttonSouth.isPressed;
            bool north = gp.buttonNorth.isPressed;
            bool east = gp.buttonEast.isPressed;
            bool west = gp.buttonWest.isPressed;
            bool lb = gp.leftShoulder.isPressed;
            bool rb = gp.rightShoulder.isPressed;
            bool start = gp.startButton.isPressed;
            bool select = gp.selectButton.isPressed;
            
            if ((south && !_prevGamepadSouth) || (north && !_prevGamepadNorth) || 
                (east && !_prevGamepadEast) || (west && !_prevGamepadWest) ||
                (lb && !_prevGamepadLB) || (rb && !_prevGamepadRB) ||
                (start && !_prevGamepadStart) || (select && !_prevGamepadSelect))
            {
                gamepadActive = true;
            }
            
            _prevGamepadSouth = south;
            _prevGamepadNorth = north;
            _prevGamepadEast = east;
            _prevGamepadWest = west;
            _prevGamepadLB = lb;
            _prevGamepadRB = rb;
            _prevGamepadStart = start;
            _prevGamepadSelect = select;
            
            // Check sticks and dpad (these work even when paused)
            if (gp.leftStick.ReadValue().sqrMagnitude > 0.1f || 
                gp.rightStick.ReadValue().sqrMagnitude > 0.1f || 
                gp.dpad.ReadValue().sqrMagnitude > 0.1f)
            {
                gamepadActive = true;
            }
        }
        
        if (Keyboard.current != null)
        {
            // Check any key with state tracking
            bool anyKey = Keyboard.current.anyKey.isPressed;
            if (anyKey && !_prevAnyKey)
            {
                keyboardActive = true;
            }
            _prevAnyKey = anyKey;
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
