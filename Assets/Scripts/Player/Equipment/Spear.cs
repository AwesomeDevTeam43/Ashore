using UnityEngine;

public class Spear : Equipment
{
    [SerializeField] private Transform throwpoint;
    [SerializeField] private float throwForce;
    private Collider2D pickupCollider; // Reference to the trigger collider for pickup

    void Awake()
    {
        pickupCollider = GetComponents<Collider2D>()[1]; // Adjust index if needed

    }

    public override void Equip()
    {
        Debug.Log("Equipped Spear");
        isEquipped = true;
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

        GameObject spearInstance = Instantiate(gameObject, throwpoint.position, throwpoint.rotation);
        Rigidbody2D rb = spearInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(throwpoint.right * throwForce, ForceMode2D.Impulse);
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Static;// Stops physics simulation
            }
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("Picked up spear");
            Destroy(gameObject);
        }
    }
}
