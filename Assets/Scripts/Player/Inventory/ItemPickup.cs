using UnityEngine;

/// <summary>
/// Drop-in pickup: add to an empty GameObject with a 2D trigger collider.
/// When the Player enters, it tries to add the configured ItemData to the Inventory
/// and destroys itself on success.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item To Grant")]
    [SerializeField] private ItemData item;
    [Min(1)]
    [SerializeField] private int quantity = 1;

    [Header("Pickup Behavior")]
    [Tooltip("Destroy the pickup GameObject if the item was added to the inventory.")]
    [SerializeField] private bool destroyOnPickup = true;
    [Tooltip("Optional: Only the Player (tag 'Player') can pick this up.")]
    [SerializeField] private bool requirePlayerTag = true;

    private void Reset()
    {
        // Make sure the collider is a trigger by default
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (requirePlayerTag && !other.CompareTag("Player")) return;
        TryGiveItem();
    }

    public void TryGiveItem()
    {
        if (item == null)
        {
            Debug.LogWarning($"ItemPickup on '{name}' has no ItemData assigned.");
            return;
        }
        if (Inventory.instance == null)
        {
            Debug.LogWarning("No Inventory.instance found in scene.");
            return;
        }
        bool added = Inventory.instance.Add(item, Mathf.Max(1, quantity));
        if (added)
        {
            if (destroyOnPickup) Destroy(gameObject);
        }
        else
        {
            // Inventory full or could not add; leave the pickup in the world
            Debug.Log("Inventory full or cannot add item right now.");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Show a small label/icon position where it will spawn; here we just draw a sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
#endif
}
