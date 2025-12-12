using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sistema de Algoritmo Genético GLOBAL para Metroidvania.
/// 
/// CONCEITO:
/// - UMA população global que evolui constantemente
/// - Zonas definem apenas multiplicadores de dificuldade BASE
/// - Evolução acontece a cada X inimigos mortos
/// - Rest Points trigeram respawn com genes evoluídos
/// 
/// FLUXO:
/// 1. Jogador mata inimigos → Fitness registado → População evolui
/// 2. Jogador usa Rest Point → Inimigos respawnam com genes atuais
/// 3. Quanto mais joga, mais fortes ficam os inimigos
/// </summary>
public class GlobalGeneticEvolver : MonoBehaviour
{
    public static GlobalGeneticEvolver Instance { get; private set; }
    
    [Header("Population Settings")]
    [Tooltip("Tamanho da população global")]
    [SerializeField] private int populationSize = 20;
    
    [Tooltip("Mortes para triggerar evolução")]
    [SerializeField] private int evolveTriggerCount = 5;
    
    [Header("Selection")]
    [SerializeField] private int eliteCount = 4;
    [SerializeField] private int tournamentSize = 3;
    
    [Header("Genetic Operators")]
    [SerializeField, Range(0f, 0.5f)] private float mutationRate = 0.15f;
    [SerializeField, Range(0f, 0.3f)] private float mutationStrength = 0.2f;
    [SerializeField, Range(0f, 1f)] private float crossoverRate = 0.7f;
    
    [Header("Progression Scaling")]
    [Tooltip("Multiplicador de genes por geração (1.0 = sem escala, 1.02 = +2% por geração)")]
    [SerializeField, Range(1f, 1.1f)] private float generationScaling = 1.02f;
    
    [Tooltip("Limite máximo dos genes (para não ficarem OP)")]
    [SerializeField, Range(0.5f, 1f)] private float maxGeneValue = 0.9f;
    
    [Header("Adaptive Difficulty")]
    [SerializeField] private bool adaptiveDifficulty = true;
    [Tooltip("Se jogador morre muito, reduz dificuldade")]
    [SerializeField, Range(0f, 0.5f)] private float adaptiveStrength = 0.2f;
    
    [Header("Persistence")]
    [SerializeField] private bool persistProgress = true;
    private const string SAVE_KEY = "GlobalGeneticPopulation";
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    [Header("Runtime Stats (Read-Only)")]
    [SerializeField] private int _generation = 0;
    [SerializeField] private int _totalKills = 0;
    [SerializeField] private int _killsSinceEvolution = 0;
    [SerializeField] private float _averageFitness = 0f;
    [SerializeField] private int _playerDeaths = 0;
    
    // População global
    private List<EnemyGenome> population = new List<EnemyGenome>();
    
    // Genomas ativos (inimigos vivos)
    private Dictionary<int, EnemyGenome> activeGenomes = new Dictionary<int, EnemyGenome>();
    
    // Eventos
    public System.Action OnEvolutionComplete;
    public System.Action OnRestPointUsed;
    
    // ==================== PROPRIEDADES PÚBLICAS ====================
    
    public int Generation => _generation;
    public int TotalKills => _totalKills;
    public float AverageFitness => _averageFitness;
    public float CurrentDifficultyMultiplier => 1f + (_generation * (generationScaling - 1f));
    
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
        
