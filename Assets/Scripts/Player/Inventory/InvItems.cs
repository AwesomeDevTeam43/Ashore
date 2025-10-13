using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon = null;
    public string description = "";

    [Header("Gameplay")]
    public bool isUsable = false;
    public bool isCraftable = false;

    [System.Serializable]
    public class CraftIngredient
    {
        public ItemData material;
        public int amount = 1;
    }

    // When isCraftable == true you'll define the required materials here
    public List<CraftIngredient> craftIngredients = new List<CraftIngredient>();

    [Header("Crafting Result")]
    [Tooltip("If set, this ItemData will be given when this recipe is crafted. If null, the recipe's ItemData is added.")]
    public ItemData craftResult;

    [Header("Stacking")]
    public int maxStackSize = 50;
    public bool isStackable = false;
    
     public bool CanCraft(System.Func<ItemData, int> getPlayerCount)
    {
        if (!isCraftable) return false;
        if (getPlayerCount == null) return false;

        foreach (var ing in craftIngredients)
        {
            if (ing == null || ing.material == null) return false;
            if (getPlayerCount(ing.material) < Mathf.Max(1, ing.amount)) return false;
        }
        return true;
    }
}