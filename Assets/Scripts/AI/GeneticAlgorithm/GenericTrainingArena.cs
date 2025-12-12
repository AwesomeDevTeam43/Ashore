using UnityEngine;
using System.Collections;

/// <summary>
/// Arena de treino para testar o algoritmo genético.
/// 
/// MODO SIMULADO: Controla o combate diretamente, aplicando dano frame a frame.
/// Não depende da IA natural dos inimigos.
/// 
/// SETUP NECESSÁRIO:
/// 1. Enemy prefab deve ter: EnemyBase (ou derivado), Enemy_Health, HealthSystem, EnemyFitnessTracker
/// 2. Enemy prefab deve ter stats atribuídos (Enemy_Stats scriptable object)
/// 3. GlobalGeneticEvolver deve existir na cena
/// 
/// FUNCIONAMENTO:
/// - Spawna inimigo
/// - Simula combate: player ataca enemy, enemy ataca player
/// - Usa valores do genoma para escalar dano do enemy
/// - Regista fitness baseado em dano feito e sobrevivência
/// </summary>
public class GeneticTrainingArena : MonoBehaviour{
        private ArenaScoreUI arenaScoreUI;
    void Awake()
    {
        // Ensure ArenaScoreUICreator is present for UI
        if (FindFirstObjectByType<ArenaScoreUICreator>() == null)
        {
            gameObject.AddComponent<ArenaScoreUICreator>();
        }
    }

    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject playerPrefab;
    
    [Header("Arena Settings")]
    public int rounds = 100;
    public float timeScale = 1f; // Manter 1 para ver o que acontece
    [Tooltip("Tempo entre ataques. DEVE ser > 0.2s (invincibility do enemy)")]
    public float attackInterval = 0.3f; // Tempo entre ataques (segundos reais)
    
    [Header("Player Stats (Simulado)")]
    public int playerMaxHealth = 100;
    public int playerDamage = 10;
    
    [Header("Enemy Base Stats")]
    [Tooltip("HP base do enemy - será escalado pelo genoma")]
    public int enemyBaseHealth = 80;
    [Tooltip("Dano base do enemy - será escalado pelo genoma")]
    public int enemyBaseDamage = 12;
    
    [Header("Spawn Position")]
    public Vector3 enemySpawnPos = new Vector3(0, 0, 0);
    [Tooltip("Se true, spawna o enemy perto da câmara principal")]
    public bool spawnNearCamera = true;
    
    [Header("Debug")]
    public bool debugMode = true;
    public bool showEveryAttack = false;
   
    public Transform playerSpawnTransform;

      
    // Runtime
    private int playerWins = 0;
    private int enemyWins = 0;
    public int PlayerWins => playerWins;
    public int EnemyWins => enemyWins;
    private int currentRound = 0;
    
    private GameObject currentEnemy;
    
    // Real player state
    private GameObject currentPlayer;
    private Player_Health playerHealthComponent;
    private HealthSystem playerHealthSystem;

    void Start(){
        // Find ArenaScoreUI for later use
        arenaScoreUI = FindObjectOfType<ArenaScoreUI>();
        if (GlobalGeneticEvolver.Instance == null)
        {
            Debug.LogError("[Arena] GlobalGeneticEvolver não encontrado! Criando um...");
            var go = new GameObject("GlobalGeneticEvolver");
            go.AddComponent<GlobalGeneticEvolver>();
        }
        StartCoroutine(RunTraining());
    }

