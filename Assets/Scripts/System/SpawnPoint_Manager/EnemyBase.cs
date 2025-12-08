using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Assigned by SpawnManager (optional)")]
    public Enemy_Stats stats;

    [Header("Level Info")]
    [SerializeField] private int currentLevel = 1;
    public float currentDamage { get; private set; }
    
    [Header("Damage Scaling")]
    [SerializeField] protected float damageVariance = 0.1f; // ±10% variação aleatória
    
    protected Enemy_Health enemyHealth; // Tornar protected para classes filhas usarem

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private float hitVolume = 1f;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathVolume = 1f;

    protected virtual void Awake() // Mudar para protected virtual
    {
        enemyHealth = GetComponent<Enemy_Health>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public virtual void SetStats(Enemy_Stats s)
    {
        if (s == null) return;
        
        stats = s;
        currentDamage = s.damage;
        transform.localScale = s.baseScale;
    }

    public void ApplyLevelMultipliers(
        int level,
        float healthMult,
        float damageMult,
        float xpMult,
        float materialsMult,
        bool applyScaling,
        float sizeMult)
    {
        if (stats == null)
        {
            return;
        }
        
        currentLevel = level;
        
        float levelFactor = level - 1;

        
        int scaledHealth = Mathf.RoundToInt(stats.maxHealth * Mathf.Pow(healthMult, levelFactor));
        currentDamage = stats.damage * Mathf.Pow(damageMult, levelFactor);
        int scaledXP = Mathf.RoundToInt(stats.xpOnDeath * Mathf.Pow(xpMult, levelFactor));
        int scaledWood = Mathf.RoundToInt(stats.woodDrop * Mathf.Pow(materialsMult, levelFactor));
        int scaledStone = Mathf.RoundToInt(stats.stoneDrop * Mathf.Pow(materialsMult, levelFactor));
        int scaledRope = Mathf.RoundToInt(stats.ropeDrop * Mathf.Pow(materialsMult, levelFactor));
        
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<Enemy_Health>();
        }
        
        if (enemyHealth != null)
        {
            enemyHealth.Initialize(
                scaledHealth,
                scaledXP,
                scaledWood,
                scaledStone,
                scaledRope,
                stats.meleeResistance,
                stats.rangedResistance
            );
    
        }
        
        if (applyScaling)
        {
            float scaleFactor = Mathf.Pow(sizeMult, levelFactor);
            transform.localScale = stats.baseScale * scaleFactor;
            
        }
        else
        {
            transform.localScale = stats.baseScale;
        }
    }

    protected int GetScaledDamage()
    {
        float variance = Random.Range(1f - damageVariance, 1f + damageVariance);
        return Mathf.RoundToInt(currentDamage * variance);
    }

    public int GetLevel() => currentLevel;

    /// <summary>
    /// Plays the configured hit sound, if available. Safe to call from damage handlers.
    /// </summary>
    public virtual void PlayHitSound()
    {
        if (hitSound == null)
        {
            return;
        }
        // Prefer one-shot to avoid interrupting looping sources
        if (audioSource != null)
        {
            audioSource.PlayOneShot(hitSound, Mathf.Clamp01(hitVolume));
        }
        else
        {
            // Fallback: create a temporary AudioSource to play the clip
            var temp = gameObject.AddComponent<AudioSource>();
            temp.playOnAwake = false;
            temp.spatialBlend = 0f; // 2D by default; adjust if needed
            temp.volume = Mathf.Clamp01(hitVolume);
            temp.clip = hitSound;
            temp.Play();
            Destroy(temp, hitSound.length + 0.05f);
        }
    }

    /// <summary>
    /// Plays the configured death sound immediately. Uses a detached temporary AudioSource
    /// so the sound continues even if the enemy GameObject is destroyed.
    /// </summary>
    public virtual void PlayDeathSound()
    {
        if (deathSound == null)
        {
            return;
        }
        // Create an ephemeral GO to host the one-shot so destruction of the enemy doesn't cut audio
        var host = new GameObject($"{name}_DeathSound");
        host.transform.position = transform.position;
        var src = host.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D by default; set to 1f for 3D
        src.volume = Mathf.Clamp01(deathVolume);
        src.clip = deathSound;
        src.Play();
        Object.Destroy(host, deathSound.length + 0.1f);
    }
}