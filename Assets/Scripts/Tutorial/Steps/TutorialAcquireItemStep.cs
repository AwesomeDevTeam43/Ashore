using UnityEngine;

public class TutorialAcquireItemStep : TutorialStep
{
    [Header("Item Requirement")]
    public ItemData item;
    public int quantity = 1;

    private void Reset()
    {
        requiresInventoryFocus = false;
    }

    public override bool IsComplete()
    {
        if (!HasMinimumFramesPassed()) return false;
        if (Inventory.instance == null || item == null) return false;
        return Inventory.instance.GetItemQuantity(item) >= Mathf.Max(1, quantity);
    }
}
