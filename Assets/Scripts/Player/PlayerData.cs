using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    // Player Stats & Position
    public int level;
    public int currentXp;
    public int maxXp;
    public int currentHealth;
    public int maxHealth;
    public float[] position; // Stored as {x, y, z}

    public Dictionary<string, object> worldData;

    // Inventory
    public List<string> inventoryItemNames;
    public List<int> inventoryItemQuantities;

    // Equipment
    public string equippedItemName;

    // Parameterless constructor for JSON deserialization
    public PlayerData() { }

    public PlayerData(Player_Controller player, XP_System xp, Player_Health health, Inventory inventory)
    {
        // Stats
        level = xp.CurrentLevel;
        currentXp = xp.CurrentXp;
        maxXp = xp.MaxXpPerLevel;
        currentHealth = player.GetComponent<HealthSystem>().CurrentHealth;
        maxHealth = player.GetComponent<HealthSystem>().MaxHealth;

        worldData = new Dictionary<string, object>();

        // Position
        Vector3 playerPos = player.transform.position;
        position = new float[] { playerPos.x, playerPos.y, playerPos.z };

        // Inventory
        inventoryItemNames = new List<string>();
        inventoryItemQuantities = new List<int>();
        foreach (var invItem in inventory.inventoryItems)
        {
            if (invItem.itemData != null)
            {
                inventoryItemNames.Add(invItem.itemData.itemName);
                inventoryItemQuantities.Add(invItem.quantity);
            }
        }

        // Equipment
        if (player.CurrentEquipment != null && player.CurrentEquipment.equipmentData != null)
        {
            equippedItemName = player.CurrentEquipment.equipmentData.itemName;
        }
        else
        {
            equippedItemName = null;
        }
    }
}
