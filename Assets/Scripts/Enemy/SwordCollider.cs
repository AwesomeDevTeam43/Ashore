using UnityEngine;

// Attach this to the sword child with a Trigger Collider2D. It will only apply damage
// while the parent boss reports an attack in progress. Designed to work with Enemy_RuinsBoss.
public class SwordCollider : MonoBehaviour
{
    [Tooltip("Optional damage override. If zero, the owner's stat damage will be used.")]
    public int damage = 0;

    [Tooltip("Optional: limit which layers can be damaged (Player layer etc). If empty, any HealthSystem will be used.")]
    public LayerMask targetMask;

    private Enemy_RuinsBoss owner;

    private void Awake()
    {
        owner = GetComponentInParent<Enemy_RuinsBoss>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || owner == null) return;

        // If targetMask is set, require it
        if (targetMask.value != 0 && (targetMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (!owner.IsAttackInProgress()) return; // only hurt during attack

        // Find a HealthSystem on the collider or parent
        HealthSystem hs = other.GetComponent<HealthSystem>() ?? other.GetComponentInParent<HealthSystem>();
        if (hs == null) return;

        // Use owner's stats damage if this collider hasn't been configured with an override
        int dmg = damage > 0 ? damage : (owner != null && owner.typedStats != null ? owner.typedStats.attackDamage : 1);

        // Apply damage via the owner helper so knockback etc use the same logic
        // The owner expects a Transform for DoMeleeHit; pass nearest transform that owns the HealthSystem
        Transform targetTransform = hs.transform;

        // Directly call owner's DoMeleeHit which will apply damage and knockback appropriately
        owner.DoMeleeHit(targetTransform);
    }
}
