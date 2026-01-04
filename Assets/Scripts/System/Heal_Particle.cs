using UnityEngine;

public class Heal_Particle : MonoBehaviour
{
     [SerializeField] private int healAmount = 2;
    [SerializeField] private string playerTag = "Player";

    [Header("Floating Settings")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float heightAboveGround = 0.5f;
    [SerializeField] private AudioClip healClip;
    [SerializeField] private bool usePersistentAudio = true;

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
                if (healClip != null)
                {
                    if (usePersistentAudio)
                    {
                        // Disable visuals and interaction while sound plays
                        var rends = GetComponentsInChildren<Renderer>();
                        foreach (var r in rends)
                            r.enabled = false;

                        if (triggerCollider != null) triggerCollider.enabled = false;
                        if (groundCollider != null) groundCollider.enabled = false;

                        // Ensure an AudioSource exists on this object and play the clip
                        AudioSource src = GetComponent<AudioSource>();
                        if (src == null) src = gameObject.AddComponent<AudioSource>();
                        src.clip = healClip;
                        src.spatialBlend = 1f;
                        src.Play();

                        playerHealSystem.Heal(healAmount);
                        Destroy(gameObject, healClip.length + 0.1f);
                    }
                    else
                    {
                        AudioSource.PlayClipAtPoint(healClip, transform.position);
                        playerHealSystem.Heal(healAmount);
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Debug.LogWarning("Heal_Particle: no healClip assigned.");
                    playerHealSystem.Heal(healAmount);
                    Destroy(gameObject);
                }
            }
        }
    }

    public void SetHealAmount(int amount)
    {
        healAmount = amount;
    }
}