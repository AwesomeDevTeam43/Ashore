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

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem healParticlePrefab;
    [SerializeField] private string particleSortingLayer = "Default";
    [SerializeField] private int particleSortingOrder = 10;

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
        if (!other.CompareTag(playerTag)) return;

        HealthSystem playerHealSystem = other.GetComponent<HealthSystem>();
        if (playerHealSystem == null || playerHealSystem.CurrentHealth >= playerHealSystem.MaxHealth)
            return;

        // Play particle effect as a child of the player (so it appears on the player)
        ParticleSystem spawnedParticle = null;
        if (healParticlePrefab != null)
        {
            // Parent to the player so it follows them visually
            spawnedParticle = Instantiate(healParticlePrefab, other.transform);
            spawnedParticle.transform.localPosition = Vector3.zero;

            // Set sorting layer and order on all renderers in the hierarchy
            var renderers = spawnedParticle.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                rend.sortingLayerName = particleSortingLayer;
                rend.sortingOrder = particleSortingOrder;
            }
            spawnedParticle.Play();
            Destroy(spawnedParticle.gameObject, 1.0f);
        }

        // Play sound robustly at the player's position
        if (healClip != null)
        {
            AudioSource.PlayClipAtPoint(healClip, other.transform.position);
        }
        else
        {
            Debug.LogWarning("Heal_Particle: no healClip assigned.");
        }

        // Heal the player
        playerHealSystem.Heal(healAmount);

        // Flash the player green for a short moment (run on player's HealthSystem)
        try
        {
            playerHealSystem.StartCoroutine(playerHealSystem.FlashColor(Color.green, 0.12f));
        }
        catch (System.Exception)
        {
            // If player's HealthSystem doesn't expose FlashColor, ignore silently
        }

        // Disable visuals and interaction immediately
        var rends = GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
            r.enabled = false;
        if (triggerCollider != null) triggerCollider.enabled = false;
        if (groundCollider != null) groundCollider.enabled = false;

        // Destroy this object after 1s (particle lifetime)
        Destroy(gameObject, 1.0f);
    }

    public void SetHealAmount(int amount)
    {
        healAmount = amount;
    }
}