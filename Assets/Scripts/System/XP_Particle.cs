using UnityEngine;

public class XP_Particle : MonoBehaviour
{
    [SerializeField] private int xpValue = 2;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float fallGravityScale = 2f;
    [SerializeField] private float physicsColliderRadius = 0.35f;
    [SerializeField] private float triggerRadius = 0.6f;
    [SerializeField] [Range(0.3f, 0.99f)] private float minGroundNormalY = 0.8f;
    [Header("Floating Settings")]
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float heightAboveGround = 0.35f;

    private Rigidbody2D rb;
    private Collider2D physicsCollider;
    private Collider2D triggerCollider;
    private bool hasLanded;
    private Vector3 startPosition;
    private float timeOffset;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = Mathf.Max(0.1f, fallGravityScale);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        timeOffset = Random.Range(0f, 2f * Mathf.PI);

        EnsureColliders();
    }

    private void Start()
    {
        IgnorePlayerCollision();
    }

    private void Update()
    {
        if (hasLanded)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void EnsureColliders()
    {
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (col == null) continue;
            if (col.isTrigger && triggerCollider == null)
            {
                triggerCollider = col;
            }
            else if (!col.isTrigger && physicsCollider == null)
            {
                physicsCollider = col;
            }
        }

        if (physicsCollider == null)
        {
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = physicsColliderRadius;
            circle.isTrigger = false;
            physicsCollider = circle;
        }
        else
        {
            physicsCollider.isTrigger = false;
            if (physicsCollider is CircleCollider2D physCircle && physCircle.radius <= 0f)
            {
                physCircle.radius = physicsColliderRadius;
            }
        }

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        triggerCollider.isTrigger = true;
        if (triggerCollider is CircleCollider2D triggerCircle)
        {
            triggerCircle.radius = Mathf.Max(triggerCircle.radius, triggerRadius);
        }
    }

    private void IgnorePlayerCollision()
    {
        if (physicsCollider == null) return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        var playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (var playerCollider in playerColliders)
        {
            if (playerCollider == null) continue;
            Physics2D.IgnoreCollision(physicsCollider, playerCollider, true);
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
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable physics collider and keep only trigger
        if (physicsCollider != null)
        {
            physicsCollider.enabled = false;
        }

        // Set floating position
        startPosition = transform.position;
        startPosition.y += heightAboveGround;
        transform.position = startPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    { 
        if (other.CompareTag(playerTag))
        {   
            XP_System playerXpSystem = other.GetComponent<XP_System>();

            if (playerXpSystem != null)
            {
                playerXpSystem.IncreaseXP(xpValue);
                Destroy(gameObject);
            }
        }
    }
}