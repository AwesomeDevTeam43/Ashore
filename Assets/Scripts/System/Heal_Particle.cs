using UnityEngine;

public class Heal_Particle : MonoBehaviour
{
     [SerializeField] private int healAmount = 2;
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
    

    private void Start()
    {
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        rb = GetComponent<Rigidbody2D>();

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

    private void Update()
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
            HealthSystem playerHealSystem = other.GetComponent<HealthSystem>();

            if (playerHealSystem != null && playerHealSystem.CurrentHealth < playerHealSystem.MaxHealth)
            {
                playerHealSystem.Heal(healAmount);
                Destroy(gameObject);
            }
        }
    }
}