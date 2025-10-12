using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon = null;
    public string description = "";
    [Header("Gameplay")]
    [Tooltip("If true, this item can be used (consumable)")]
    public bool isUsable = false;
    [Tooltip("If true, this item can be used as a crafting ingredient or opened in craft UI")]
    public bool isCraftable = false;
}