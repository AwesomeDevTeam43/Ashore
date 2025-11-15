using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ensures keyboard/controller navigation always stays on a valid, interactive Selectable.
// If selection becomes null or lands on a non-interactive/non-selectable object, it redirects
// to the last valid selection or the first valid selectable under the scope root.
public class UISelectionGuard : MonoBehaviour
{
    [Tooltip("Root under which to find valid Selectables. Defaults to this GameObject.")]
    [SerializeField] private Transform scopeRoot;

    [Tooltip("Only consider Selectables that are interactable and active.")]
    [SerializeField] private bool onlyInteractive = true;

    private GameObject lastValidSelected;

    private void Awake()
    {
        if (scopeRoot == null) scopeRoot = this.transform;
    }

    private void OnEnable()
    {
        // Try to correct selection immediately when enabled
        EnsureValidSelection();
    }

    private void Update()
    {
        if (!isActiveAndEnabled) return;
        if (scopeRoot == null || !scopeRoot.gameObject.activeInHierarchy) return;
        EnsureValidSelection();
    }

    private void EnsureValidSelection()
    {
        var es = EventSystem.current;
        if (es == null) return;

        var selGO = es.currentSelectedGameObject;
        if (IsValidSelectableGO(selGO))
        {
            lastValidSelected = selGO;
            return;
        }

        // If current selection is invalid, try to use last valid selection
        if (IsValidSelectableGO(lastValidSelected))
        {
            es.SetSelectedGameObject(lastValidSelected);
            return;
        }

        // Fallback: find first valid selectable under scope
        var fallback = FindFirstValidSelectableGO();
        if (fallback != null)
        {
            es.SetSelectedGameObject(fallback);
            lastValidSelected = fallback;
        }
    }

    private bool IsValidSelectableGO(GameObject go)
    {
        if (go == null) return false;
        if (!go.activeInHierarchy) return false;
        var selectable = go.GetComponent<Selectable>();
        if (selectable == null) return false;
        if (!selectable.IsActive()) return false; // includes hierarchy active & component enabled
        if (onlyInteractive && !selectable.interactable) return false;
        return true;
    }

    private GameObject FindFirstValidSelectableGO()
    {
        if (scopeRoot == null) return null;
        var selectables = scopeRoot.GetComponentsInChildren<Selectable>(true);
        foreach (var s in selectables)
        {
            if (s == null) continue;
            if (!s.gameObject.activeInHierarchy) continue;
            if (!s.IsActive()) continue;
            if (onlyInteractive && !s.interactable) continue;
            return s.gameObject;
        }
        return null;
    }
}
