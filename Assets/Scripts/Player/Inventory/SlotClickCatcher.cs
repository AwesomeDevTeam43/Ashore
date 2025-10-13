using UnityEngine;
using UnityEngine.EventSystems;

// Simple component that forwards pointer clicks to the inventory UI. This is more
// reliable in some setups than relying on Button components alone.
public class SlotClickCatcher : MonoBehaviour, IPointerClickHandler
{
    public Inventoy_UI ui;
    public int slotIndex = -1;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"SlotClickCatcher: pointer click on slotIndex={slotIndex} (gameObject={gameObject.name})");
        if (ui != null && slotIndex >= 0)
        {
            ui.OnSlotClicked(slotIndex);
        }
    }
}
