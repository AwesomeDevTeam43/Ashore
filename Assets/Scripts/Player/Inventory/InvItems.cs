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

    [Header("Stacking")]
    public int maxStackSize = 50;
    public bool isStackable = false;
}