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

    public List<ItemData> items = new List<ItemData>();

    public bool Add(ItemData item)
    {
        if (items.Count >= space)
        {
            Debug.Log("Not enough room in inventory.");
            return false;
        }

        items.Add(item);

        // Trigger the callback to update the UI
        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
        return true;
    }

    public void Remove(ItemData item)
    {
        items.Remove(item);

        // Trigger the callback to update the UI
        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }
}