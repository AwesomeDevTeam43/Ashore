using UnityEngine;
using System.Collections;

public class Spear : Equipment
{
    [SerializeField] private Transform throwpoint;
    [SerializeField] private float throwForce;
    [SerializeField] private float pickupDelay = 0.5f;
    private Collider2D physicsCollider;
    private Collider2D triggerCollider;
    private bool canBePickedUp = false;

    void Awake()
    {
        // Get both colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger)
                triggerCollider = col;
            else
                physicsCollider = col;
        }
        
        // If this is a thrown instance (not in player's inventory)
        if (!isEquipped && throwpoint == null)
        {
            canBePickedUp = false;
        }
    }

    public override void Equip()
    {
        Debug.Log("Equipped Spear");
        isEquipped = true;
        canBePickedUp = false;
    }

    public override void Unequip()
    {
        Debug.Log("Unequipped Spear");
        isEquipped = false;
    }

    public override void Use()
    {
        if (isEquipped)
        {
            ThrowSpear();
        }
    }

    private void ThrowSpear()
    {
        throwpoint = GameObject.FindWithTag("Shootpoint").transform;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        Vector2 throwDirection = Vector2.right; // Default
        
        // Get the input handler to read current input direction
        Player_InputHandler inputHandler = player.GetComponent<Player_InputHandler>();
        if (inputHandler != null)
        {
            Vector2 inputDir = inputHandler.MovementInput;
            Debug.Log($"Current movement input: {inputDir}");
            
            if (inputDir != Vector2.zero)
            {
                // Prioritize vertical input for up/down throws
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
                // No input, use player facing direction
                Player_Movement playerMovement = player.GetComponent<Player_Movement>();
                if (playerMovement != null)
                {
                    throwDirection = playerMovement.IsFacingRight ? Vector2.right : Vector2.left;
                }
            }
        }
        
        Debug.Log($"FINAL throw direction: {throwDirection}");

        GameObject spearInstance = Instantiate(gameObject, throwpoint.position, Quaternion.identity);
        spearInstance.SetActive(true);
        
        // Rotate and flip the spear based on throw direction
        SpriteRenderer spearSprite = spearInstance.GetComponent<SpriteRenderer>();
        if (spearSprite != null)
        {
            if (throwDirection == Vector2.right)
            {
                spearInstance.transform.rotation = Quaternion.Euler(0, 0, 0);
                spearSprite.flipX = false;
                Debug.Log("Spear set to RIGHT");
            }
            else if (throwDirection == Vector2.left)
            {
                spearInstance.transform.rotation = Quaternion.Euler(0, 0, 0);
                spearSprite.flipX = true;
                Debug.Log("Spear set to LEFT");
            }
            else if (throwDirection == Vector2.up)
            {
                spearInstance.transform.rotation = Quaternion.Euler(0, 0, 90);
                spearSprite.flipX = false;
                Debug.Log("Spear set to UP");
            }
            else if (throwDirection == Vector2.down)
            {
                spearInstance.transform.rotation = Quaternion.Euler(0, 0, -90);
                spearSprite.flipX = false;
                Debug.Log("Spear set to DOWN");
            }
        }
        
        Spear spearScript = spearInstance.GetComponent<Spear>();
        if (spearScript != null)
        {
            spearScript.isEquipped = false;
            spearScript.hasLanded = false;
            spearScript.canBePickedUp = false;
            spearScript.throwpoint = null;
            
            if (spearScript.physicsCollider != null && player != null)
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    Physics2D.IgnoreCollision(spearScript.physicsCollider, playerCollider, true);
                }
            }
            
            if (spearScript.triggerCollider != null)
            {
                spearScript.triggerCollider.enabled = true;
            }
        }

        Rigidbody2D rb = spearInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.AddForce(throwDirection * throwForce, ForceMode2D.Impulse);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Debug.Log("Spear hit ground");
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
                Debug.Log("Enemy hit by spear");
            }
        }
        if (hasLanded && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Physics2D.IgnoreCollision(physicsCollider, collision.collider);
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
        
        Debug.Log("Spear reset for inventory");
    }

    private IEnumerator EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(pickupDelay);
        hasLanded = true;
        canBePickedUp = true;
        Debug.Log("Spear can now be picked up");
    }

    public bool CanBePickedUp()
    {
        return hasLanded && canBePickedUp && !isEquipped;
    }
}