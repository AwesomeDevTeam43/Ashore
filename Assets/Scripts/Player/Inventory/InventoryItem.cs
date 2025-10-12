using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int quantity;

    public InventoryItem(ItemData item, int qty = 1)
    {
        itemData = item;
        quantity = qty;
    }

    public bool CanStack(ItemData item)
    {
        return itemData == item && itemData.isStackable && quantity < itemData.maxStackSize;
    }

    public int AddToStack(int amount)
    {
        int canAdd = Mathf.Min(amount, itemData.maxStackSize - quantity);
        quantity += canAdd;
        return amount - canAdd; // Return remaining amount that couldn't be added
    }
}