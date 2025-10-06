    using UnityEngine;

public interface IEquipment
{
    string Id { get; }

    string Name { get; }

    string Description { get; }

    void OnEquip();

    void OnUnequip();

    void OnUse();

}
