using UnityEngine;

public class XP_Particle : MonoBehaviour
{
    [SerializeField] private int xpValue = 2;
    [SerializeField] private string playerTag = "Player";

    [Header("Floating Settings")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float heightAboveGround = 0.5f;

    private Vector3 startPosition;
    private float timeOffset;
    private bool hasLanded = false;
    private Rigidbody2D rb;

    private void Start()
    {
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        CircleCollider2D existingCollider = GetComponent<CircleCollider2D>();
        if (existingCollider != null)
        {
            existingCollider.isTrigger = false;
        }

        CircleCollider2D triggerCollider = gameObject.AddComponent<CircleCollider2D>();
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
            
            startPosition = transform.position;
            startPosition.y += heightAboveGround;
            transform.position = startPosition;
        }
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