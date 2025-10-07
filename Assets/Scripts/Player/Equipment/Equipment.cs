using UnityEngine;

public abstract class Equipment : MonoBehaviour
{
    public string equipmentName;
    public string description;
    public bool isEquipped;

    public abstract void Equip();
    public abstract void Unequip();

    public abstract void Use();
}
