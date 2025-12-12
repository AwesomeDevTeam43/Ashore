using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sistema principal do Algoritmo Genético que evolui os inimigos ao longo do jogo.
/// Singleton que persiste entre cenas e gerações.
/// 
/// COMO FUNCIONA:
/// 1. Mantém uma população de genomas
/// 2. Quando inimigos morrem, seu fitness é registrado
/// 3. A cada N mortes ou ao trocar de sala/wave, evolui a população
/// 4. Novos inimigos recebem genomas da população evoluída
/// </summary>
public class GeneticEnemyEvolver : MonoBehaviour
{
    public static GeneticEnemyEvolver Instance { get; private set; }
    
    [Header("Population Settings")]
    [Tooltip("Tamanho da população de genomas")]
    [SerializeField] private int populationSize = 20;
    
    [Tooltip("Número de inimigos mortos para triggerar evolução")]
    [SerializeField] private int evolveTriggerCount = 10;
    
    [Header("Selection Settings")]
    [Tooltip("Número de melhores genomas a manter (elitismo)")]
    [SerializeField] private int eliteCount = 4;
    
    [Tooltip("Tamanho do torneio para seleção")]
    [SerializeField] private int tournamentSize = 3;
    
    [Header("Genetic Operators")]
    [Tooltip("Taxa de mutação (0-1)")]
    [SerializeField, Range(0f, 1f)] private float mutationRate = 0.15f;
    
    [Tooltip("Força da mutação (quanto cada gene pode mudar)")]
    [SerializeField, Range(0f, 0.5f)] private float mutationStrength = 0.2f;
    
    [Tooltip("Taxa de crossover (probabilidade de cruzar dois pais vs clonar)")]
    [SerializeField, Range(0f, 1f)] private float crossoverRate = 0.7f;
    
    [Header("Adaptive Difficulty")]
    [Tooltip("Ativar dificuldade adaptativa baseada no desempenho do jogador")]
    [SerializeField] private bool adaptiveDifficulty = true;
    
    [Tooltip("Se jogador está morrendo muito, reduzir fitness de genomas agressivos")]
    [SerializeField, Range(0f, 1f)] private float difficultyAdjustmentStrength = 0.3f;
    
    [Header("Persistence")]
    [Tooltip("Salvar população entre sessões")]
    [SerializeField] private bool persistBetweenSessions = true;
    
    private const string SAVE_KEY = "GeneticEnemyPopulation";
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private int _generation = 0;
    [SerializeField] private int _totalEnemiesKilled = 0;
    [SerializeField] private float _averagePopulationFitness = 0f;
    
    // População atual
    private List<EnemyGenome> population = new List<EnemyGenome>();
    
    // Genomas ativos (associados a inimigos vivos)
    private Dictionary<int, EnemyGenome> activeGenomes = new Dictionary<int, EnemyGenome>();
    
    // Contador para evolução
    private int enemiesKilledSinceLastEvolution = 0;
    
    // Estatísticas do jogador para dificuldade adaptativa
    private int playerDeathsThisSession = 0;
    private int playerDamageTakenThisSession = 0;
    