    IEnumerator RunTraining()
    {
        Time.timeScale = timeScale;
        
        Debug.Log($"[Arena] ===== INICIANDO TREINO =====");
        Debug.Log($"[Arena] Rounds: {rounds}, Player HP: {playerMaxHealth}, Player DMG: {playerDamage}");
        Debug.Log($"[Arena] Enemy Base HP: {enemyBaseHealth}, Enemy Base DMG: {enemyBaseDamage}");
        
        for (currentRound = 1; currentRound <= rounds; currentRound++)
        {
            yield return StartCoroutine(RunSimulatedRound());
            
            if (currentRound % 10 == 0)
            {
                Debug.Log($"[Arena] === Progresso Round {currentRound}/{rounds} ===");
                Debug.Log($"[Arena] PlayerWins={playerWins}, EnemyWins={enemyWins}");
                
                // Mostrar estado da evolução
                if (GlobalGeneticEvolver.Instance != null)
                {
                    var species = EnemySpecies.BigCrab; // Ajustar conforme o enemy
                    int gen = GlobalGeneticEvolver.Instance.GetGeneration(species);
                    float avgFit = GlobalGeneticEvolver.Instance.GetAverageFitness(species);
                    Debug.Log($"[Arena] Evolução: Gen {gen}, Avg Fitness: {avgFit:F2}");
                }
            }
        }

        Time.timeScale = 1f;
        Debug.Log($"[Arena] ===== TREINO COMPLETO =====");
        Debug.Log($"[Arena] Resultados Finais: PlayerWins={playerWins}, EnemyWins={enemyWins}");
        Debug.Log($"[Arena] Win Rate Player: {(float)playerWins / rounds * 100:F1}%");
        Debug.Log($"[Arena] Win Rate Enemy: {(float)enemyWins / rounds * 100:F1}%");

        // Display the best genome for BigCrab in the UI
        if (GlobalGeneticEvolver.Instance != null && arenaScoreUI != null)
        {
            var bestGenome = GlobalGeneticEvolver.Instance.GetBestGenome(EnemySpecies.BigCrab);
            if (bestGenome != null)
            {
                string genomeInfo =
                    $"BEST GENOME (BigCrab):\n" +
                    $"  HP Gene: {bestGenome.healthGene:F3}\n" +
                    $"  DMG Gene: {bestGenome.damageGene:F3}\n" +
                    $"  AtkSpeed Gene: {bestGenome.attackSpeedGene:F3}\n" +
                    $"  MoveSpeed Gene: {bestGenome.movementSpeedGene:F3}\n" +
                    $"  AggroRange Gene: {bestGenome.aggressionRangeGene:F3}\n" +
                    $"  MeleeResist Gene: {bestGenome.meleeResistanceGene:F3}\n" +
                    $"  RangedResist Gene: {bestGenome.rangedResistanceGene:F3}\n" +
                    $"  Aggressiveness Gene: {bestGenome.aggressivenessGene:F3}\n" +
                    $"  Avg Fitness: {bestGenome.AverageFitness:F2}\n" +
                    $"  Usages: {bestGenome.TimesUsed}\n" +
                    $"  PowerLevel: {bestGenome.GetPowerLevel():F2}";
                arenaScoreUI.SetBestGenome(genomeInfo);
            }
            else
            {
                arenaScoreUI.SetBestGenome("No genome found for BigCrab.");
            }
        }
    }

