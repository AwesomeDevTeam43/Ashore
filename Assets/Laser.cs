using UnityEngine;
using System;

public class Laser : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of HP to remove when the laser hits the player")]
    public int damageAmount = 1;

    [Tooltip("Layers that can be damaged by this laser (set to Player layer)")]
    public LayerMask targetLayers = 1 << 8; // default to layer 8 (change as needed)

    [Tooltip("If true, this laser will only damage once per 'hasDamaged' cycle. Call ResetDamageFlag() to allow damage again.")]
    public bool oneShotPerCycle = true;

    private bool hasDamaged = false;
    private Collider2D areaCollider;

    // Optional callback the spawner can set to be notified when this laser finishes and is destroyed.
    public Action onFinished;

    void Awake()
    {
        hasDamaged = false;
        areaCollider = GetComponent<Collider2D>();
        if (areaCollider == null)
        {
            Debug.LogWarning($"Laser: no Collider2D found on '{gameObject.name}'. The animation event check requires a Collider2D to define the hit area.");
        }
    }

    // Public method you can call from an Animation Event (example name: "DealDamageNow")
    // This checks the attached 2D collider for overlapping colliders on the configured target layers
    // and applies damage via a HealthSystem component if present.
    public void DealDamageNow()
    {
        if (oneShotPerCycle && hasDamaged)
        {
            // already damaged this cycle
            return;
        }

        if (areaCollider == null)
        {
            Debug.LogWarning("Laser.DealDamageNow: no Collider2D to check overlaps.");
            return;
        }

        // Use OverlapCollider with a ContactFilter to respect targetLayers
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayers);
        filter.useLayerMask = true;

    Collider2D[] results = new Collider2D[8];
    int count = areaCollider.Overlap(filter, results);
    if (count == 0)
    {
        Debug.Log($"Laser.DealDamageNow: no overlaps found for '{gameObject.name}' (layerMask={targetLayers}).");
    }

        for (int i = 0; i < count; i++)
        {
            Collider2D col = results[i];
            if (col == null) continue;

            Debug.Log($"Laser.DealDamageNow: overlap found: {col.gameObject.name} (layer {col.gameObject.layer})");

            // Try to find a HealthSystem on the collider or its parents
            HealthSystem hs = col.GetComponent<HealthSystem>();
            if (hs == null)
            {
                hs = col.GetComponentInParent<HealthSystem>();
            }

            if (hs != null)
            {
                hs.TakeDamage(damageAmount);
                Debug.Log($"Laser: dealt {damageAmount} damage to '{hs.gameObject.name}' via '{gameObject.name}'");
                hasDamaged = true;
                // Defensive: if this Laser is a child of a Boss, ensure the boss gating is enforced
                Boss parentBoss = GetComponentInParent<Boss>();
                if (parentBoss != null)
                {
                    parentBoss.canUseLaser = false;
                }
                // If you want the laser to damage multiple targets in the same frame, don't break here.
                // We continue so we can hit multiple players if present.
            }
        }
    }

    // Reset the hasDamaged flag so the laser can damage again (call this from animation end or other code)
    public void ResetDamageFlag()
    {
        hasDamaged = false;
    }

    // Call from an Animation Event on the laser prefab (last frame) to signal it's finished
    // and destroy its GameObject. This will also invoke onFinished if set.
    public void FinishAndDestroy()
    {
        try
        {
            onFinished?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Laser.FinishAndDestroy: exception invoking onFinished: {ex}");
        }

        Destroy(gameObject);
    }

    // Optional simple helper with a custom name for animation events
    public void DealDamageEvent() => DealDamageNow();


}
