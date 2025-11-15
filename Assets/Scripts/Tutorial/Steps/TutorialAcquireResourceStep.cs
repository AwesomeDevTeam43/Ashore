using UnityEngine;

public class TutorialAcquireResourceStep : TutorialStep
{
    [Header("Item by Resource Name")]
    [Tooltip("Use the ItemData's asset name (Resources/Items/<name>) to decouple from a direct asset reference.")]
    public string itemResourceName;
    public int quantity = 1;

    public override bool IsComplete()
    {
        if (string.IsNullOrEmpty(itemResourceName) || Inventory.instance == null) return false;
        int have = 0;
        foreach (var inv in Inventory.instance.inventoryItems)
        {
            if (inv?.itemData == null) continue;
            if (inv.itemData.name == itemResourceName) have += inv.quantity;
        }
        return have >= Mathf.Max(1, quantity);
    }
}
