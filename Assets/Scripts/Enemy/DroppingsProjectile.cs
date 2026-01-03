using UnityEngine;

public class DroppingsProjectile : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 8f;
    [SerializeField] private float gravityAcceleration = 0f; // set >0 to simulate acceleration
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private bool destroyOnGround = true;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask damageLayers;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip groundImpactClip;
    [SerializeField, Range(0f, 1f)] private float groundImpactVolume = 1f;

    [Header("Puddle Spawn")]
    [SerializeField] private bool spawnPuddleOnGround = true;
    [SerializeField] private GameObject miasmaPuddlePrefab;
    [SerializeField] private Vector2 puddleSpawnOffset = Vector2.zero;

    private float _lifeTimer;
    private float _verticalVelocity;
    private bool _hasImpacted;

    private void Awake()
    {
        _verticalVelocity = -fallSpeed;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
        if (sfxSource != null && AudioManager.Instance != null)
        {
            var g = AudioManager.Instance.GetSFXGroup();
            if (g != null) sfxSource.outputAudioMixerGroup = g;
        }
    }

    private void Update()
    {
        if (_hasImpacted) return;

        // Simple custom gravity (optional)
        if (gravityAcceleration > 0f)
        {
            _verticalVelocity -= gravityAcceleration * Time.deltaTime;
        }
        transform.Translate(Vector3.up * _verticalVelocity * Time.deltaTime, Space.World);

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (destroyOnGround)
        {
            var hit = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundMask);
            if (hit)
            {
                HandleGroundImpact();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsDamageLayer(other.gameObject.layer))
        {
            return;
        }

        if (TryApplyDamage(other))
        {
            Destroy(gameObject);
        }
    }

    private bool IsDamageLayer(int otherLayer)
    {
        if (damageLayers == 0)
        {
            // Default to player-only (assumes Player layer exists)
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                damageLayers = 1 << playerLayer;
            }
        }
        return (damageLayers.value & (1 << otherLayer)) != 0;
    }

    private bool TryApplyDamage(Collider2D other)
    {
        // Prefer custom damage receivers (player/enemies that opt-in)
        var receiver = other.GetComponentInParent<IDamageReceiver>();
        if (receiver != null)
        {
            receiver.ReceiveDamage(damageAmount);
            return true;
        }

        // Directly drive the shared HealthSystem if present
        var hs = other.GetComponentInParent<HealthSystem>();
        if (hs != null)
        {
            hs.TakeDamage(damageAmount, gameObject);
            return true;
        }

        // As a fallback, search for any MonoBehaviour exposing TakeDamage(int)
        var behaviours = other.GetComponentsInParent<MonoBehaviour>();
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            var method = mb.GetType().GetMethod("TakeDamage", new[] { typeof(int) });
            if (method != null)
            {
                method.Invoke(mb, new object[] { damageAmount });
                return true;
            }
        }

        return false;
    }

    private void HandleGroundImpact()
    {
        if (_hasImpacted) return;
        _hasImpacted = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (groundImpactClip != null)
        {
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
                if (AudioManager.Instance != null)
                {
                    var g = AudioManager.Instance.GetSFXGroup();
                    if (g != null) sfxSource.outputAudioMixerGroup = g;
                }
            }
            sfxSource.PlayOneShot(groundImpactClip, groundImpactVolume);
        }

        // Prevent further hits/damage while the audio finishes.
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (spawnPuddleOnGround && miasmaPuddlePrefab != null)
        {
            var pos = transform.position + (Vector3)puddleSpawnOffset;
            Instantiate(miasmaPuddlePrefab, pos, Quaternion.identity);
        }

        if (groundImpactClip != null)
        {
            Destroy(gameObject, groundImpactClip.length + 0.05f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!destroyOnGround) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
    }
}

// Simple interface you can implement on the player or health component.
public interface IDamageReceiver
{
    void ReceiveDamage(int amount);
}