    // ==================== UNITY LIFECYCLE ====================
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializePopulation();
    }
    
    private void Start()
    {
        if (persistBetweenSessions)
        {
            LoadPopulation();
        }
    }
    
    private void OnApplicationQuit()
    {
        if (persistBetweenSessions)
        {
            SavePopulation();
        }
    }
    
    // ==================== INICIALIZAÇÃO ====================
    
    private void InitializePopulation()
    {
        if (population.Count >= populationSize) return;
        
        int toCreate = populationSize - population.Count;
        
        for (int i = 0; i < toCreate; i++)
        {
            population.Add(EnemyGenome.CreateRandom());
        }
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] Initialized population with {populationSize} genomes");
        }
    }
    
    // ==================== API PÚBLICA ====================
    
    /// <summary>
    /// Obtém um genoma para um novo inimigo (seleção por torneio)
    /// </summary>
    public EnemyGenome GetGenomeForNewEnemy(int enemyInstanceId)
    {
        if (population.Count == 0)
        {
            InitializePopulation();
        }
        
        // Seleção por torneio: escolhe o melhor entre K candidatos aleatórios
        EnemyGenome selected = TournamentSelect();
        
        // Clona para não modificar o original da população
        EnemyGenome genome = selected.Clone();
        
        // Pequena mutação para variação
        genome.Mutate(mutationRate * 0.5f, mutationStrength * 0.5f);
        
        // Registra como ativo
        activeGenomes[enemyInstanceId] = genome;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] Assigned genome to enemy {enemyInstanceId}: {genome}");
        }
        
        return genome;
    }
    
    /// <summary>
    /// Registra o fitness de um inimigo quando ele morre
    /// </summary>
    /// <param name="enemyInstanceId">ID da instância do inimigo</param>
    /// <param name="damageDealtToPlayer">Dano total causado ao jogador</param>
    /// <param name="survivalTime">Tempo que o inimigo sobreviveu</param>
    /// <param name="wasKilledByPlayer">Se foi morto pelo jogador (vs caiu em buraco, etc)</param>
    public void RegisterEnemyDeath(int enemyInstanceId, float damageDealtToPlayer, float survivalTime, bool wasKilledByPlayer = true)
    {
        if (!activeGenomes.TryGetValue(enemyInstanceId, out EnemyGenome genome))
        {
            return; // Inimigo não tinha genoma registrado
        }
        
        // Calcula fitness
        float fitness = CalculateFitness(damageDealtToPlayer, survivalTime, wasKilledByPlayer);
        
        // Aplica ajuste de dificuldade adaptativa
        if (adaptiveDifficulty)
        {
            fitness = ApplyAdaptiveDifficultyAdjustment(fitness, genome);
        }
        
        // Atualiza o genoma correspondente na população
        UpdatePopulationFitness(genome, fitness);
        
        // Remove dos ativos
        activeGenomes.Remove(enemyInstanceId);
        
        // Incrementa contador
        enemiesKilledSinceLastEvolution++;
        _totalEnemiesKilled++;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] Enemy {enemyInstanceId} died. Fitness: {fitness:F2}. Killed since evolution: {enemiesKilledSinceLastEvolution}/{evolveTriggerCount}");
        }
        
        // Verifica se deve evoluir
        if (enemiesKilledSinceLastEvolution >= evolveTriggerCount)
        {
            EvolvePopulation();
        }
    }
    
    /// <summary>
    /// Força uma evolução da população (chamar ao trocar de sala/wave)
    /// </summary>
    public void ForceEvolution()
    {
        if (enemiesKilledSinceLastEvolution > 0)
        {
            EvolvePopulation();
        }
    }
    
    /// <summary>
    /// Registra morte do jogador (para dificuldade adaptativa)
    /// </summary>
    public void RegisterPlayerDeath()
    {
        playerDeathsThisSession++;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] Player death registered. Total this session: {playerDeathsThisSession}");
        }
    }
    
    /// <summary>
    /// Registra dano tomado pelo jogador
    /// </summary>
    public void RegisterPlayerDamageTaken(int damage)
    {
        playerDamageTakenThisSession += damage;
    }
    
    /// <summary>
    /// Reseta as estatísticas da sessão
    /// </summary>
    public void ResetSessionStats()
    {
        playerDeathsThisSession = 0;
        playerDamageTakenThisSession = 0;
    }
    
    /// <summary>
    /// Reseta toda a população para valores aleatórios
    /// </summary>
    public void ResetPopulation()
    {
        population.Clear();
        activeGenomes.Clear();
        _generation = 0;
        enemiesKilledSinceLastEvolution = 0;
        InitializePopulation();
        
        if (persistBetweenSessions)
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
        }
        
        Debug.Log("🧬 [GA] Population reset to random!");
    }
    
    // ==================== ALGORITMO GENÉTICO ====================
    
    private float CalculateFitness(float damageDealt, float survivalTime, bool wasKilledByPlayer)
    {
        // Fitness baseado em:
        // 1. Dano causado ao jogador (principal)
        // 2. Tempo de sobrevivência (secundário)
        // 3. Bônus se foi morto pelo jogador (significa que foi uma ameaça)
        
        float fitness = 0f;
        
        // Dano é o principal indicador de sucesso
        fitness += damageDealt * 10f;
        
        // Sobrevivência mostra que o inimigo foi difícil de matar
        fitness += survivalTime * 0.5f;
        
        // Bônus por ser uma ameaça real (jogador precisou matar)
        if (wasKilledByPlayer && damageDealt > 0)
        {
            fitness *= 1.2f;
        }
        
        return fitness;
    }
    
    private float ApplyAdaptiveDifficultyAdjustment(float fitness, EnemyGenome genome)
    {
        // Se jogador está morrendo muito, penaliza genomas muito agressivos
        // para naturalmente selecionar inimigos mais fáceis
        
        if (playerDeathsThisSession > 3)
        {
            // Jogador morrendo muito - penaliza agressividade
            float aggressivePenalty = genome.aggressivenessGene * difficultyAdjustmentStrength;
            fitness *= (1f - aggressivePenalty);
        }
        else if (playerDeathsThisSession == 0 && _totalEnemiesKilled > 20)
        {
            // Jogador está dominando - bônus para genomas agressivos
            float aggressiveBonus = genome.aggressivenessGene * difficultyAdjustmentStrength;
            fitness *= (1f + aggressiveBonus);
        }
        
        return fitness;
    }
    
    private void UpdatePopulationFitness(EnemyGenome usedGenome, float fitness)
    {
        // Encontra genomas similares na população e atualiza seu fitness
        // (já que usamos clones, precisamos encontrar o "pai" mais próximo)
        
        EnemyGenome mostSimilar = FindMostSimilarInPopulation(usedGenome);
        if (mostSimilar != null)
        {
            mostSimilar.Fitness += fitness;
            mostSimilar.TimesUsed++;
        }
        
        // Atualiza estatística de fitness médio
        _averagePopulationFitness = population.Average(g => g.AverageFitness);
    }
    
    private EnemyGenome FindMostSimilarInPopulation(EnemyGenome target)
    {
        EnemyGenome best = null;
        float bestSimilarity = float.MaxValue;
        
        foreach (var genome in population)
        {
            float diff = CalculateGenomeDifference(target, genome);
            if (diff < bestSimilarity)
            {
                bestSimilarity = diff;
                best = genome;
            }
        }
        
        return best;
    }
    
    private float CalculateGenomeDifference(EnemyGenome a, EnemyGenome b)
    {
        return Mathf.Abs(a.healthGene - b.healthGene) +
               Mathf.Abs(a.damageGene - b.damageGene) +
               Mathf.Abs(a.attackSpeedGene - b.attackSpeedGene) +
               Mathf.Abs(a.movementSpeedGene - b.movementSpeedGene) +
               Mathf.Abs(a.aggressionRangeGene - b.aggressionRangeGene) +
               Mathf.Abs(a.aggressivenessGene - b.aggressivenessGene);
    }
    
    private void EvolvePopulation()
    {
        _generation++;
        enemiesKilledSinceLastEvolution = 0;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] ========== EVOLVING TO GENERATION {_generation} ==========");
            Debug.Log($"🧬 [GA] Best fitness before: {population.Max(g => g.AverageFitness):F2}");
        }
        
        List<EnemyGenome> newPopulation = new List<EnemyGenome>();
        
        // 1. ELITISMO: Mantém os melhores genomas intactos
        var elites = population
            .OrderByDescending(g => g.AverageFitness)
            .Take(eliteCount)
            .ToList();
        
        foreach (var elite in elites)
        {
            EnemyGenome eliteClone = elite.Clone();
            // Mantém parte do fitness para dar vantagem
            eliteClone.Fitness = elite.AverageFitness * 0.5f;
            eliteClone.TimesUsed = 1;
            newPopulation.Add(eliteClone);
        }
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] Kept {eliteCount} elites. Best elite: {elites[0]}");
        }
        
        // 2. Preenche o resto com crossover e mutação
        while (newPopulation.Count < populationSize)
        {
            EnemyGenome child;
            
            if (Random.value < crossoverRate)
            {
                // Crossover de dois pais
                EnemyGenome parent1 = TournamentSelect();
                EnemyGenome parent2 = TournamentSelect();
                child = EnemyGenome.Crossover(parent1, parent2);
            }
            else
            {
                // Clone de um pai
                child = TournamentSelect().Clone();
            }
            
            // Aplica mutação
            child.Mutate(mutationRate, mutationStrength);
            
            newPopulation.Add(child);
        }
        
        population = newPopulation;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] Evolution complete. New population size: {population.Count}");
        }
    }
    
    private EnemyGenome TournamentSelect()
    {
        EnemyGenome best = null;
        float bestFitness = float.MinValue;
        
        for (int i = 0; i < tournamentSize; i++)
        {
            EnemyGenome candidate = population[Random.Range(0, population.Count)];
            float fitness = candidate.AverageFitness;
            
            // Se nunca foi usado, dá uma chance inicial
            if (candidate.TimesUsed == 0)
            {
                fitness = _averagePopulationFitness > 0 ? _averagePopulationFitness : 1f;
            }
            
            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                best = candidate;
            }
        }
        
        return best ?? population[0];
    }
    
    // ==================== PERSISTÊNCIA ====================
    
    private void SavePopulation()
    {
        PopulationSaveData saveData = new PopulationSaveData
        {
            generation = _generation,
            genomes = population.Select(g => g.ToJson()).ToArray()
        };
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GA] Saved population (Gen {_generation}) to PlayerPrefs");
        }
    }
    
    private void LoadPopulation()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
        
        try
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            PopulationSaveData saveData = JsonUtility.FromJson<PopulationSaveData>(json);
            
            _generation = saveData.generation;
            population.Clear();
            
            foreach (string genomeJson in saveData.genomes)
            {
                population.Add(EnemyGenome.FromJson(genomeJson));
            }
            
            if (debugMode)
            {
                Debug.Log($"🧬 [GA] Loaded population (Gen {_generation}) with {population.Count} genomes");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"🧬 [GA] Failed to load population: {e.Message}. Starting fresh.");
            population.Clear();
            InitializePopulation();
        }
    }
    
    [System.Serializable]
    private class PopulationSaveData
    {
        public int generation;
        public string[] genomes;
    }
    
    // ==================== DEBUG UI ====================
    
    public int Generation => _generation;
    public int PopulationSize => population.Count;
    public float AveragePopulationFitness => _averagePopulationFitness;
    public EnemyGenome GetBestGenome() => population.OrderByDescending(g => g.AverageFitness).FirstOrDefault();
}
