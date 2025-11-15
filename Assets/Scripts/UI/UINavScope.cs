using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Constrains keyboard/controller selection to only elements marked with UINavTarget.
// Optionally disables navigation on all other Selectables (while still allowing pointer clicks).
public class UINavScope : MonoBehaviour
{
    [Tooltip("Root under which to enforce navigation. Defaults to this GameObject.")]
    [SerializeField] private Transform scopeRoot;

    [Tooltip("If true, non-target Selectables get Navigation=None while this scope is enabled.")]
    [SerializeField] private bool filterNonTargets = true;

    private readonly Dictionary<Selectable, Navigation> originalNav = new();
    private readonly HashSet<GameObject> allowed = new();
    private GameObject lastValid;

    private void Awake()
    {
        if (scopeRoot == null) scopeRoot = this.transform;
    }

    private void OnEnable()
    {
        Rebuild();
        EnsureValidSelection(initial: true);
    }

    private void OnDisable()
    {
        RestoreNavigation();
    }

    private void Update()
    {
        EnsureValidSelection();
    }

    public void Rebuild()
    {
        RestoreNavigation();
        allowed.Clear();

        if (scopeRoot == null || !scopeRoot.gameObject.activeInHierarchy)
            return;

        // Build allowed set from UINavTarget markers
        var targets = scopeRoot.GetComponentsInChildren<UINavTarget>(true);
        foreach (var t in targets)
        {
            if (t == null) continue;
            var sel = t.GetComponent<Selectable>();
            if (sel == null) continue;
            if (!sel.gameObject.activeInHierarchy) continue;
            if (!sel.IsActive()) continue;
            allowed.Add(sel.gameObject);
        }

        if (allowed.Count == 0)
        {
            // Nothing explicitly marked: do not filter; fall back to normal behavior
            return;
        }

        if (filterNonTargets)
        {
            var all = scopeRoot.GetComponentsInChildren<Selectable>(true);
            foreach (var s in all)
            {
                if (s == null) continue;
                if (!s.gameObject.activeInHierarchy) continue;
                if (!s.IsActive()) continue;

                if (!allowed.Contains(s.gameObject))
                {
                    // Store original navigation and set to None so stick/keys can't land here
                    if (!originalNav.ContainsKey(s))
                        originalNav[s] = s.navigation;
                    var nav = s.navigation;
                    nav.mode = Navigation.Mode.None;
                    s.navigation = nav;
                }
            }
        }
    }

    private void RestoreNavigation()
    {
        if (originalNav.Count == 0) return;
        foreach (var kvp in originalNav)
        {
            if (kvp.Key != null)
            {
                kvp.Key.navigation = kvp.Value;
            }
        }
        originalNav.Clear();
    }

    private bool IsAllowed(GameObject go)
    {
        if (go == null) return false;
        if (allowed.Count == 0) return true; // nothing marked -> allow all
        return allowed.Contains(go);
    }

    private GameObject FirstAllowed()
    {
        if (allowed.Count == 0)
        {
            // Fallback to any selectable
            var s = scopeRoot.GetComponentInChildren<Selectable>(true);
            return s != null ? s.gameObject : null;
        }
        foreach (var go in allowed)
        {
            if (go != null && go.activeInHierarchy)
                return go;
        }
        return null;
    }

    private void EnsureValidSelection(bool initial = false)
    {
        var es = EventSystem.current;
        if (es == null) return;
        if (scopeRoot == null || !scopeRoot.gameObject.activeInHierarchy) return;

        var sel = es.currentSelectedGameObject;
        if (IsAllowed(sel))
        {
            lastValid = sel;
            return;
        }

        // When initial or when leaving allowed set, redirect to last valid or first allowed
        GameObject target = lastValid;
        if (!IsAllowed(target)) target = FirstAllowed();
        if (target != null)
        {
            es.SetSelectedGameObject(target);
            lastValid = target;
        }
    }
}
