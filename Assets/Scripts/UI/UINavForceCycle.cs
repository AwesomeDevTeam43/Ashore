using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Forces UI navigation to simply cycle through all active UINavTarget elements under a scope,
// ignoring direction. Any nav input (up/down/left/right, dpad, left stick) advances to the next target.
public class UINavForceCycle : MonoBehaviour
{
    [Tooltip("Root under which to search for UINavTarget elements. Defaults to this GameObject.")]
    public Transform scopeRoot;

    [Tooltip("Seconds between repeated advances when holding a stick/dpad.")]
    public float repeatDelay = 0.18f;

    [Tooltip("Minimum stick magnitude to register a navigation.")]
    public float stickThreshold = 0.6f;

    private float nextAllowedTime = 0f;
    private List<GameObject> cache = new List<GameObject>();
    private int lastVersion = -1;

    private void Awake()
    {
        if (scopeRoot == null) scopeRoot = this.transform;
    }

    private void OnEnable()
    {
        RebuildCache();
        EnsureAnySelected();
    }

    private void OnDisable()
    {
        cache.Clear();
    }

    private void Update()
    {
        if (scopeRoot == null || !scopeRoot.gameObject.activeInHierarchy) return;

        // Rebuild if hierarchy likely changed (simple cheap heuristic: child count)
        int v = scopeRoot.childCount;
        if (v != lastVersion)
        {
            RebuildCache();
        }

        if (!HasNavPressedThisFrame()) return;
        if (Time.unscaledTime < nextAllowedTime) return;
        nextAllowedTime = Time.unscaledTime + repeatDelay;

        SelectNext(+1);
    }

    private void RebuildCache()
    {
        cache.Clear();
        if (scopeRoot == null) return;
        var targets = scopeRoot.GetComponentsInChildren<UINavTarget>(true);
        foreach (var t in targets)
        {
            if (t == null) continue;
            var go = t.gameObject;
            if (!go.activeInHierarchy) continue;
            var sel = go.GetComponent<Selectable>();
            if (sel == null || !sel.IsActive() || !sel.interactable) continue;
            cache.Add(go);
        }
        lastVersion = scopeRoot != null ? scopeRoot.childCount : -1;
    }

    private void EnsureAnySelected()
    {
        if (cache.Count == 0) return;
        var es = EventSystem.current;
        if (es == null) return;
        if (es.currentSelectedGameObject == null || !cache.Contains(es.currentSelectedGameObject))
        {
            es.SetSelectedGameObject(cache[0]);
        }
    }

    private void SelectNext(int dir)
    {
        var es = EventSystem.current;
        if (es == null) return;
        if (cache.Count == 0)
        {
            RebuildCache();
            if (cache.Count == 0) return;
        }

        int idx = 0;
        var cur = es.currentSelectedGameObject;
        if (cur != null)
        {
            idx = cache.IndexOf(cur);
            if (idx < 0) idx = 0;
        }

        int next = (idx + dir + cache.Count) % cache.Count;
        var target = cache[next];
        if (target != null)
        {
            es.SetSelectedGameObject(target);
        }
    }

    private bool HasNavPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        bool key = (kb != null) && (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
        bool dpad = (gp != null) && (gp.dpad.up.wasPressedThisFrame || gp.dpad.down.wasPressedThisFrame || gp.dpad.left.wasPressedThisFrame || gp.dpad.right.wasPressedThisFrame);
        bool stick = false;
        if (gp != null)
        {
            var v = gp.leftStick.ReadValue();
            if (v.sqrMagnitude > (stickThreshold * stickThreshold))
            {
                // Only treat as new press when crossing threshold (simple cooldown handles repeat)
                stick = true;
            }
        }
        return key || dpad || stick;
#else
        // Legacy Input fallback
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D);
#endif
    }
}
