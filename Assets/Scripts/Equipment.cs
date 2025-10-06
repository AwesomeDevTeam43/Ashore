using UnityEngine;

public abstract class Equipment : MonoBehaviour, IEquipment
{
    [SerializeField] private string id;
    [SerializeField] private string equipmentName;
    [SerializeField] private string description;

    public string Id => id;
    public string Name => equipmentName;
    public string Description => description;

            public virtual void OnEquip()
        {
            Debug.Log($"{Name} equipped!");
            gameObject.SetActive(true);
        }
        
        public virtual void OnUnequip()
        {
            Debug.Log($"{Name} unequipped!");
            gameObject.SetActive(false);
        }
        
    public abstract void OnUse();
}
