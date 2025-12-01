using UnityEngine;

public class Materials : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] [Range(0.3f, 0.99f)] private float minGroundNormalY = 0.8f;

    [Header("Floating Settings")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float heightAboveGround = 0.5f;

    private Vector3 startPosition;
    private float timeOffset;
    private bool hasLanded = false;
    private Rigidbody2D rb;
    private CircleCollider2D groundCollider;
    private CircleCollider2D triggerCollider;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        rb = GetComponent<Rigidbody2D>();

        // Get or create SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Set sprite from ItemData
        UpdateSprite();

        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Get or create the ground collision collider
        groundCollider = GetComponent<CircleCollider2D>();
        if (groundCollider == null)
        {
            groundCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        groundCollider.isTrigger = false;
        groundCollider.radius = 0.35f;

        // Create the trigger collider for player interaction
        triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.6f;

        IgnorePlayerCollision();
    }

    void Update()
    {
        if (hasLanded)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void IgnorePlayerCollision()
    {
        if (groundCollider == null) return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        var playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (var playerCollider in playerColliders)
        {
            if (playerCollider == null) continue;
            Physics2D.IgnoreCollision(groundCollider, playerCollider, true);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;

        bool isGround = (groundLayers.value & (1 << collision.gameObject.layer)) != 0;
        if (!isGround) return;

        if (collision.relativeVelocity.y >= 0f)
            return;

        bool landedOnTop = false;
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y >= minGroundNormalY && Mathf.Abs(contact.normal.x) <= (1f - minGroundNormalY))
            {
                landedOnTop = true;
                break;
            }
        }

        if (!landedOnTop)
            return;

        hasLanded = true;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable the ground collider and enable only trigger
        if (groundCollider != null)
        {
            groundCollider.enabled = false;
        }

        startPosition = transform.position;
        startPosition.y += heightAboveGround;
        transform.position = startPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // Add the item to player's inventory
            Inventory playerInventory = Inventory.instance;
            if (playerInventory != null && item != null)
            {
                bool wasPickedUp = playerInventory.Add(item);
                if (wasPickedUp)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    // Method to set ItemData (called by Drop_Materials)
    public void SetItemData(ItemData newItem)
    {
        item = newItem;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer != null && item != null && item.icon != null)
        {
            spriteRenderer.sprite = item.icon;
        }
    }

    public ItemData GetItemData()
    {
        return item;
    }
}