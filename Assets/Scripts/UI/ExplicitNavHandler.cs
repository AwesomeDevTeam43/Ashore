using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ensures directional navigation (Up/Down/Left/Right) follows the Selectable's
// explicit Navigation targets even if default UI navigation is interfered with.
// Attach to Selectables that use Navigation.Mode.Explicit.
public class ExplicitNavHandler : MonoBehaviour, IMoveHandler
{
    private Selectable sel;

    private void Awake()
    {
        sel = GetComponent<Selectable>();
    }

    public void OnMove(AxisEventData eventData)
    {
        if (sel == null) return;
        var nav = sel.navigation;
        if (nav.mode != Navigation.Mode.Explicit) return;

        Selectable target = null;
        switch (eventData.moveDir)
        {
            case MoveDirection.Up:
                target = nav.selectOnUp; break;
            case MoveDirection.Down:
                target = nav.selectOnDown; break;
            case MoveDirection.Left:
                target = nav.selectOnLeft; break;
            case MoveDirection.Right:
                target = nav.selectOnRight; break;
            default:
                break;
        }

        if (target != null && target.IsActive() && target.interactable)
        {
            var es = EventSystem.current;
            if (es != null)
            {
                es.SetSelectedGameObject(target.gameObject);
                eventData.Use();
            }
        }
    }
}
