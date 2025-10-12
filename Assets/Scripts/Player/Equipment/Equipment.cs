using UnityEngine;

public abstract class Equipment : MonoBehaviour
{
    public string equipmentName;
    public string description;
    public bool isEquipped = false;

    public bool hasLanded = false;

    public EquipmentData equipmentData;
    
    // If the equipmentData has an icon, copy it to the GameObject's SpriteRenderer
    // so the world object visually matches the inventory asset.
    private void Awake()
    {
        SyncIconToSprite();
    }
    
#if UNITY_EDITOR
    // Keep the prefab in editor in sync when changing the referenced EquipmentData
    private void OnValidate()
    {
        SyncIconToSprite();
    }
#endif
    
    private void SyncIconToSprite()
    {
        if (equipmentData == null || equipmentData.icon == null) return;
        
        // Prefer SpriteRenderer on the same GameObject, otherwise search children
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (sr != null)
        {
            sr.sprite = equipmentData.icon;
        }
    }

    public abstract void Equip();
    public abstract void Unequip();

    public abstract void Use();
}
