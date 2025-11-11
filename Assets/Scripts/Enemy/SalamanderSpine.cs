using UnityEngine;

// Salamander spine helper: supports two modes:
// - melee hitbox (isProjectile = false): collider should be a trigger and will damage on OnTriggerEnter2D
// - projectile (isProjectile = true): call LaunchAtTarget(playerTransform)
//    * direct hit on player -> damage & destroy
//    * misses and hits ground -> becomes a stuck trap (collider -> trigger) for stuckLifetime seconds and damages players who walk over it
[RequireComponent(typeof(Collider2D))]
public class SalamanderSpine : MonoBehaviour
{
    [Tooltip("If >0 this value will be used instead of the parent's stat damage.")]
    public int damageOverride = 0;

    [Tooltip("Optional: restrict which layers the spine's trigger will affect. Leave empty to affect any HealthSystem.")]
    public LayerMask targetMask;

    // owner reference (if this spine belongs to an Enemy_Salamander)
    private Enemy_Salamander ownerSalamander;
    private Rigidbody2D rb;

    [Header("Projectile (optional)")]
    [Tooltip("When true the spine can be launched as a projectile using LaunchAtTarget.")]
    public bool isProjectile = false;
    [Tooltip("Initial speed for the projectile.")]
    public float launchSpeed = 8f;
    [Tooltip("Upward bias added to aim to form a simple arc. 0 = straight shot.")]
    public float arcFactor = 0.25f;
    [Tooltip("How long a stuck spine remains on the ground before being destroyed (seconds).")]
    public float stuckLifetime = 6f;

    [Tooltip("Optional: when set, the projectile will only stick when colliding with layers in this mask. Leave empty to stick on any non-player collision.")]
    public LayerMask groundMask;

    // runtime state
    private bool launched = false;
    private bool stuckOnGround = false;
    private Collider2D cachedCollider;
    private Collider2D[] ownerColliders;

    private void Awake()
    {
        ownerSalamander = GetComponentInParent<Enemy_Salamander>();
        rb = GetComponent<Rigidbody2D>();
        cachedCollider = GetComponent<Collider2D>();
        if (cachedCollider == null)
        {
            // add a default circle collider if none exists
            cachedCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        // default trigger state: melee mode uses trigger; projectile uses collision while flying
        cachedCollider.isTrigger = !isProjectile;
    }

    /// <summary>
    /// Simple launch method called by the salamander to fire this spine at the target.
    /// Uses a straightforward aim with a vertical bias to approximate an arc.
    /// </summary>
    // Launch the spine at a target. Optional owner parameter ensures the projectile ignores that owner.
    public void LaunchAtTarget(Transform target, GameObject owner = null)
    {
        if (target == null) return;
        isProjectile = true;
        cachedCollider = cachedCollider ?? GetComponent<Collider2D>();
        if (cachedCollider == null) cachedCollider = gameObject.AddComponent<CircleCollider2D>();

        rb = rb ?? GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 1f;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // use collisions while flying
        cachedCollider.isTrigger = false;

        // aim towards target and add upward bias
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        Vector2 aim = (toTarget.normalized + Vector2.up * arcFactor).normalized;
        rb.linearVelocity = aim * launchSpeed;

        launched = true;

        // ignore collisions with owner colliders so it won't hurt its parent
        // If an explicit owner GameObject was passed, use that to set ownerSalamander and ignore its colliders.
        if (owner != null)
        {
            var explicitOwner = owner.GetComponent<Enemy_Salamander>();
            if (explicitOwner != null)
            {
                ownerSalamander = explicitOwner;
            }
            else
            {
                // also accept a generic parent which may hold the Enemy_Salamander component
                ownerSalamander = owner.GetComponentInChildren<Enemy_Salamander>();
            }
        }

        if (ownerSalamander != null && cachedCollider != null)
        {
            ownerColliders = ownerSalamander.GetComponentsInChildren<Collider2D>(true);
            if (ownerColliders != null)
            {
                foreach (var oc in ownerColliders)
                {
                    if (oc != null) Physics2D.IgnoreCollision(cachedCollider, oc, true);
                }
            }
        }

        // Detach from any parent to avoid being inside the owner's collider at launch
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only flying projectiles use collisions
        if (!isProjectile) return;
        if (collision == null) return;
        Collider2D other = collision.collider;
        if (other == null) return;

        // ignore collisions with owner
        if (ownerSalamander != null && other.GetComponentInParent<Enemy_Salamander>() == ownerSalamander) return;

        // If we hit a HealthSystem (player), apply damage and destroy
        HealthSystem hs = other.GetComponent<HealthSystem>() ?? other.GetComponentInParent<HealthSystem>();
        if (hs != null)
        {
            ApplyDamage(hs);
            Destroy(gameObject);
            return;
        }

        // Otherwise decide whether to stick to ground. If groundMask is set, only stick on those layers.
        bool shouldStick = (groundMask.value == 0) || ((groundMask.value & (1 << other.gameObject.layer)) != 0);
        if (shouldStick)
        {
            StickToGround(collision.contacts != null && collision.contacts.Length > 0 ? collision.contacts[0].point : (Vector2)transform.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // If a target mask is set, require it
        if (targetMask.value != 0 && (targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        // ignore owner hits
        if (ownerSalamander != null && other.GetComponentInParent<Enemy_Salamander>() == ownerSalamander) return;

        // Only respond to triggers when in melee mode or when stuck as a trap
        if (isProjectile && !stuckOnGround) return;

        HealthSystem hs = other.GetComponent<HealthSystem>() ?? other.GetComponentInParent<HealthSystem>();
        if (hs == null) return;

        ApplyDamage(hs);

        // If this is a flying projectile that happened to trigger directly, destroy it
        if (isProjectile && launched && !stuckOnGround)
        {
            Destroy(gameObject);
        }
    }

    private void StickToGround(Vector2 hitPoint)
    {
        if (stuckOnGround) return;
        stuckOnGround = true;
        launched = false;

        // stop physics
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // make the collider a trigger so it acts like a trap
        cachedCollider = cachedCollider ?? GetComponent<Collider2D>();
        if (cachedCollider != null) cachedCollider.isTrigger = true;

        // snap to the hit point for nicer visuals
        transform.position = hitPoint;

        if (stuckLifetime > 0f) Destroy(gameObject, stuckLifetime);
    }

    private void ApplyDamage(HealthSystem hs)
    {
        if (hs == null) return;
        int dmg = damageOverride;
        if (dmg <= 0 && ownerSalamander != null)
        {
            var typed = ownerSalamander.typedStats as Salamander_Stats;
            if (typed != null) dmg = typed.biteDamage;
        }
        if (dmg <= 0) dmg = 1;

        hs.TakeDamage(dmg);
    }
}
