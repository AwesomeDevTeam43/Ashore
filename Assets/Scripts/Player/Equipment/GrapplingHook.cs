using UnityEngine;

public class GrapplingHook : Equipment
{
    [SerializeField] private Transform throwpoint;
    [SerializeField] private GameObject grappleProjectilePrefab;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float pickupDelay = 0.5f;
    private Collider2D physicsCollider;
    private Collider2D triggerCollider;
    private bool canBePickedUp = false;

    public override void Equip()
    {
        isEquipped = true;
        Debug.Log("GrapplingHook equipped");
    }

    public override void Unequip()
    {
        isEquipped = false;
        Debug.Log("GrapplingHook unequipped");
    }

    public override void Use()
    {
        if (!isEquipped) return;
        ThrowHook();
    }

    private void ThrowHook()
    {
        throwpoint = GameObject.FindWithTag("Shootpoint")?.transform;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (throwpoint == null || player == null || grappleProjectilePrefab == null)
        {
            Debug.LogWarning("GrapplingHook: missing throwpoint/player/prefab");
            return;
        }

        Vector2 throwDirection = Vector2.right;

        Player_InputHandler inputHandler = player.GetComponent<Player_InputHandler>();
        if (inputHandler != null)
        {
            Vector2 inputDir = inputHandler.MovementInput;
            if (inputDir != Vector2.zero)
            {
                if (Mathf.Abs(inputDir.y) > Mathf.Abs(inputDir.x))
                {
                    throwDirection = inputDir.y > 0 ? Vector2.up : Vector2.down;
                }
                else
                {
                    throwDirection = inputDir.x > 0 ? Vector2.right : Vector2.left;
                }
            }
            else
            {
                Player_Movement pm = player.GetComponent<Player_Movement>();
                if (pm != null)
                    throwDirection = pm.IsFacingRight ? Vector2.right : Vector2.left;
            }
        }

        GameObject proj = Instantiate(grappleProjectilePrefab, throwpoint.position, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.AddForce(throwDirection.normalized * throwForce, ForceMode2D.Impulse);
        }

        // Ensure the projectile does not collide with the player that fired it
        Collider2D projCol = proj.GetComponent<Collider2D>();
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (projCol != null && playerCol != null)
        {
            Physics2D.IgnoreCollision(projCol, playerCol, true);
        }

        // Consumed when thrown
        isEquipped = false;

        // If this equipment instance exists in the world as a thrown object, prepare it for physics pickup behavior
        // (Match Spear behavior: make it dynamic and allow pickup after a short delay when it lands)
        // Note: in this design the actual thrown object is the projectile; the equipment GameObject in inventory
        // is left as-is. If you instantiate the equipment itself as the thrown object, ensure colliders/Rigidbody are present.
    }

    void Awake()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols)
        {
            if (c.isTrigger) triggerCollider = c;
            else physicsCollider = c;
        }
        if (!isEquipped)
        {
            canBePickedUp = false;
        }
    }

    public void ResetForInventory()
    {
        hasLanded = false;
        canBePickedUp = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        Debug.Log("GrapplingHook reset for inventory");
    }

    private System.Collections.IEnumerator EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(pickupDelay);
        hasLanded = true;
        canBePickedUp = true;
        Debug.Log("GrapplingHook can now be picked up");
    }

    public bool CanBePickedUp()
    {
        return hasLanded && canBePickedUp;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Static;
            }
            StartCoroutine(EnablePickupAfterDelay());
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") && !hasLanded)
        {
            HealthSystem enemyHealth = collision.gameObject.GetComponent<HealthSystem>();
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (enemyHealth != null && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                enemyHealth.TakeDamage(6);
                Debug.Log("Enemy hit by grappling hook (projectile)");
            }
        }

        if (hasLanded && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Physics2D.IgnoreCollision(physicsCollider, collision.collider);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("FallLevel"))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                transform.position = player.transform.position;
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
                hasLanded = true;
                canBePickedUp = true;
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = true;
                }
                Debug.Log("GrapplingHook teleported back to player after falling.");
            }
        }
    }
}
