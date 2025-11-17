using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach to the root of the inventory item context menu.
// Ensures first selectable is focused when menu enables and provides
// keyboard/gamepad navigation + cancel handling.
public class InventoryContextMenuNavigator : MonoBehaviour
{
    [Tooltip("Optional explicit list of menu buttons. If empty, child Selectables are auto-collected on enable.")]
    public List<Selectable> menuItems = new List<Selectable>();

    [Tooltip("Close the menu when Escape / RightClick / B / Circle is pressed.")]
    public bool enableCancelInputs = true;

    [Tooltip("Cycle selection when reaching ends instead of stopping.")]
    public bool wrapNavigation = true;

    private int currentIndex = -1;
    private GameObject previouslySelected;

    private void OnEnable()
    {
        BuildItemListIfNeeded();
        FocusFirst();
        // Ensure an EventSystem exists (prefer existing persistent one)
        if (EventSystem.current == null)
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem)
#if ENABLE_INPUT_SYSTEM
                    , typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)
#else
                    , typeof(StandaloneInputModule)
#endif
                );
                DontDestroyOnLoad(go);
            }
        }
    }

    private void OnDisable()
    {
        if (EventSystem.current != null && previouslySelected != null)
        {
            EventSystem.current.SetSelectedGameObject(previouslySelected);
        }
        previouslySelected = null;
    }

    private void BuildItemListIfNeeded()
    {
        // Always rebuild to catch runtime-created buttons
        menuItems.Clear();
        var selectables = GetComponentsInChildren<Selectable>(includeInactive:true);
        foreach (var s in selectables)
        {
            if (s != null && s.gameObject.activeInHierarchy && s.IsInteractable()) menuItems.Add(s);
        }
    }

    private void FocusFirst()
    {
        if (EventSystem.current == null) return;
        if (menuItems.Count == 0) return;
        previouslySelected = EventSystem.current.currentSelectedGameObject;
        currentIndex = 0;
        EventSystem.current.SetSelectedGameObject(menuItems[currentIndex].gameObject);
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        // Rebuild list every frame briefly if empty (handles late population)
        if (menuItems.Count == 0)
        {
            BuildItemListIfNeeded();
            if (menuItems.Count > 0 && (EventSystem.current.currentSelectedGameObject == null || !menuItems.Contains(EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>())))
            {
                FocusFirst();
            }
        }
        // Keep currentIndex in sync with the actual EventSystem selection
        SyncIndexFromEventSystem();
        if (menuItems.Count == 0) return;

        HandleMoveInputs();
        HandleSubmitCancel();
    }

    private void SyncIndexFromEventSystem()
    {
        var es = EventSystem.current;
        if (es == null) return;
        var sel = es.currentSelectedGameObject;
        if (sel == null) return;
        for (int i = 0; i < menuItems.Count; i++)
        {
            var it = menuItems[i];
            if (it == null) continue;
            if (it.gameObject == sel)
            {
                currentIndex = i;
                return;
            }
        }
    }

    private void HandleMoveInputs()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var gp = UnityEngine.InputSystem.Gamepad.current;
        bool up = false, down = false;

        if (kb != null)
        {
            up |= kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
            down |= kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
            // Optional left/right could jump by 1 as well
            up |= kb.tabKey.wasPressedThisFrame && kb.leftShiftKey.isPressed; // Shift+Tab reverse
            down |= kb.tabKey.wasPressedThisFrame && !kb.leftShiftKey.isPressed;
        }
        if (gp != null)
        {
            up |= gp.dpad.up.wasPressedThisFrame || gp.leftStick.up.wasPressedThisFrame;
            down |= gp.dpad.down.wasPressedThisFrame || gp.leftStick.down.wasPressedThisFrame;
        }

        if (up) MoveSelection(-1);
        else if (down) MoveSelection(1);
    }

    private void MoveSelection(int delta)
    {
        if (menuItems.Count == 0) return;
        // If currentIndex is invalid or stale, derive from current EventSystem selection
        if (currentIndex < 0 || currentIndex >= menuItems.Count)
        {
            var es = EventSystem.current;
            if (es != null)
            {
                var sel = es.currentSelectedGameObject;
                int idx = -1;
                for (int i = 0; i < menuItems.Count; i++)
                {
                    var it = menuItems[i];
                    if (it != null && it.gameObject == sel) { idx = i; break; }
                }
                currentIndex = (idx >= 0) ? idx : 0;
            }
            else
            {
                currentIndex = 0;
            }
        }

        int newIndex = currentIndex + delta;
        if (wrapNavigation)
        {
            if (newIndex < 0) newIndex = menuItems.Count - 1;
            if (newIndex >= menuItems.Count) newIndex = 0;
        }
        else
        {
            newIndex = Mathf.Clamp(newIndex, 0, menuItems.Count - 1);
        }
        if (newIndex == currentIndex) return;
        currentIndex = newIndex;
        EventSystem.current.SetSelectedGameObject(menuItems[currentIndex].gameObject);
    }

    private void HandleSubmitCancel()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var gp = UnityEngine.InputSystem.Gamepad.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;

        bool submit = false;
        bool cancel = false;

        if (kb != null)
        {
            submit |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
            cancel |= kb.escapeKey.wasPressedThisFrame;
        }
        if (gp != null)
        {
            submit |= gp.buttonSouth.wasPressedThisFrame; // A / Cross
            cancel |= gp.buttonEast.wasPressedThisFrame || gp.startButton.wasPressedThisFrame; // B / Circle or Start
        }
        if (mouse != null)
        {
            cancel |= mouse.rightButton.wasPressedThisFrame;
        }

        if (submit && currentIndex >= 0 && currentIndex < menuItems.Count)
        {
            var btn = menuItems[currentIndex] as Button;
            if (btn != null) btn.onClick.Invoke();
            else
            {
                // Try submitting via selectable handler
                ExecuteEvents.Execute(menuItems[currentIndex].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }
        if (enableCancelInputs && cancel)
        {
            // Disable the menu root to close; caller responsible for re-open.
            gameObject.SetActive(false);
        }
    }
}