    IEnumerator RunSimulatedRound()
    {
        // Cleanup anterior
        CleanupRound();
        Vector3 playerSpawnPos = playerSpawnTransform.position;
        // Spawn player
         // Place player left of enemy
        currentPlayer = Instantiate(playerPrefab, playerSpawnPos, Quaternion.identity);
        playerHealthComponent = currentPlayer.GetComponent<Player_Health>();
        playerHealthSystem = currentPlayer.GetComponent<HealthSystem>();
        if (playerHealthSystem != null)
        {
            playerHealthSystem.Initialize(playerMaxHealth);
        }
        
        // Determinar posição de spawn
        Vector3 spawnPos = enemySpawnPos;
        if (spawnNearCamera && Camera.main != null)
        {
            // Spawnar em frente à câmara, um pouco para baixo
            spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 5f;
            spawnPos.z = 0; // Manter Z = 0 para 2D
        }
        // Spawnar inimigo
        currentEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        
        // Desativar física/gravidade para o enemy não cair durante o teste
        var rb = currentEnemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Obter componentes do inimigo
        var enemyHealth = currentEnemy.GetComponent<Enemy_Health>();
        var enemyHealthSystem = currentEnemy.GetComponent<HealthSystem>();
        var tracker = currentEnemy.GetComponent<EnemyFitnessTracker>();
        var enemyBase = currentEnemy.GetComponent<EnemyBase>();
        
        // Esperar inicialização completa
        yield return null;
        yield return null;
        
        // Calcular stats do enemy baseado no genoma
        int enemyMaxHP = enemyBaseHealth;
        int enemyDamage = enemyBaseDamage;
        
        if (tracker != null && tracker.Genome != null)
        {
            enemyMaxHP = tracker.Genome.GetScaledHealth(enemyBaseHealth);
            enemyDamage = Mathf.RoundToInt(tracker.Genome.GetScaledDamage(enemyBaseDamage));
            
            if (debugMode)
            {
                Debug.Log($"[Arena] Round {currentRound} - Enemy Genome aplicado:");
                Debug.Log($"[Arena]   HP: {enemyBaseHealth} → {enemyMaxHP}");
                Debug.Log($"[Arena]   DMG: {enemyBaseDamage} → {enemyDamage}");
                Debug.Log($"[Arena]   Genome: {tracker.Genome}");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[Arena] Round {currentRound} - Sem genoma, usando stats base");
        }
        
        // Inicializar health do enemy
        if (enemyHealth != null)
        {
            enemyHealth.Initialize(enemyMaxHP, 0, 0, 0, 0, 0f, 0f);
        }
        
        // Esperar inicialização do health
        yield return null;
        
        int currentEnemyHP = enemyHealthSystem?.CurrentHealth ?? enemyMaxHP;
        int currentPlayerHP = playerHealthSystem != null ? playerHealthSystem.CurrentHealth : playerMaxHealth;

        if (debugMode)
        {
            Debug.Log($"[Arena] Round {currentRound} INÍCIO - Player HP: {currentPlayerHP}, Enemy HP: {currentEnemyHP}");
        }

        // Loop de combate real
        int turnCount = 0;
        float totalDamageToPlayer = 0f;

        while (currentPlayerHP > 0 && IsEnemyAlive(enemyHealthSystem))
        {
            turnCount++;

            // Player ataca enemy
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(playerDamage, Enemy_Health.DamageSourceType.PlayerMelee);
            }

            currentEnemyHP = enemyHealthSystem?.CurrentHealth ?? 0;

            if (showEveryAttack)
            {
                Debug.Log($"[Arena] Turn {turnCount}: Player → Enemy ({playerDamage} dmg). Enemy HP: {currentEnemyHP}");
            }

            // Verificar se enemy morreu
            if (!IsEnemyAlive(enemyHealthSystem))
            {
                break;
            }

            // Enemy ataca player (usando dano escalado pelo genoma)
            if (playerHealthSystem != null)
            {
                playerHealthSystem.TakeDamage(enemyDamage, currentEnemy);
                currentPlayerHP = playerHealthSystem.CurrentHealth;
            }
            totalDamageToPlayer += enemyDamage;

            // Registar dano para fitness tracking
            if (tracker != null)
            {
                tracker.RegisterDamageDealt(enemyDamage);
            }

            if (showEveryAttack)
            {
                Debug.Log($"[Arena] Turn {turnCount}: Enemy → Player ({enemyDamage} dmg). Player HP: {currentPlayerHP}");
            }

            // If player is dead (including overkill), break immediately so enemy gets the win
            if (currentPlayerHP <= 0)
            {
                break;
            }

            // Pequena pausa para não bloquear
            if (attackInterval > 0)
            {
                yield return new WaitForSeconds(attackInterval);
            }
            else
            {
                yield return null;
            }
        }

        // Determinar vencedor
        bool playerAlive = currentPlayerHP > 0;
        bool enemyAlive = IsEnemyAlive(enemyHealthSystem);
        // If both died in the same turn, count as enemy win (prevents player always winning)
        if (playerAlive && !enemyAlive)
        {
            playerWins++;
            if (debugMode)
            {
                Debug.Log($"[Arena] Round {currentRound} FIM - PLAYER VENCEU em {turnCount} turnos");
                Debug.Log($"[Arena]   Player HP restante: {currentPlayerHP}/{playerMaxHealth}");
                Debug.Log($"[Arena]   Dano total ao player: {totalDamageToPlayer}");
            }
            // O enemy morreu pelo combate - o sistema de health vai destruí-lo
            yield return new WaitForSeconds(0.2f);
            if (currentEnemy == null && debugMode)
                Debug.Log($"[Arena] Enemy já foi destruído pelo sistema de health");
        }
        else // enemy wins if both die or if only player dies
        {
            enemyWins++;
            if (debugMode)
            {
                Debug.Log($"[Arena] Round {currentRound} FIM - ENEMY VENCEU em {turnCount} turnos");
                Debug.Log($"[Arena]   Enemy HP restante: {currentEnemyHP}/{enemyMaxHP}");
                Debug.Log($"[Arena]   Dano total ao player: {totalDamageToPlayer}");
            }
            // O player morreu - o enemy venceu!
            if (tracker != null)
            {
                tracker.ForceKillCredit();
                if (debugMode) Debug.Log($"[Arena] Enemy venceu! Fitness credit forçado.");
            }
        }

        // Pequena pausa antes do próximo round
        yield return new WaitForSeconds(0.1f);

        // Cleanup - só se o enemy ainda existir
        CleanupRound();

        yield return null;
    }
    
    void CleanupRound()
    {
        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
            currentEnemy = null;
        }
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
            playerHealthComponent = null;
            playerHealthSystem = null;
        }
    }

    private bool IsEnemyAlive(HealthSystem healthSystem)
    {
        return healthSystem != null && healthSystem.IsAlive;
    }
    
    void OnDestroy()
    {
        CleanupRound();
        Time.timeScale = 1f;
    }
    
    public void StopTraining()
    {
        StopAllCoroutines();
        CleanupRound();
        Time.timeScale = 1f;
        Debug.Log($"[Arena] Treino parado. PlayerWins={playerWins}, EnemyWins={enemyWins}");
    }
    
    // Botão de contexto para testar
    [ContextMenu("Start Training")]
    void StartTrainingManual()
    {
        StopAllCoroutines();
        playerWins = 0;
        enemyWins = 0;
        currentRound = 0;
        StartCoroutine(RunTraining());
    }
}
    