using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.SceneManagement;

// Global manager to apply UI input mode across the entire game.
// - Detects device usage every frame
// - Switches cursor visibility
// - Enables/disables pointer actions on the InputSystemUIInputModule
// Attach once in any bootstrap scene or create via code; it persists across scenes.
public class UIInputModeManager : MonoBehaviour
{
    private InputSystemUIInputModule uiModule;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        var existing = Object.FindFirstObjectByType<UIInputModeManager>();
        if (existing == null)
        {
            var go = new GameObject("UIInputModeManager");
            go.AddComponent<UIInputModeManager>();
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindUIModule();
    Apply(UIInputMode.CurrentScheme);
    UIInputMode.OnSchemeChanged += Apply;
    }

    private void OnDestroy()
    {
    UIInputMode.OnSchemeChanged -= Apply;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // Always detect globally
        UIInputMode.DetectThisFrame();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIModule();
    Apply(UIInputMode.CurrentScheme);
    }

    private void FindUIModule()
    {
        // Prefer the current EventSystem's module
        uiModule = FindFirstObjectByType<InputSystemUIInputModule>();
    }

    private void Apply(UIInputMode.Scheme scheme)
    {
        bool mouseKeyboard = scheme == UIInputMode.Scheme.MouseKeyboard;

        // Cursor handling (optional): hide in controller/keyboard mode
        Cursor.visible = mouseKeyboard;
        if (mouseKeyboard)
            Cursor.lockState = CursorLockMode.None;
        else
            Cursor.lockState = CursorLockMode.Locked; // hide and keep centered for stick navigation

        if (uiModule == null)
        {
            FindUIModule();
            if (uiModule == null) return;
        }

        // Toggle pointer-related actions on the UI module
        try
        {
            var pt = uiModule.point.action; if (pt != null) { if (mouseKeyboard && !pt.enabled) pt.Enable(); else if (!mouseKeyboard && pt.enabled) pt.Disable(); }
            var lc = uiModule.leftClick.action; if (lc != null) { if (mouseKeyboard && !lc.enabled) lc.Enable(); else if (!mouseKeyboard && lc.enabled) lc.Disable(); }
            var rc = uiModule.rightClick.action; if (rc != null) { if (mouseKeyboard && !rc.enabled) rc.Enable(); else if (!mouseKeyboard && rc.enabled) rc.Disable(); }
            var mc = uiModule.middleClick.action; if (mc != null) { if (mouseKeyboard && !mc.enabled) mc.Enable(); else if (!mouseKeyboard && mc.enabled) mc.Disable(); }
            var sw = uiModule.scrollWheel.action; if (sw != null) { if (mouseKeyboard && !sw.enabled) sw.Enable(); else if (!mouseKeyboard && sw.enabled) sw.Disable(); }
            // Ensure move/submit/cancel are ALWAYS enabled in both modes so keyboard/controller navigation works
            var mv = uiModule.move.action; if (mv != null && !mv.enabled) mv.Enable();
            var sb = uiModule.submit.action; if (sb != null && !sb.enabled) sb.Enable();
            var cn = uiModule.cancel.action; if (cn != null && !cn.enabled) cn.Enable();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"UIInputModeManager: Failed to toggle UI actions: {ex.Message}");
        }

        // Optional: try to switch PlayerInput control scheme if available
        try
        {
            var inputs = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pi in inputs)
            {
                if (pi == null || pi.actions == null) continue;
                if (mouseKeyboard)
                {
                    if (pi.currentControlScheme != null && pi.currentControlScheme.Equals("Keyboard&Mouse")) continue;
                    if (pi.actions.controlSchemes.Count > 0)
                    {
                        // Try common names; if not present, let PlayerInput auto-switch by itself
                        var hasKM = pi.actions.controlSchemes.Any(s => s.name == "Keyboard&Mouse" || s.name == "KeyboardMouse" || s.name == "KBM");
                        if (hasKM)
                        {
                            // Use both keyboard and mouse devices if present
                            var keyboard = Keyboard.current as InputDevice;
                            var mouse = Mouse.current as InputDevice;
                            if (keyboard != null && mouse != null)
                                pi.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
                            else if (keyboard != null)
                                pi.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard);
                            else if (mouse != null)
                                pi.SwitchCurrentControlScheme("Keyboard&Mouse", mouse);
                        }
                    }
                }
                else // Gamepad
                {
                    if (pi.currentControlScheme != null && pi.currentControlScheme.Equals("Gamepad")) continue;
                    if (pi.actions.controlSchemes.Count > 0)
                    {
                        var hasGp = pi.actions.controlSchemes.Any(s => s.name == "Gamepad");
                        if (hasGp && Gamepad.current != null)
                        {
                            pi.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log($"UIInputModeManager: ControlScheme switch skipped: {ex.Message}");
        }
    }
}