        // Subscrever ao evento de mudança de cena
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        InitializePopulation();
    }
    
    private void OnDestroy()
    {
        // Limpar subscrição ao destruir
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Salvar progresso antes de destruir
        if (persistProgress && Instance == this)
        {
            SaveProgress();
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Scene Changed: {scene.name} | Mode: {mode}");
            Debug.Log($"🧬 [GlobalGA] Generation: {_generation} | Population: {population.Count} | Active Genomes: {activeGenomes.Count}");
        }
        
        // Limpar genomas ativos (inimigos da cena anterior foram destruídos)
        activeGenomes.Clear();
        
        // Salvar progresso automaticamente ao mudar de cena
        if (persistProgress)
        {
            SaveProgress();
        }
        
        // Resetar contagem de mortes do jogador por cena (opcional)
        // _playerDeaths = 0;
    }
    
    private void Start()
    {
        if (persistProgress)
        {
            LoadProgress();
        }
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Started at Generation {_generation}, Avg Fitness: {_averageFitness:F2}");
        }
    }
    
    private void OnApplicationQuit()
    {
        if (persistProgress)
        {
            SaveProgress();
        }
    }
    
    // ==================== API PRINCIPAL ====================
    
    /// <summary>
    /// Obtém um genoma para um novo inimigo.
    /// Aplica o multiplicador de zona se fornecido.
    /// </summary>
    public EnemyGenome GetGenome(int enemyId, float zoneDifficultyMult = 1f)
    {
        if (population.Count == 0) InitializePopulation();
        
        // Seleção por torneio
        EnemyGenome selected = TournamentSelect();
        EnemyGenome genome = selected.Clone();
        
        // Aplica scaling de geração (progressão natural)
        float genMult = CurrentDifficultyMultiplier;
        genome.healthGene = Mathf.Min(genome.healthGene * genMult, maxGeneValue);
        genome.damageGene = Mathf.Min(genome.damageGene * genMult, maxGeneValue);
        genome.movementSpeedGene = Mathf.Min(genome.movementSpeedGene * genMult, maxGeneValue);
        genome.aggressivenessGene = Mathf.Min(genome.aggressivenessGene * genMult, maxGeneValue);
        
        // Aplica multiplicador de zona
        if (zoneDifficultyMult != 1f)
        {
            genome.healthGene = Mathf.Min(genome.healthGene * zoneDifficultyMult, maxGeneValue);
            genome.damageGene = Mathf.Min(genome.damageGene * zoneDifficultyMult, maxGeneValue);
        }
        
        // Pequena variação
        genome.Mutate(mutationRate * 0.3f, mutationStrength * 0.3f);
        
        // Regista como ativo
        activeGenomes[enemyId] = genome;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Genome assigned (Gen {_generation}, Zone×{zoneDifficultyMult:F1}): {genome}");
        }
        
        return genome;
    }
    
    /// <summary>
    /// Regista morte de um inimigo e o seu fitness.
    /// </summary>
    public void RegisterKill(int enemyId, float damageDealt, float survivalTime, bool killedByPlayer = true)
    {
        if (!activeGenomes.TryGetValue(enemyId, out EnemyGenome genome))
            return;
        
        // Remove dos ativos primeiro
        activeGenomes.Remove(enemyId);
        
        // Se não foi morto pelo jogador (ex: mudança de cena), não conta para evolução
        if (!killedByPlayer)
        {
            if (debugMode)
            {
                Debug.Log($"🧬 [GlobalGA] Enemy destroyed (not killed by player). Not counting towards evolution.");
            }
            return;
        }
        
        // Calcula fitness
        float fitness = CalculateFitness(damageDealt, survivalTime, killedByPlayer);
        
        // Ajuste adaptativo
        if (adaptiveDifficulty)
        {
            fitness = ApplyAdaptiveAdjustment(fitness, genome);
        }
        
        // Atualiza população
        UpdatePopulationFitness(genome, fitness);
        
        // Incrementa contadores (só para mortes reais)
        _totalKills++;
        _killsSinceEvolution++;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Kill #{_totalKills}. Fitness: {fitness:F1}. Progress: {_killsSinceEvolution}/{evolveTriggerCount}");
        }
        
        // Evolui se necessário
        if (_killsSinceEvolution >= evolveTriggerCount)
        {
            Evolve();
        }
    }
    
    /// <summary>
    /// Chamado quando o jogador usa um Rest Point.
    /// Pode forçar evolução e notifica sistemas.
    /// </summary>
    public void OnRestPoint()
    {
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Rest Point used. Generation: {_generation}");
        }
        
        // Opcional: força evolução se houver kills pendentes
        if (_killsSinceEvolution > 0)
        {
            Evolve();
        }
        
        // Salva progresso
        if (persistProgress)
        {
            SaveProgress();
        }
        
        // Notifica outros sistemas (para respawn de inimigos)
        OnRestPointUsed?.Invoke();
    }
    
    /// <summary>
    /// Regista morte do jogador (para dificuldade adaptativa).
    /// </summary>
    public void RegisterPlayerDeath()
    {
        _playerDeaths++;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Player death #{_playerDeaths}");
        }
        
        // Se adaptativo, reduz ligeiramente a dificuldade
        if (adaptiveDifficulty && _playerDeaths > 2)
        {
            ReduceDifficulty();
        }
    }
    
    /// <summary>
    /// Reseta o jogador (nova run).
    /// </summary>
    public void ResetPlayerDeaths()
    {
        _playerDeaths = 0;
    }
    
    /// <summary>
    /// Reseta toda a progressão genética.
    /// </summary>
    public void ResetAll()
    {
        population.Clear();
        activeGenomes.Clear();
        _generation = 0;
        _totalKills = 0;
        _killsSinceEvolution = 0;
        _playerDeaths = 0;
        _averageFitness = 0;
        
        InitializePopulation();
        
        if (persistProgress)
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
        }
        
        Debug.Log("🧬 [GlobalGA] Full reset!");
    }
    
    /// <summary>
    /// Obtém o melhor genoma atual.
    /// </summary>
    public EnemyGenome GetBestGenome()
    {
        return population.OrderByDescending(g => g.AverageFitness).FirstOrDefault();
    }
    
    // ==================== EVOLUÇÃO ====================
    
    private void Evolve()
    {
        _generation++;
        _killsSinceEvolution = 0;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] ══════ EVOLUTION TO GEN {_generation} ══════");
            Debug.Log($"🧬 [GlobalGA] Best fitness: {population.Max(g => g.AverageFitness):F2}");
        }
        
        List<EnemyGenome> newPopulation = new List<EnemyGenome>();
        
        // Elitismo: melhores passam direto
        var elites = population
            .OrderByDescending(g => g.AverageFitness)
            .Take(eliteCount)
            .ToList();
        
        foreach (var elite in elites)
        {
            var clone = elite.Clone();
            clone.Fitness = elite.AverageFitness * 0.5f;
            clone.TimesUsed = 1;
            newPopulation.Add(clone);
        }
        
        // Preenche com crossover e mutação
        while (newPopulation.Count < populationSize)
        {
            EnemyGenome child;
            
            if (Random.value < crossoverRate && population.Count >= 2)
            {
                var p1 = TournamentSelect();
                var p2 = TournamentSelect();
                child = EnemyGenome.Crossover(p1, p2);
            }
            else
            {
                child = TournamentSelect().Clone();
            }
            
            child.Mutate(mutationRate, mutationStrength);
            newPopulation.Add(child);
        }
        
        population = newPopulation;
        _averageFitness = population.Average(g => g.AverageFitness);
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] New avg fitness: {_averageFitness:F2}");
        }
        
        OnEvolutionComplete?.Invoke();
    }
    
    private void InitializePopulation()
    {
        population.Clear();
        
        for (int i = 0; i < populationSize; i++)
        {
            // Começa com genes baixos (jogo fácil no início)
            var genome = new EnemyGenome
            {
                healthGene = Random.Range(0.2f, 0.5f),
                damageGene = Random.Range(0.2f, 0.4f),
                attackSpeedGene = Random.Range(0.3f, 0.5f),
                movementSpeedGene = Random.Range(0.3f, 0.5f),
                aggressionRangeGene = Random.Range(0.3f, 0.5f),
                aggressivenessGene = Random.Range(0.2f, 0.4f),
                meleeResistanceGene = Random.Range(0f, 0.2f),
                rangedResistanceGene = Random.Range(0f, 0.2f)
            };
            population.Add(genome);
        }
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Initialized with {populationSize} genomes (easy start)");
        }
    }
    
    private float CalculateFitness(float damageDealt, float survivalTime, bool killedByPlayer)
    {
        float fitness = (damageDealt * 10f) + (survivalTime * 0.5f);
        
        if (killedByPlayer && damageDealt > 0)
        {
            fitness *= 1.2f; // Bónus por ser ameaça real
        }
        
        return fitness;
    }
    
    private float ApplyAdaptiveAdjustment(float fitness, EnemyGenome genome)
    {
        // Se jogador está a morrer muito, penaliza genomas agressivos
        if (_playerDeaths > 3)
        {
            float penalty = genome.aggressivenessGene * adaptiveStrength;
            fitness *= (1f - penalty);
        }
        // Se jogador está a dominar, bónus para agressivos
        else if (_playerDeaths == 0 && _totalKills > 30)
        {
            float bonus = genome.aggressivenessGene * adaptiveStrength;
            fitness *= (1f + bonus);
        }
        
        return fitness;
    }
    
    private void ReduceDifficulty()
    {
        // Reduz ligeiramente os genes mais altos
        foreach (var genome in population)
        {
            genome.healthGene *= 0.95f;
            genome.damageGene *= 0.95f;
            genome.aggressivenessGene *= 0.9f;
        }
        
        if (debugMode)
        {
            Debug.Log("🧬 [GlobalGA] Difficulty reduced due to player deaths");
        }
    }
    
    private EnemyGenome TournamentSelect()
    {
        EnemyGenome best = null;
        float bestFitness = float.MinValue;
        
        for (int i = 0; i < tournamentSize; i++)
        {
            var candidate = population[Random.Range(0, population.Count)];
            float fitness = candidate.TimesUsed > 0 ? candidate.AverageFitness : _averageFitness;
            
            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                best = candidate;
            }
        }
        
        return best ?? population[0];
    }
    
    private void UpdatePopulationFitness(EnemyGenome usedGenome, float fitness)
    {
        float bestDiff = float.MaxValue;
        EnemyGenome mostSimilar = null;
        
        foreach (var genome in population)
        {
            float diff = Mathf.Abs(usedGenome.healthGene - genome.healthGene) +
                        Mathf.Abs(usedGenome.damageGene - genome.damageGene) +
                        Mathf.Abs(usedGenome.movementSpeedGene - genome.movementSpeedGene);
            
            if (diff < bestDiff)
            {
                bestDiff = diff;
                mostSimilar = genome;
            }
        }
        
        if (mostSimilar != null)
        {
            mostSimilar.Fitness += fitness;
            mostSimilar.TimesUsed++;
        }
        
        _averageFitness = population.Average(g => g.AverageFitness);
    }
    
    // ==================== PERSISTÊNCIA ====================
    
    private void SaveProgress()
    {
        var data = new SaveData
        {
            generation = _generation,
            totalKills = _totalKills,
            playerDeaths = _playerDeaths,
            killsSinceEvolution = _killsSinceEvolution, // Salvar progresso parcial!
            genomes = population.Select(g => g.ToJson()).ToArray()
        };
        
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Saved: Gen {_generation}, Partial: {_killsSinceEvolution}/{evolveTriggerCount}");
        }
    }
    
    private void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
        
        try
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SAVE_KEY));
            _generation = data.generation;
            _totalKills = data.totalKills;
            _playerDeaths = data.playerDeaths;
            _killsSinceEvolution = data.killsSinceEvolution; // Carregar progresso parcial!
            
            population.Clear();
            foreach (var json in data.genomes)
            {
                population.Add(EnemyGenome.FromJson(json));
            }
            
            _averageFitness = population.Average(g => g.AverageFitness);
            
            if (debugMode)
            {
                Debug.Log($"🧬 [GlobalGA] Loaded: Gen {_generation}, Kills {_totalKills}, Partial: {_killsSinceEvolution}/{evolveTriggerCount}");
            }
        }
        catch
        {
            Debug.LogWarning("🧬 [GlobalGA] Failed to load, starting fresh");
            InitializePopulation();
        }
    }
    
    [System.Serializable]
    private class SaveData
    {
        public int generation;
        public int totalKills;
        public int playerDeaths;
        public int killsSinceEvolution; // Novo campo!
        public string[] genomes;
    }
}
