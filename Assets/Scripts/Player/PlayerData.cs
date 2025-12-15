using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class PlayerData
{
    // Total play time in seconds
    public float playTime;
    // Player Stats & Position
    public int level;
    public int currentXp;
    public int maxXp;
    public int currentHealth;
    public int maxHealth;
    public float[] position; // Stored as {x, y, z}
    public string sceneName;

    public Dictionary<string, object> worldData;

    // Inventory
    public List<string> inventoryItemNames;
    // Resource asset names (preferred for load). Falls back to inventoryItemNames if null/empty.
    public List<string> inventoryResourceNames;
    public List<int> inventoryItemQuantities;

    // Equipment
    public string equippedItemName;
    public string mainWeaponType; // "Melee" or "Ranged" (enum name)

    // Parameterless constructor for JSON deserialization
    public PlayerData() { }

    public PlayerData(Player_Controller player, XP_System xp, Player_Health health, Inventory inventory, float playTimeSeconds = 0f)
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

        // Scene
        sceneName = SceneManager.GetActiveScene().name;

        // Inventory
        inventoryItemNames = new List<string>();
        inventoryResourceNames = new List<string>();
        inventoryItemQuantities = new List<int>();
        foreach (var invItem in inventory.inventoryItems)
        {
            if (invItem.itemData != null)
            {
                inventoryItemNames.Add(invItem.itemData.itemName);
                // Save the asset/resource name so Resources.Load can find it reliably
                inventoryResourceNames.Add(invItem.itemData.name);
                inventoryItemQuantities.Add(invItem.quantity);
            }
        }

        // Equipment
        if (player.CurrentEquipment != null && player.CurrentEquipment.equipmentData != null)
        {
            equippedItemName = player.CurrentEquipment.equipmentData.itemName;
            // Save asset/resource name for reliable lookup
            equippedResourceName = player.CurrentEquipment.equipmentData.name;
        }
        else
        {
            equippedItemName = null;
            equippedResourceName = null;
        }

        // Main weapon type (persist player's selected combat mode)
        mainWeaponType = player.CurrentMainWeapon.ToString();

        // Play time
        playTime = playTimeSeconds;
    }

    // Preferred key for equipment resource lookup
    public string equippedResourceName;
}
