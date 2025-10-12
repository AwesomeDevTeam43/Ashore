using UnityEngine;

public class Materials : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private string playerTag = "Player";

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
        }

        // Get or create the ground collision collider
        groundCollider = GetComponent<CircleCollider2D>();
        if (groundCollider == null)
        {
            groundCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        groundCollider.isTrigger = false;

        // Create the trigger collider for player interaction
        triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.6f;
    }

    void Update()
    {
        if (hasLanded)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        string layerName = LayerMask.LayerToName(collision.gameObject.layer);

        if (layerName == "Ground" || layerName == "MovingPlatform")
        {
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
        else if (item == null)
        {
            Debug.LogWarning($"Materials on {gameObject.name}: No ItemData assigned!");
        }
        else if (item.icon == null)
        {
            Debug.LogWarning($"Materials on {gameObject.name}: ItemData '{item.itemName}' has no icon!");
        }
    }

    public ItemData GetItemData()
    {
        return item;
    }
}