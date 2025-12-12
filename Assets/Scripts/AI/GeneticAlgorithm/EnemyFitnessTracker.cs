using UnityEngine;

/// <summary>
/// Componente que rastreia o desempenho de um inimigo individual.
/// Anexar a cada inimigo para registrar fitness quando morrer.
/// </summary>
[RequireComponent(typeof(Enemy_Health))]
public class EnemyFitnessTracker : MonoBehaviour
{
    [Header("Tracking Status (Read-Only)")]
    [SerializeField] private float _damageDealtToPlayer = 0f;
    [SerializeField] private float _spawnTime;
    [SerializeField] private bool _isTracking = false;
    [SerializeField] private bool _wasKilledByPlayer = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    [Header("UI")]
    [SerializeField] private bool showGenomeUI = true;
    
    [Header("Zone Difficulty")]
    [Tooltip("Multiplicador de dificuldade da zona (opcional)")]
    [SerializeField] private ZoneDifficulty zoneDifficulty;
    
    // Referências
    private EnemyGenome genome;
    private EnemyGenomeUI genomeUI;
    private Enemy_Health enemyHealth;
    private HealthSystem playerHealthSystem;
    private int instanceId;
    
    // Cache para detectar dano ao jogador
    private int lastKnownPlayerHealth;
    
    /// <summary>
    /// O genoma atribuído a este inimigo
    /// </summary>
    public EnemyGenome Genome => genome;
    
    /// <summary>
    /// Dano total causado ao jogador
    /// </summary>
    public float DamageDealtToPlayer => _damageDealtToPlayer;
    
    /// <summary>
    /// Tempo de vida desde o spawn
    /// </summary>
    public float SurvivalTime => Time.time - _spawnTime;
    // Referência ao HealthSystem do inimigo
    private HealthSystem enemyHealthSystem;
    
    private void Awake()
    {
        enemyHealth = GetComponent<Enemy_Health>();
        instanceId = GetInstanceID();
        
        // Subscribe to health changed event for accurate kill detection
        enemyHealthSystem = GetComponent<HealthSystem>();
        if (enemyHealthSystem != null)
        {
            enemyHealthSystem.OnHealthChanged += OnEnemyHealthChanged;
        }
    }
    
