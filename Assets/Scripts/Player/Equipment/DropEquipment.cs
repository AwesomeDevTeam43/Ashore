using UnityEngine;

public class DropEquipment : MonoBehaviour
{
    [SerializeField] private GameObject equipmentPrefab;
    private HealthSystem health;
    void Awake()
    {


        if (equipmentPrefab == null)
        {
            Debug.LogError("Equipment prefab is not assigned!");
        }
        health = GetComponent<HealthSystem>();
        
        health.OnHealthChanged += OnHealthChanged;
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= OnHealthChanged;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= OnHealthChanged;
        }
    }

    void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            Drop();
        }
    }

    private void Drop()
    {
        if (equipmentPrefab != null)
        {
            equipmentPrefab.GetComponent<Equipment>().isEquipped = false;
            Instantiate(equipmentPrefab, transform.position, Quaternion.identity);
        }
    }
}
