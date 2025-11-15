using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager instance;

    private Player_Controller playerController;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;

        playerController = GetComponent<Player_Controller>();
    }

    // Equip an item from inventory: instantiate the equipment prefab as a child of the player
    // and register it as the currentEquipment so it can be used/thrown.
    public void EquipFromInventory(EquipmentData data)
    {
        if (data == null || data.equipmentPrefab == null)
        {
            Debug.LogWarning("EquipmentManager: no data or prefab to equip");
            return;
        }

        // Disallow equipping craftable equipment directly (must be crafted first)
        if (data.isCraftable)
        {
            Debug.Log("EquipmentManager: cannot equip craftable equipment before crafting: " + data.itemName);
            return;
        }

        // If playerController not found, try to get it
        if (playerController == null)
        {
            playerController = GetComponent<Player_Controller>();
            if (playerController == null)
            {
                Debug.LogError("EquipmentManager: Player_Controller not found on same GameObject");
                return;
            }
        }

        // Do not equip if something is already equipped
        if (playerController != null && playerController.CurrentEquipment != null)
        {
            Debug.Log("EquipmentManager: cannot equip, player already has equipment equipped.");
            return;
        }

        // Instantiate the equipment as a child of the player and keep it inactive (inventory holder)
        GameObject equipObj = Instantiate(data.equipmentPrefab, playerController.transform);
        equipObj.name = data.itemName + "_InventoryHolder";
        equipObj.SetActive(false);

        Equipment eq = equipObj.GetComponent<Equipment>();
        if (eq == null)
        {
            Debug.LogError("Equip prefab does not contain Equipment component");
            Destroy(equipObj);
            return;
        }

        // Register on player controller
        playerController.GetType(); // avoid unused warning
        playerController.SendMessage("SetCurrentEquipment", eq, SendMessageOptions.DontRequireReceiver);

        // Mark as equipped in code
        eq.isEquipped = true;
        eq.Equip();

        // Remove from inventory
        if (Inventory.instance != null)
        {
            Inventory.instance.Remove(data);
        }

        Debug.Log("Equipped from inventory: " + data.itemName);
    }
}