    /// <summary>
    /// Called when enemy health changes - detects death
    /// </summary>
    private void OnEnemyHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0 && !_wasKilledByPlayer)
        {
            _wasKilledByPlayer = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"🧬 [{name}] Killed by player!");
            }
        }
    }
    
    private void Start()
    {
        _spawnTime = Time.time;
        
        // Encontra o jogador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealthSystem = player.GetComponent<HealthSystem>();
            if (playerHealthSystem != null)
            {
                lastKnownPlayerHealth = playerHealthSystem.CurrentHealth;
            }
        }
        
        // Obtém genoma do sistema GLOBAL
        float zoneMult = zoneDifficulty != null ? zoneDifficulty.difficultyMultiplier : 1f;
        
        // Prioridade: GlobalGeneticEvolver > GeneticEnemyEvolver (legacy)
        if (GlobalGeneticEvolver.Instance != null)
        {
            genome = GlobalGeneticEvolver.Instance.GetGenome(instanceId, zoneMult);
            _isTracking = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"🧬 [{name}] Got genome (Gen {GlobalGeneticEvolver.Instance.Generation}, Zone×{zoneMult})");
            }
        }
        else if (GeneticEnemyEvolver.Instance != null)
        {
            genome = GeneticEnemyEvolver.Instance.GetGenomeForNewEnemy(instanceId);
            _isTracking = true;
        }
        
        if (_isTracking && genome != null)
        {
            // Aplica os genes ao inimigo
            ApplyGenomeToEnemy();
            
            // Adiciona UI se ativado
            if (showGenomeUI)
            {
                genomeUI = GetComponent<EnemyGenomeUI>();
                if (genomeUI == null)
                {
                    genomeUI = gameObject.AddComponent<EnemyGenomeUI>();
                }
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"🧬 [{name}] No genetic system found. Using default stats.");
            }
        }
    }
    
    private void Update()
    {
        if (!_isTracking || playerHealthSystem == null) return;
        
        // Detecta dano causado ao jogador
        int currentPlayerHealth = playerHealthSystem.CurrentHealth;
        if (currentPlayerHealth < lastKnownPlayerHealth)
        {
            int damageDone = lastKnownPlayerHealth - currentPlayerHealth;
            
            // Verifica se este inimigo provavelmente causou o dano
            // (simplificação: qualquer inimigo próximo ao jogador recebe crédito parcial)
            GameObject player = playerHealthSystem.gameObject;
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            // Se está próximo o suficiente para ter causado dano
            if (distanceToPlayer < 5f)
            {
                float creditMultiplier = Mathf.Clamp01(1f - (distanceToPlayer / 5f));
                float creditedDamage = damageDone * creditMultiplier;
                _damageDealtToPlayer += creditedDamage;
                
                // Notifica o evolver sobre dano ao jogador
                GeneticEnemyEvolver.Instance?.RegisterPlayerDamageTaken(Mathf.RoundToInt(creditedDamage));
                
                if (showDebugInfo)
                {
                    Debug.Log($"🧬 [{name}] Credited with {creditedDamage:F1} damage (distance: {distanceToPlayer:F1})");
                }
            }
        }
        lastKnownPlayerHealth = currentPlayerHealth;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (enemyHealthSystem != null)
        {
            enemyHealthSystem.OnHealthChanged -= OnEnemyHealthChanged;
        }
        
        if (!_isTracking || genome == null) return;
        
        // Only count as killed if health reached 0
        // (scene changes destroy objects without triggering health change to 0)
        
        if (showDebugInfo)
        {
            Debug.Log($"🧬 [{name}] Destroyed. Killed by player: {_wasKilledByPlayer}");
        }
        
        // Regista no sistema GLOBAL
        if (GlobalGeneticEvolver.Instance != null)
        {
            GlobalGeneticEvolver.Instance.RegisterKill(
                instanceId,
                _damageDealtToPlayer,
                SurvivalTime,
                _wasKilledByPlayer
            );
        }
        // Fallback para sistema legacy
        else if (GeneticEnemyEvolver.Instance != null)
        {
            GeneticEnemyEvolver.Instance.RegisterEnemyDeath(
                instanceId,
                _damageDealtToPlayer,
                SurvivalTime,
                _wasKilledByPlayer
            );
        }
    }
    
    /// <summary>
    /// Aplica os genes do genoma aos stats do inimigo
    /// </summary>
    private void ApplyGenomeToEnemy()
    {
        if (genome == null) return;
        
        // Obtém referências
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
        
        // Calcula valores escalados baseados no genoma
        int scaledHealth = genome.GetScaledHealth(stats.maxHealth);
        float scaledDamage = genome.GetScaledDamage(stats.damage);
        float scaledMeleeRes = Mathf.Max(stats.meleeResistance, genome.GetScaledMeleeResistance());
        float scaledRangedRes = Mathf.Max(stats.rangedResistance, genome.GetScaledRangedResistance());
        
        // Aplica ao Enemy_Health através do método Initialize
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
        
        // Atualiza o dano no EnemyBase (via reflection ou campo público se disponível)
        // Por agora, armazenamos no tracker para uso posterior
        _scaledDamage = scaledDamage;
        
        if (showDebugInfo)
        {
            Debug.Log($"🧬 [{name}] Applied genome: HP={scaledHealth}, DMG={scaledDamage:F1}, MeleeRes={scaledMeleeRes:F2}, RangedRes={scaledRangedRes:F2}");
        }
    }
    
    // Armazena dano escalado para uso por outros componentes
    private float _scaledDamage;
    
    /// <summary>
    /// Obtém o dano escalado pelo genoma
    /// </summary>
    public float GetScaledDamage(float baseDamage)
    {
        if (genome == null) return baseDamage;
        return genome.GetScaledDamage(baseDamage);
    }
    
    /// <summary>
    /// Obtém a velocidade de movimento escalada pelo genoma
    /// </summary>
    public float GetScaledMovementSpeed(float baseSpeed)
    {
        if (genome == null) return baseSpeed;
        return genome.GetScaledMovementSpeed(baseSpeed);
    }
    
    /// <summary>
    /// Obtém o intervalo de ataque escalado pelo genoma
    /// </summary>
    public float GetScaledAttackInterval(float baseInterval)
    {
        if (genome == null) return baseInterval;
        return genome.GetScaledAttackInterval(baseInterval);
    }
    
    /// <summary>
    /// Obtém o alcance de agressão escalado pelo genoma
    /// </summary>
    public float GetScaledAggressionRange(float baseRange)
    {
        if (genome == null) return baseRange;
        return genome.GetScaledAggressionRange(baseRange);
    }
    
    /// <summary>
    /// Registra dano causado manualmente (chamar quando o inimigo ataca)
    /// </summary>
    public void RegisterDamageDealt(float damage)
    {
        _damageDealtToPlayer += damage;
        
        if (showDebugInfo)
        {
            Debug.Log($"🧬 [{name}] Manually registered {damage} damage. Total: {_damageDealtToPlayer:F1}");
        }
    }
}
