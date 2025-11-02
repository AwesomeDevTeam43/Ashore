using UnityEngine;
using UnityEngine.EventSystems;

// Legacy helper. Kept as a no-op to avoid compile references to the removed legacy inventory UI.
public class SlotClickCatcher : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex = -1; // no longer used

    public void OnPointerClick(PointerEventData eventData)
    {
        // Intentionally no-op; InventoryPage wires button clicks directly.
    }
}
