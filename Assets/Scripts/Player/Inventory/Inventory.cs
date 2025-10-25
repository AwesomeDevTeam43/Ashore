using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory found!");
            return;
        }
        instance = this;
    }
    #endregion

    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public int space = 20; // Amount of inventory slots

    // Changed to use InventoryItem instead of ItemData directly
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    
    // Keep this for backward compatibility with UI
    public List<ItemData> items = new List<ItemData>();

    public void Clear()
    {
        inventoryItems.Clear();
        items.Clear();
        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }

    public bool Add(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        int remainingQuantity = quantity;

        // If item is stackable, try to stack with existing items first
        if (item.isStackable)
        {
            for (int i = 0; i < inventoryItems.Count && remainingQuantity > 0; i++)
            {
                if (inventoryItems[i].CanStack(item))
                {
                    remainingQuantity = inventoryItems[i].AddToStack(remainingQuantity);
                }
            }
        }

        // If there's still quantity remaining, create new stacks
        while (remainingQuantity > 0)
        {
            if (inventoryItems.Count >= space)
            {
                Debug.Log("Not enough room in inventory.");
                return false;
            }

            int stackSize = item.isStackable ? Mathf.Min(remainingQuantity, item.maxStackSize) : 1;
            inventoryItems.Add(new InventoryItem(item, stackSize));
            remainingQuantity -= stackSize;
        }

        // Update the items list for UI compatibility
        UpdateItemsList();

        // Trigger the callback to update the UI
        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
        
        return true;
    }

    public void Remove(ItemData item, int quantity = 1)
    {
        int remainingToRemove = quantity;

        for (int i = inventoryItems.Count - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            if (inventoryItems[i].itemData == item)
            {
                int toRemove = Mathf.Min(remainingToRemove, inventoryItems[i].quantity);
                inventoryItems[i].quantity -= toRemove;
                remainingToRemove -= toRemove;

                if (inventoryItems[i].quantity <= 0)
                {
                    inventoryItems.RemoveAt(i);
                }
            }
        }

        // Update the items list for UI compatibility
        UpdateItemsList();

        // Trigger the callback to update the UI
        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }

    // Backward compatibility method - removes one item
    public void Remove(ItemData item)
    {
        Remove(item, 1);
    }

    // Update the items list for UI compatibility
    private void UpdateItemsList()
    {
        items.Clear();
        foreach (var inventoryItem in inventoryItems)
        {
            items.Add(inventoryItem.itemData);
        }
    }

    // Get quantity of a specific item
    public int GetItemQuantity(ItemData item)
    {
        int total = 0;
        foreach (var inventoryItem in inventoryItems)
        {
            if (inventoryItem.itemData == item)
            {
                total += inventoryItem.quantity;
            }
        }
        return total;
    }

    // Get the InventoryItem for a specific slot (for UI)
    public InventoryItem GetInventoryItem(int index)
    {
        if (index >= 0 && index < inventoryItems.Count)
        {
            return inventoryItems[index];
        }
        return null;
    }
}