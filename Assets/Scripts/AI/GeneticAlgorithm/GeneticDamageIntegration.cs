using UnityEngine;

/// <summary>
/// Automatically integrates the Genetic Algorithm system with player damage events.
/// Attach this to the Player to automatically register damage from enemies.
/// 
/// This eliminates the need to manually call RegisterDamageDealt() in every enemy attack script.
/// 
/// HOW IT WORKS:
/// 1. Listens to HealthSystem.OnDamageTaken events
/// 2. When player takes damage, checks if the source has an EnemyFitnessTracker
/// 3. If so, registers the damage for genetic fitness tracking
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class GeneticDamageIntegration : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Enable automatic damage registration for genetic algorithm")]
    [SerializeField] private bool enableAutoRegistration = true;
    
    [Tooltip("Show debug messages")]
    [SerializeField] private bool debugMode = true; // Default ON for debugging
    
    private HealthSystem healthSystem;
    private int lastHealth;
    
    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }
    
    private void Start()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken += OnPlayerDamageTaken;
            lastHealth = healthSystem.CurrentHealth;
            
            if (debugMode)
            {
                Debug.Log($"🧬 [GeneticIntegration] Initialized on {gameObject.name}");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken -= OnPlayerDamageTaken;
        }
    }
    
    /// <summary>
    /// Called when the player takes damage from any source.
    /// </summary>
    private void OnPlayerDamageTaken(GameObject damageSource)
    {
        if (!enableAutoRegistration) return;
        
        // Calculate actual damage taken
        int currentHealth = healthSystem.CurrentHealth;
        int damageTaken = lastHealth - currentHealth;
        lastHealth = currentHealth;
        
        // Always log when damage is taken for debugging
        if (debugMode)
        {
            Debug.Log($"🧬 [GeneticIntegration] Player took {damageTaken} damage. Source: {(damageSource != null ? damageSource.name : "NULL")}");
        }
        
        if (damageSource == null)
        {
            if (debugMode) Debug.LogWarning("🧬 [GeneticIntegration] Damage source is NULL - cannot attribute damage!");
            return;
        }
        
        if (damageTaken <= 0) 
        {
            if (debugMode) Debug.Log($"🧬 [GeneticIntegration] No actual damage taken (damageTaken={damageTaken})");
            return;
        }
        
        // Try to find EnemyFitnessTracker on the damage source or its parent
        var tracker = damageSource.GetComponent<EnemyFitnessTracker>();
        if (tracker == null)
        {
            tracker = damageSource.GetComponentInParent<EnemyFitnessTracker>();
        }
        
        if (tracker != null)
        {
            tracker.RegisterDamageDealt(damageTaken);
            
            if (debugMode)
            {
                Debug.Log($"🧬 [GeneticIntegration] ✓ Registered {damageTaken} damage from {damageSource.name} ({tracker.Species})");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"🧬 [GeneticIntegration] Damage source '{damageSource.name}' has no EnemyFitnessTracker - damage not tracked for genetics!");
        }
    }
    
    /// <summary>
    /// Manually register damage if needed (for special cases).
    /// </summary>
    public void RegisterDamageFrom(GameObject enemy, float damage)
    {
        var tracker = enemy?.GetComponent<EnemyFitnessTracker>();
        tracker?.RegisterDamageDealt(damage);
    }
}
