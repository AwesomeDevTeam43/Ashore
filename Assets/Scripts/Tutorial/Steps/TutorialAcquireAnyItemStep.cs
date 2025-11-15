using System.Collections.Generic;
using UnityEngine;

public class TutorialAcquireAnyItemStep : TutorialStep
{
    private Dictionary<ItemData, int> baseline;

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        CaptureBaseline();
        // Also listen to changes by refreshing baseline if inventory cleared mid-step (unlikely)
    }

    private void CaptureBaseline()
    {
        baseline = new Dictionary<ItemData, int>();
        if (Inventory.instance == null) return;
        foreach (var inv in Inventory.instance.inventoryItems)
        {
            if (inv?.itemData == null) continue;
            baseline[inv.itemData] = inv.quantity;
        }
    }

    public override bool IsComplete()
    {
        if (Inventory.instance == null) return false;
        // Complete when any item quantity increases beyond baseline (new pickup)
        foreach (var inv in Inventory.instance.inventoryItems)
        {
            if (inv?.itemData == null) continue;
            int prev = 0; baseline?.TryGetValue(inv.itemData, out prev);
            if (inv.quantity > prev) return true;
        }
        // Also complete if inventory went from 0 to >0
        if ((baseline == null || baseline.Count == 0) && Inventory.instance.inventoryItems.Count > 0)
            return true;
        return false;
    }
}
