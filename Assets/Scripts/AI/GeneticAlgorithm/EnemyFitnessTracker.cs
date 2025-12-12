using UnityEngine;

/// <summary>
/// Component that tracks an individual enemy's performance for the genetic algorithm.
/// Attach to each enemy to enable genetic evolution tracking.
/// 
/// FEATURES:
/// - Auto-detects species from Enemy_Stats
/// - Tracks damage dealt (via explicit registration, not proximity)
/// - Tracks survival time
/// - Reports to GlobalGeneticEvolver on death
/// - Applies genome stats to the enemy
/// 
/// DAMAGE ATTRIBUTION:
/// - Enemies MUST call RegisterDamageDealt() when they deal damage
/// - This ensures only the actual attacker gets fitness credit
/// - The old proximity-based system was inaccurate
/// </summary>
[RequireComponent(typeof(Enemy_Health))]
public class EnemyFitnessTracker : MonoBehaviour
{
    [Header("Tracking Status (Read-Only)")]
    [SerializeField] private EnemySpecies _species = EnemySpecies.Unknown;
    [SerializeField] private float _damageDealtToPlayer = 0f;
    [SerializeField] private float _spawnTime;
    [SerializeField] private bool _isTracking = false;
    [SerializeField] private bool _wasKilledByPlayer = false;
    [SerializeField] private int _currentGeneration = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("UI")]
    [SerializeField] private bool showGenomeUI = true;
    
    [Header("Zone Difficulty")]
    [Tooltip("Zone difficulty multiplier (optional)")]
    [SerializeField] private ZoneDifficulty zoneDifficulty;
    
    // References
    private EnemyGenome genome;
    private EnemyGenomeUI genomeUI;
    private Enemy_Health enemyHealth;
    private HealthSystem enemyHealthSystem;
    private int instanceId;
    
    // ==================== PUBLIC PROPERTIES ====================
    
    /// <summary>
    /// The genome assigned to this enemy.
    /// </summary>
    public EnemyGenome Genome => genome;
    
    /// <summary>
    /// The species of this enemy.
    /// </summary>
    public EnemySpecies Species => _species;
    
    /// <summary>
    /// Total damage dealt to player.
    /// </summary>
    public float DamageDealtToPlayer => _damageDealtToPlayer;
    
    /// <summary>
    /// Time since spawn.
    /// </summary>
    public float SurvivalTime => Time.time - _spawnTime;
    
    // ==================== UNITY LIFECYCLE ====================
    
    private void Awake()
    {
        enemyHealth = GetComponent<Enemy_Health>();
        enemyHealthSystem = GetComponent<HealthSystem>();
        instanceId = GetInstanceID();
        
        // Subscribe to health events for death detection
        if (enemyHealthSystem != null)
        {
            enemyHealthSystem.OnHealthChanged += OnEnemyHealthChanged;
        }
    }
    
    private void Start()
    {
        _spawnTime = Time.time;
        
        // Detect species from EnemyBase stats
        var enemyBase = GetComponent<EnemyBase>();
        if (enemyBase != null && enemyBase.stats != null)
        {
            _species = EnemySpeciesHelper.GetSpeciesFromStats(enemyBase.stats);
            
            if (showDebugInfo)
            {
                Debug.Log($"🧬 [{name}] Detected species: {EnemySpeciesHelper.GetDisplayName(_species)}");
            }
        }
        else
        {
            Debug.LogWarning($"🧬 [{name}] Could not detect species - no EnemyBase or stats found");
        }
        
        // Always add UI if enabled (even without genome, to show debug state)
        if (showGenomeUI)
        {
            genomeUI = GetComponent<EnemyGenomeUI>();
            if (genomeUI == null)
            {
                genomeUI = gameObject.AddComponent<EnemyGenomeUI>();
                Debug.Log($"🧬 [{name}] Added EnemyGenomeUI component");
            }
            else
            {
                Debug.Log($"🧬 [{name}] EnemyGenomeUI already exists");
            }
        }
        else
        {
            Debug.Log($"🧬 [{name}] showGenomeUI is FALSE - not adding UI");
        }
        
        // Get genome from global evolver
        if (GlobalGeneticEvolver.Instance != null && EnemySpeciesHelper.ShouldEvolve(_species))
        {
            float zoneMult = zoneDifficulty != null ? zoneDifficulty.difficultyMultiplier : 1f;
            
            genome = GlobalGeneticEvolver.Instance.GetGenome(instanceId, _species, zoneMult);
            _isTracking = true;
            _currentGeneration = GlobalGeneticEvolver.Instance.GetGeneration(_species);
            
            if (showDebugInfo)
            {
                Debug.Log($"🧬 [{name}] Got genome (Gen {_currentGeneration}, Species: {EnemySpeciesHelper.GetDisplayName(_species)}, Zone×{zoneMult})");
            }
            
            // Apply genome to enemy stats
            ApplyGenomeToEnemy();
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log($"🧬 [{name}] No genetic system found or species doesn't evolve. Using default stats.");
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (enemyHealthSystem != null)
        {
            enemyHealthSystem.OnHealthChanged -= OnEnemyHealthChanged;
        }
        
        if (!_isTracking || genome == null)
            return;
        
        if (showDebugInfo)
        {
            Debug.Log($"🧬 [{name}] Destroyed. Killed by player: {_wasKilledByPlayer}, Damage dealt: {_damageDealtToPlayer:F1}");
        }
        
        // Report to global evolver
        if (GlobalGeneticEvolver.Instance != null)
        {
            GlobalGeneticEvolver.Instance.RegisterKill(
                instanceId,
                SurvivalTime,
                _wasKilledByPlayer
            );
        }
    }
    
    // ==================== EVENT HANDLERS ====================
    
    /// <summary>
    /// Called when enemy health changes - detects death.
    /// </summary>
    private void OnEnemyHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0 && !_wasKilledByPlayer)
        {
            _wasKilledByPlayer = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"🧬 [{name}] Killed by player! Final damage dealt: {_damageDealtToPlayer:F1}");
            }
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// MUST BE CALLED when this enemy deals damage to the player.
    /// This is the ONLY way to get accurate fitness tracking.
    /// 
    /// Call this from your enemy attack scripts when damage is confirmed.
    /// </summary>
    /// <param name="damage">Amount of damage dealt</param>
    public void RegisterDamageDealt(float damage)
    {
        if (!_isTracking || genome == null)
            return;
        
        _damageDealtToPlayer += damage;
        genome.damageDealtThisLife += damage;
        
        // Also notify global evolver for real-time tracking
        if (GlobalGeneticEvolver.Instance != null)
        {
            GlobalGeneticEvolver.Instance.RegisterDamageDealt(instanceId, damage);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"🧬 [{name}] Dealt {damage} damage to player. Total: {_damageDealtToPlayer:F1}");
        }
    }
    
    /// <summary>
    /// Gets the scaled damage from the genome.
    /// Use this instead of base damage for attacks.
    /// </summary>
    public float GetScaledDamage(float baseDamage)
    {
        if (genome == null) return baseDamage;
        return genome.GetScaledDamage(baseDamage);
    }
    
    /// <summary>
    /// Gets the scaled movement speed from the genome.
    /// </summary>
    public float GetScaledMovementSpeed(float baseSpeed)
    {
        if (genome == null) return baseSpeed;
        return genome.GetScaledMovementSpeed(baseSpeed);
    }
    
    /// <summary>
    /// Gets the scaled attack interval from the genome.
    /// </summary>
    public float GetScaledAttackInterval(float baseInterval)
    {
        if (genome == null) return baseInterval;
        return genome.GetScaledAttackInterval(baseInterval);
    }
    
    /// <summary>
    /// Gets the scaled aggression range from the genome.
    /// </summary>
    public float GetScaledAggressionRange(float baseRange)
    {
        if (genome == null) return baseRange;
        return genome.GetScaledAggressionRange(baseRange);
    }
    
    // ==================== INTERNAL ====================
    
    /// <summary>
    /// Applies the genome to enemy stats.
    /// </summary>
    private void ApplyGenomeToEnemy()
    {
        if (genome == null) return;
        
        // Get references
        var enemyBase = GetComponent<EnemyBase>();
        if (enemyBase == null || enemyBase.stats == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"🧬 [{name}] No EnemyBase or stats found. Cannot apply genome.");
            }
            return;
        }
        
        var stats = enemyBase.stats;
        
        // Calculate scaled values from genome
        int scaledHealth = genome.GetScaledHealth(stats.maxHealth);
        float scaledMeleeRes = Mathf.Max(stats.meleeResistance, genome.GetScaledMeleeResistance());
        float scaledRangedRes = Mathf.Max(stats.rangedResistance, genome.GetScaledRangedResistance());
        
        // Apply to Enemy_Health
        if (enemyHealth != null)
        {
            enemyHealth.Initialize(
                scaledHealth,
                stats.xpOnDeath,
                stats.woodDrop,
                stats.stoneDrop,
                stats.ropeDrop,
                scaledMeleeRes,
                scaledRangedRes
            );
        }
        
        if (showDebugInfo)
        {
            float scaledDamage = genome.GetScaledDamage(stats.damage);
            Debug.Log($"🧬 [{name}] Applied genome: HP={scaledHealth}, DMG={scaledDamage:F1}, MeleeRes={scaledMeleeRes:F2}, RangedRes={scaledRangedRes:F2}");
        }
    }
    
    // ==================== DEBUG ====================
    
#if UNITY_EDITOR
    [ContextMenu("Log Genome Info")]
    private void LogGenomeInfo()
    {
        if (genome == null)
        {
            Debug.Log($"🧬 [{name}] No genome assigned");
            return;
        }
        
        Debug.Log($"🧬 [{name}] Genome Info:");
        Debug.Log($"  Species: {EnemySpeciesHelper.GetDisplayName(_species)}");
        Debug.Log($"  Generation: {_currentGeneration}");
        Debug.Log($"  Power Level: {genome.GetPowerLevel():F2}");
        Debug.Log($"  {genome}");
        Debug.Log($"  Damage Dealt This Life: {_damageDealtToPlayer:F1}");
        Debug.Log($"  Survival Time: {SurvivalTime:F1}s");
    }
#endif
}
