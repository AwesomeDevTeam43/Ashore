using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Equipment")]
public class EquipmentData : ItemData
{
    [Tooltip("Prefab that will be spawned when this equipment is used/equipped")]
    public GameObject equipmentPrefab;

}
