using UnityEngine;

public class TutorialCraftItemStep : TutorialStep
{
    [Header("Craft Requirement")]
    [Tooltip("Recipe or resulting item to track. If the recipe's ItemData has a craftResult, that result will be tracked.")]
    public ItemData itemToCraft;
    [Tooltip("How many more of the resulting item need to be obtained during this step.")]
    public int quantityIncrease = 1;

    private int baseline;
    private ItemData resultItem;

    private void Reset()
    {
        freezePlayerMovement = false;
        dimScreen = true; // dim is globally disabled in overlay now
    }

    public override void Begin(TutorialManager mgr)
    {
        base.Begin(mgr);
        resultItem = itemToCraft != null && itemToCraft.craftResult != null ? itemToCraft.craftResult : itemToCraft;
        if (Inventory.instance != null && resultItem != null)
            baseline = Mathf.Max(0, Inventory.instance.GetItemQuantity(resultItem));
        else
            baseline = 0;
    }

    public override bool IsComplete()
    {
        if (Inventory.instance == null || resultItem == null) return false;
        int current = Inventory.instance.GetItemQuantity(resultItem);
        return current >= baseline + Mathf.Max(1, quantityIncrease);
    }
}
