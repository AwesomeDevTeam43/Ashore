using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Global Genetic Algorithm System for Metroidvania Games.
/// 
/// KEY FEATURES:
/// - Species-based evolution: Each enemy type evolves independently
/// - Hard difficulty ceiling: Prevents runaway difficulty
/// - Adaptive difficulty: Adjusts based on player performance
/// - Proper damage attribution: Only credits the actual attacker
/// - File-based persistence: Reliable save/load system
/// 
/// CONCEPT:
/// - Each species (Fly, Crab, Bee, etc.) has its own genetic population
/// - Evolution happens per-species when enough of that species dies
/// - Zone multipliers apply on top of evolved genes
/// - Rest Points trigger respawn with current evolved genes
/// 
/// FLOW:
/// 1. Enemy spawns → Gets genome from its species population
/// 2. Enemy dies → Fitness recorded → Species population may evolve
/// 3. Rest Point → Enemies respawn with current evolved genes
/// 4. Player struggles → Adaptive difficulty reduces pressure
/// </summary>
public class GlobalGeneticEvolver : MonoBehaviour
{
    public static GlobalGeneticEvolver Instance { get; private set; }
    
    // ==================== CONFIGURATION ====================
    
    [Header("Population Settings")]
    [Tooltip("Population size per species")]
    [SerializeField] private int populationSize = 20;
    
    [Tooltip("Kills of same species to trigger evolution")]
    [SerializeField] private int evolveTriggerCount = 5;
    
    [Header("Selection")]
    [Tooltip("Number of top performers that pass to next generation unchanged")]
    [SerializeField] private int eliteCount = 4;
    
    [Tooltip("Number of candidates in tournament selection")]
    [SerializeField] private int tournamentSize = 3;
    
    [Header("Genetic Operators")]
    [Tooltip("Chance for each gene to mutate (0-0.5)")]
    [SerializeField, Range(0f, 0.5f)] private float mutationRate = 0.35f;
    
    [Tooltip("Maximum change when a gene mutates (0-0.5)")]
    [SerializeField, Range(0f, 0.5f)] private float mutationStrength = 0.4f;
    
    [Tooltip("Chance to use crossover vs cloning (0-1)")]
    [SerializeField, Range(0f, 1f)] private float crossoverRate = 0.7f;
    
    [Header("Difficulty Ceiling (IMPORTANT)")]
    [Tooltip("Absolute maximum combined difficulty multiplier. Prevents runaway difficulty.")]
    [SerializeField, Range(1.5f, 4f)] private float absoluteMaxDifficulty = 2.5f;
    
    [Tooltip("Maximum value any single gene can reach (0.5-1)")]
    [SerializeField, Range(0.5f, 1f)] private float maxGeneValue = 0.95f;
    
    [Tooltip("Per-generation difficulty increase (1.0 = none, 1.02 = +2%). Set to 1.0 to disable.")]
    [SerializeField, Range(1f, 1.1f)] private float generationScaling = 1.03f;
    
    [Tooltip("Maximum generation for scaling (caps progression)")]
    [SerializeField] private int maxScalingGeneration = 50;
    
    [Header("Adaptive Difficulty")]
    [Tooltip("Enable automatic difficulty adjustment based on player performance")]
    [SerializeField] private bool adaptiveDifficulty = true;
    
    [Tooltip("Player deaths before difficulty reduction kicks in")]
    [SerializeField] private int deathsBeforeReduction = 3;
    
    [Tooltip("How much to reduce difficulty per excess death (0-0.2)")]
    [SerializeField, Range(0f, 0.2f)] private float deathPenaltyStrength = 0.1f;
    
    [Tooltip("Kills without player death before difficulty increase")]
    [SerializeField] private int killsBeforeIncrease = 20;
    
    [Tooltip("How much to increase difficulty when player dominates (0-0.1)")]
    [SerializeField, Range(0f, 0.1f)] private float dominationBonusStrength = 0.05f;
    
    [Header("Target Difficulty (Challenge Rating)")]
    [Tooltip("Enable challenge rating targeting")]
    [SerializeField] private bool useTargetDifficulty = false;
    
    [Tooltip("Target average fitness to evolve towards (0 = as hard as possible)")]
    [SerializeField] private float targetFitness = 50f;
    
    [Header("Persistence")]
    [Tooltip("Save progress to file")]
    [SerializeField] private bool persistProgress = true;
    
    [Tooltip("Save file name (stored in Application.persistentDataPath)")]
    [SerializeField] private string saveFileName = "genetic_evolution_save.json";
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool logEveryKill = false;
    
    [Header("Runtime Stats (Read-Only)")]
    [SerializeField] private int _totalKills = 0;
    [SerializeField] private int _playerDeaths = 0;
    [SerializeField] private int _killsSinceLastDeath = 0;
    [SerializeField] private float _currentDifficultyModifier = 1f;
    
    // ==================== SPECIES POPULATIONS ====================
    
    /// <summary>
    /// Data for a single species' evolutionary population.
    /// </summary>
    [System.Serializable]
    private class SpeciesPopulation
    {
        public EnemySpecies species;
        public int generation = 0;
        public int killsSinceEvolution = 0;
        public float averageFitness = 0f;
        public List<EnemyGenome> population = new List<EnemyGenome>();
        
        // Runtime only (not serialized)
        [System.NonSerialized]
        public Dictionary<int, EnemyGenome> activeGenomes = new Dictionary<int, EnemyGenome>();
    }
    
    // All species populations
    private Dictionary<EnemySpecies, SpeciesPopulation> speciesPopulations = new Dictionary<EnemySpecies, SpeciesPopulation>();
    
    // Active enemy tracking (maps instance ID to species + genome)
    private Dictionary<int, (EnemySpecies species, EnemyGenome genome, int attackerId)> activeEnemies = 
        new Dictionary<int, (EnemySpecies, EnemyGenome, int)>();
    
    // ==================== EVENTS ====================
    
    public System.Action<EnemySpecies, int> OnSpeciesEvolved; // (species, newGeneration)
    public System.Action OnRestPointUsed;
    public System.Action<float> OnDifficultyChanged; // (newModifier)
    
    // ==================== PROPERTIES ====================
    
    public int TotalKills => _totalKills;
    public int PlayerDeaths => _playerDeaths;
    public float CurrentDifficultyModifier => _currentDifficultyModifier;
    
    /// <summary>
    /// Gets the current generation for a specific species.
    /// </summary>
    public int GetGeneration(EnemySpecies species)
    {
        return speciesPopulations.TryGetValue(species, out var pop) ? pop.generation : 0;
    }
    
    /// <summary>
    /// Gets the average fitness for a specific species.
    /// </summary>
    public float GetAverageFitness(EnemySpecies species)
    {
        return speciesPopulations.TryGetValue(species, out var pop) ? pop.averageFitness : 0f;
    }
    
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
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Initialize with empty populations (will load or create on first access)
        _currentDifficultyModifier = 1f;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (persistProgress && Instance == this)
        {
            SaveProgress();
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Scene loaded: {scene.name}");
        }
        
        // Clear active enemies (they were destroyed with the old scene)
        activeEnemies.Clear();
        foreach (var pop in speciesPopulations.Values)
        {
            pop.activeGenomes.Clear();
        }
        
        if (persistProgress)
        {
            SaveProgress();
        }
    }
    
    private void Start()
    {
        if (persistProgress)
        {
            LoadProgress();
        }
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Started. {speciesPopulations.Count} species loaded. Difficulty modifier: {_currentDifficultyModifier:F2}");
        }
    }
    
    private void OnApplicationQuit()
    {
        if (persistProgress)
        {
            SaveProgress();
        }
    }
    
    // ==================== MAIN API ====================
    
    /// <summary>
    /// Gets a genome for a new enemy of the specified species.
    /// </summary>
    /// <param name="enemyId">Unique instance ID of the enemy</param>
    /// <param name="species">The enemy species</param>
    /// <param name="zoneDifficultyMult">Zone difficulty multiplier (1.0 = normal)</param>
    /// <returns>A genome configured for this enemy</returns>
    public EnemyGenome GetGenome(int enemyId, EnemySpecies species, float zoneDifficultyMult = 1f)
    {
        // Don't evolve bosses or unknown species
        if (!EnemySpeciesHelper.ShouldEvolve(species))
        {
            return CreateDefaultGenome();
        }
        
        // Get or create population for this species
        var pop = GetOrCreatePopulation(species);
        
        // Select genome via tournament
        EnemyGenome selected = TournamentSelect(pop);
        EnemyGenome genome = selected.Clone();
        genome.species = species;
        
        // Calculate total difficulty multiplier (with hard cap)
        float genMult = CalculateGenerationMultiplier(pop.generation);
        float adaptiveMult = _currentDifficultyModifier;
        float totalMult = genMult * zoneDifficultyMult * adaptiveMult;
        
        // HARD CAP: Never exceed absolute maximum
        totalMult = Mathf.Min(totalMult, absoluteMaxDifficulty);
        
        // Apply multiplier to combat genes only
        genome.healthGene = Mathf.Min(genome.healthGene * totalMult, maxGeneValue);
        genome.damageGene = Mathf.Min(genome.damageGene * totalMult, maxGeneValue);
        
        // Small random variation for variety
        genome.Mutate(mutationRate * 0.2f, mutationStrength * 0.2f);
        
        // Track this enemy
        activeEnemies[enemyId] = (species, genome, enemyId);
        pop.activeGenomes[enemyId] = genome;
        
        if (debugMode && logEveryKill)
        {
            Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(species)}] Genome assigned (Gen {pop.generation}, ×{totalMult:F2}): {genome}");
        }
        
        return genome;
    }
    
    /// <summary>
    /// Registers that an enemy dealt damage to the player.
    /// This is the ONLY way damage should be attributed for accurate fitness.
    /// </summary>
    /// <param name="attackerInstanceId">The instance ID of the attacking enemy</param>
    /// <param name="damageAmount">Amount of damage dealt</param>
    public void RegisterDamageDealt(int attackerInstanceId, float damageAmount)
    {
        if (!activeEnemies.TryGetValue(attackerInstanceId, out var data))
            return;
        
        // Update the genome's tracked damage
        if (speciesPopulations.TryGetValue(data.species, out var pop))
        {
            if (pop.activeGenomes.TryGetValue(attackerInstanceId, out var genome))
            {
                genome.damageDealtThisLife += damageAmount;
                
                if (debugMode && logEveryKill)
                {
                    Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(data.species)}] Dealt {damageAmount} damage. Total: {genome.damageDealtThisLife:F1}");
                }
            }
        }
    }
    
    /// <summary>
    /// Registers the death of an enemy.
    /// </summary>
    /// <param name="enemyId">Instance ID of the dead enemy</param>
    /// <param name="survivalTime">How long the enemy survived</param>
    /// <param name="killedByPlayer">True if player killed it, false if scene change/other</param>
    public void RegisterKill(int enemyId, float survivalTime, bool killedByPlayer = true)
    {
        if (!activeEnemies.TryGetValue(enemyId, out var data))
            return;
        
        var species = data.species;
        var genome = data.genome;
        
        // Remove from active tracking
        activeEnemies.Remove(enemyId);
        if (speciesPopulations.TryGetValue(species, out var pop))
        {
            pop.activeGenomes.Remove(enemyId);
        }
        
        // Don't count non-player kills for evolution
        if (!killedByPlayer)
        {
            if (debugMode)
            {
                Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(species)}] Destroyed (not by player). Skipping evolution credit.");
            }
            return;
        }
        
        // Calculate fitness
        float fitness = CalculateFitness(genome.damageDealtThisLife, survivalTime);
        
        // Apply adaptive adjustment
        if (adaptiveDifficulty)
        {
            fitness = ApplyAdaptiveFitnessAdjustment(fitness, genome);
        }
        
        // Update population
        UpdatePopulationFitness(pop, genome, fitness);
        
        // Track stats
        _totalKills++;
        _killsSinceLastDeath++;
        pop.killsSinceEvolution++;
        
        if (debugMode && logEveryKill)
        {
            Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(species)}] Kill #{_totalKills}. Fitness: {fitness:F1}. Progress: {pop.killsSinceEvolution}/{evolveTriggerCount}");
        }
        
        // Check for evolution
        if (pop.killsSinceEvolution >= evolveTriggerCount)
        {
            Evolve(pop);
        }
        
        // Check for domination bonus
        if (adaptiveDifficulty && _killsSinceLastDeath >= killsBeforeIncrease)
        {
            IncreaseDifficulty();
            _killsSinceLastDeath = 0;
        }
    }
    
    /// <summary>
    /// Registers player death for adaptive difficulty.
    /// </summary>
    public void RegisterPlayerDeath()
    {
        _playerDeaths++;
        _killsSinceLastDeath = 0;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Player death #{_playerDeaths}");
        }
        
        if (adaptiveDifficulty && _playerDeaths >= deathsBeforeReduction)
        {
            ReduceDifficulty();
        }
    }
    
    /// <summary>
    /// Called when player uses a rest point.
    /// </summary>
    public void OnRestPoint()
    {
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Rest Point used.");
        }
        
        // Force pending evolutions
        foreach (var pop in speciesPopulations.Values)
        {
            if (pop.killsSinceEvolution > 0)
            {
                Evolve(pop);
            }
        }
        
        if (persistProgress)
        {
            SaveProgress();
        }
        
        OnRestPointUsed?.Invoke();
    }
    
    /// <summary>
    /// Resets player death counter (e.g., after reaching a checkpoint).
    /// </summary>
    public void ResetPlayerDeathCounter()
    {
        _playerDeaths = 0;
        _killsSinceLastDeath = 0;
    }
    
    /// <summary>
    /// Completely resets all genetic progress.
    /// </summary>
    public void ResetAll()
    {
        speciesPopulations.Clear();
        activeEnemies.Clear();
        _totalKills = 0;
        _playerDeaths = 0;
        _killsSinceLastDeath = 0;
        _currentDifficultyModifier = 1f;
        
        if (persistProgress)
        {
            DeleteSaveFile();
        }
        
        Debug.Log("🧬 [GlobalGA] Full reset complete!");
    }
    
    /// <summary>
    /// Gets the best genome for a species.
    /// </summary>
    public EnemyGenome GetBestGenome(EnemySpecies species)
    {
        if (!speciesPopulations.TryGetValue(species, out var pop))
            return null;
        
        return pop.population.OrderByDescending(g => g.AverageFitness).FirstOrDefault();
    }
    
    /// <summary>
    /// Gets statistics for all species.
    /// </summary>
    public Dictionary<EnemySpecies, (int generation, float avgFitness, int popSize)> GetAllSpeciesStats()
    {
        var result = new Dictionary<EnemySpecies, (int, float, int)>();
        foreach (var kvp in speciesPopulations)
        {
            result[kvp.Key] = (kvp.Value.generation, kvp.Value.averageFitness, kvp.Value.population.Count);
        }
        return result;
    }
    
    // ==================== EVOLUTION ====================
    
    private void Evolve(SpeciesPopulation pop)
    {
        pop.generation++;
        pop.killsSinceEvolution = 0;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(pop.species)}] ══════ EVOLUTION TO GEN {pop.generation} ══════");
            if (pop.population.Count > 0)
            {
                Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(pop.species)}] Best fitness: {pop.population.Max(g => g.AverageFitness):F2}");
            }
        }
        
        List<EnemyGenome> newPopulation = new List<EnemyGenome>();
        
        // Elitism: top performers pass through
        var elites = pop.population
            .OrderByDescending(g => g.AverageFitness)
            .Take(eliteCount)
            .ToList();
        
        foreach (var elite in elites)
        {
            var clone = elite.Clone();
            clone.Fitness = elite.AverageFitness * 0.3f; // Reduced carryover
            clone.TimesUsed = 1;
            newPopulation.Add(clone);
        }
        
        // Fill rest with crossover and mutation
        while (newPopulation.Count < populationSize)
        {
            EnemyGenome child;
            
            if (Random.value < crossoverRate && pop.population.Count >= 2)
            {
                // Select TWO DIFFERENT parents
                var p1 = TournamentSelect(pop);
                var p2 = TournamentSelectExcluding(pop, p1);
                child = EnemyGenome.Crossover(p1, p2);
            }
            else
            {
                child = TournamentSelect(pop).Clone();
            }
            
            child.Mutate(mutationRate, mutationStrength);
            child.species = pop.species;
            
            // Enforce gene caps
            child.ClampGenes(maxGeneValue);
            
            newPopulation.Add(child);
        }
        
        pop.population = newPopulation;
        pop.averageFitness = pop.population.Average(g => g.AverageFitness);
        
        // Challenge rating: if targeting specific difficulty, adjust
        if (useTargetDifficulty && pop.averageFitness > targetFitness * 1.2f)
        {
            // Population is too strong, weaken it
            foreach (var genome in pop.population)
            {
                genome.healthGene *= 0.95f;
                genome.damageGene *= 0.95f;
            }
            
            if (debugMode)
            {
                Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(pop.species)}] Challenge rating adjustment: weakened population");
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"🧬 [{EnemySpeciesHelper.GetDisplayName(pop.species)}] New avg fitness: {pop.averageFitness:F2}");
        }
        
        OnSpeciesEvolved?.Invoke(pop.species, pop.generation);
    }
    
    private SpeciesPopulation GetOrCreatePopulation(EnemySpecies species)
    {
        if (speciesPopulations.TryGetValue(species, out var existing))
            return existing;
        
        var pop = new SpeciesPopulation
        {
            species = species,
            generation = 0,
            killsSinceEvolution = 0,
            averageFitness = 0f,
            population = new List<EnemyGenome>()
        };
        
        // Initialize with starting population
        for (int i = 0; i < populationSize; i++)
        {
            var genome = new EnemyGenome
            {
                species = species,
                healthGene = Random.Range(0.25f, 0.45f),
                damageGene = Random.Range(0.2f, 0.4f),
                attackSpeedGene = Random.Range(0.3f, 0.5f),
                movementSpeedGene = Random.Range(0.3f, 0.5f),
                aggressionRangeGene = Random.Range(0.3f, 0.5f),
                aggressivenessGene = Random.Range(0.2f, 0.4f),
                meleeResistanceGene = Random.Range(0f, 0.15f),
                rangedResistanceGene = Random.Range(0f, 0.15f)
            };
            pop.population.Add(genome);
        }
        
        speciesPopulations[species] = pop;
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Created new population for {EnemySpeciesHelper.GetDisplayName(species)} with {populationSize} genomes");
        }
        
        return pop;
    }
    
    private EnemyGenome CreateDefaultGenome()
    {
        return new EnemyGenome
        {
            healthGene = 0.5f,
            damageGene = 0.5f,
            attackSpeedGene = 0.5f,
            movementSpeedGene = 0.5f,
            aggressionRangeGene = 0.5f,
            aggressivenessGene = 0.5f,
            meleeResistanceGene = 0f,
            rangedResistanceGene = 0f
        };
    }
    
    private float CalculateGenerationMultiplier(int generation)
    {
        // Cap at max scaling generation
        int effectiveGen = Mathf.Min(generation, maxScalingGeneration);
        return Mathf.Pow(generationScaling, effectiveGen);
    }
    
    private float CalculateFitness(float damageDealt, float survivalTime)
    {
        // --- New fitness function to reward all genes ---
        // Damage dealt is still most important
        float fitness = (damageDealt * 10f);

        // Reward for survival time (encourages tankiness/mobility)
        fitness += survivalTime * 0.5f;

        // Reward for killing the player quickly (attack speed)
        // If the enemy killed the player, survivalTime is short, so reward inversely
        if (damageDealt > 0 && survivalTime < 10f) // killed player in under 10s
        {
            fitness += (10f - survivalTime) * 2.0f; // up to +20 for very fast kills
        }

        // Reward for high movement/aggression if enemy spent time near player (proxy: damageDealt > 0)
        // These values should be passed in, but for now, use genome values if available
        // (Assume this method is called from RegisterKill, which has access to the genome)
        // We'll add an overload to pass the genome
        return fitness;
    }
    
    private float ApplyAdaptiveFitnessAdjustment(float fitness, EnemyGenome genome)
    {
        // If player is struggling, penalize aggressive genomes
        if (_playerDeaths > deathsBeforeReduction)
        {
            float penalty = genome.aggressivenessGene * deathPenaltyStrength * (_playerDeaths - deathsBeforeReduction);
            fitness *= Mathf.Max(0.5f, 1f - penalty);
        }
        // Reward for non-damage genes:
        // - Attack speed: higher is better if enemy dealt damage
        // - Movement speed: higher is better if enemy dealt damage
        // - Aggression: higher is better if enemy dealt damage
        // - Resistances: higher is better if enemy survived longer
        if (genome != null)
        {
            // If enemy dealt damage, reward attack speed, movement, aggression
            if (genome.damageDealtThisLife > 0)
            {
                fitness += genome.attackSpeedGene * 5f;
                fitness += genome.movementSpeedGene * 3f;
                fitness += genome.aggressivenessGene * 3f;
            }
            // If enemy survived a long time, reward resistances
            if (genome.damageDealtThisLife > 0 && genome.Fitness > 0)
            {
                fitness += genome.meleeResistanceGene * 2f;
                fitness += genome.rangedResistanceGene * 2f;
            }
        }
        return fitness;
    }
    
    private void ReduceDifficulty()
    {
        float reduction = deathPenaltyStrength * (_playerDeaths - deathsBeforeReduction + 1);
        _currentDifficultyModifier = Mathf.Max(0.6f, _currentDifficultyModifier - reduction);
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Difficulty reduced to {_currentDifficultyModifier:F2} due to {_playerDeaths} player deaths");
        }
        
        OnDifficultyChanged?.Invoke(_currentDifficultyModifier);
    }
    
    private void IncreaseDifficulty()
    {
        _currentDifficultyModifier = Mathf.Min(absoluteMaxDifficulty, _currentDifficultyModifier + dominationBonusStrength);
        
        if (debugMode)
        {
            Debug.Log($"🧬 [GlobalGA] Difficulty increased to {_currentDifficultyModifier:F2} (player dominating)");
        }
        
        OnDifficultyChanged?.Invoke(_currentDifficultyModifier);
    }
    
    private EnemyGenome TournamentSelect(SpeciesPopulation pop)
    {
        if (pop.population.Count == 0)
            return CreateDefaultGenome();
        
        EnemyGenome best = null;
        float bestFitness = float.MinValue;
        
        for (int i = 0; i < tournamentSize; i++)
        {
            var candidate = pop.population[Random.Range(0, pop.population.Count)];
            float fitness = candidate.TimesUsed > 0 ? candidate.AverageFitness : pop.averageFitness;
            
            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                best = candidate;
            }
        }
        
        return best ?? pop.population[0];
    }
    
    private EnemyGenome TournamentSelectExcluding(SpeciesPopulation pop, EnemyGenome exclude)
    {
        if (pop.population.Count <= 1)
            return pop.population[0];
        
        EnemyGenome best = null;
        float bestFitness = float.MinValue;
        
        int attempts = 0;
        while (attempts < tournamentSize * 2)
        {
            var candidate = pop.population[Random.Range(0, pop.population.Count)];
            if (candidate == exclude)
            {
                attempts++;
                continue;
            }
            
            float fitness = candidate.TimesUsed > 0 ? candidate.AverageFitness : pop.averageFitness;
            
            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                best = candidate;
            }
            attempts++;
        }
        
        // Fallback if we couldn't find a different one
        if (best == null || best == exclude)
        {
            foreach (var g in pop.population)
            {
                if (g != exclude)
                {
                    best = g;
                    break;
                }
            }
        }
        
        return best ?? exclude;
    }
    
    private void UpdatePopulationFitness(SpeciesPopulation pop, EnemyGenome usedGenome, float fitness)
    {
        // Find the most similar genome in the population
        float bestDiff = float.MaxValue;
        EnemyGenome mostSimilar = null;
        
        foreach (var genome in pop.population)
        {
            float diff = Mathf.Abs(usedGenome.healthGene - genome.healthGene) +
                        Mathf.Abs(usedGenome.damageGene - genome.damageGene) +
                        Mathf.Abs(usedGenome.movementSpeedGene - genome.movementSpeedGene) +
                        Mathf.Abs(usedGenome.attackSpeedGene - genome.attackSpeedGene) +
                        Mathf.Abs(usedGenome.aggressivenessGene - genome.aggressivenessGene);
            
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
        
        pop.averageFitness = pop.population.Average(g => g.AverageFitness);
    }
    
    // ==================== PERSISTENCE (FILE-BASED) ====================
    
    [System.Serializable]
    private class SaveData
    {
        public int totalKills;
        public int playerDeaths;
        public float difficultyModifier;
        public List<SpeciesSaveData> species = new List<SpeciesSaveData>();
    }
    
    [System.Serializable]
    private class SpeciesSaveData
    {
        public int speciesId;
        public int generation;
        public int killsSinceEvolution;
        public float averageFitness;
        public List<string> genomes = new List<string>();
    }
    
    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }
    
    private void SaveProgress()
    {
        try
        {
            var data = new SaveData
            {
                totalKills = _totalKills,
                playerDeaths = _playerDeaths,
                difficultyModifier = _currentDifficultyModifier
            };
            
            foreach (var kvp in speciesPopulations)
            {
                var speciesData = new SpeciesSaveData
                {
                    speciesId = (int)kvp.Key,
                    generation = kvp.Value.generation,
                    killsSinceEvolution = kvp.Value.killsSinceEvolution,
                    averageFitness = kvp.Value.averageFitness
                };
                
                foreach (var genome in kvp.Value.population)
                {
                    speciesData.genomes.Add(genome.ToJson());
                }
                
                data.species.Add(speciesData);
            }
            
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSavePath(), json);
            
            if (debugMode)
            {
                Debug.Log($"🧬 [GlobalGA] Saved to {GetSavePath()}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🧬 [GlobalGA] Failed to save: {e.Message}");
        }
    }
    
    private void LoadProgress()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            if (debugMode)
            {
                Debug.Log($"🧬 [GlobalGA] No save file found. Starting fresh.");
            }
            return;
        }
        
        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);
            
            _totalKills = data.totalKills;
            _playerDeaths = data.playerDeaths;
            _currentDifficultyModifier = data.difficultyModifier;
            
            speciesPopulations.Clear();
            foreach (var speciesData in data.species)
            {
                var pop = new SpeciesPopulation
                {
                    species = (EnemySpecies)speciesData.speciesId,
                    generation = speciesData.generation,
                    killsSinceEvolution = speciesData.killsSinceEvolution,
                    averageFitness = speciesData.averageFitness,
                    population = new List<EnemyGenome>()
                };
                
                foreach (var genomeJson in speciesData.genomes)
                {
                    pop.population.Add(EnemyGenome.FromJson(genomeJson));
                }
                
                speciesPopulations[(EnemySpecies)speciesData.speciesId] = pop;
            }
            
            if (debugMode)
            {
                Debug.Log($"🧬 [GlobalGA] Loaded {speciesPopulations.Count} species from {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"🧬 [GlobalGA] Failed to load save: {e.Message}. Starting fresh.");
            speciesPopulations.Clear();
        }
    }
    
    private void DeleteSaveFile()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            if (debugMode)
            {
                Debug.Log($"🧬 [GlobalGA] Deleted save file.");
            }
        }
    }
    
    // ==================== DEBUG / METRICS ====================

#if UNITY_EDITOR
    /// <summary>
    /// Logs comprehensive metrics for debugging and tuning.
    /// </summary>
    [ContextMenu("Log All Metrics")]
    public void LogAllMetrics()
    {
        Debug.Log("═══════════════════════════════════════════════════════════");
        Debug.Log($"🧬 GLOBAL GENETIC ALGORITHM METRICS");
        Debug.Log("═══════════════════════════════════════════════════════════");
        Debug.Log($"Total Kills: {_totalKills}");
        Debug.Log($"Player Deaths: {_playerDeaths}");
        Debug.Log($"Current Difficulty Modifier: {_currentDifficultyModifier:F3}");
        Debug.Log($"Active Enemies: {activeEnemies.Count}");
        Debug.Log("───────────────────────────────────────────────────────────");
        
        foreach (var kvp in speciesPopulations)
        {
            var pop = kvp.Value;
            Debug.Log($"Species: {EnemySpeciesHelper.GetDisplayName(kvp.Key)}");
            Debug.Log($"  Generation: {pop.generation}");
            Debug.Log($"  Population Size: {pop.population.Count}");
            Debug.Log($"  Average Fitness: {pop.averageFitness:F2}");
            Debug.Log($"  Kills Since Evolution: {pop.killsSinceEvolution}/{evolveTriggerCount}");
            
            if (pop.population.Count > 0)
            {
                var best = pop.population.OrderByDescending(g => g.AverageFitness).First();
                Debug.Log($"  Best Genome: {best}");
            }
            Debug.Log("───────────────────────────────────────────────────────────");
        }
    }
    
    [ContextMenu("Force Evolution (All Species)")]
    public void ForceEvolveAll()
    {
        foreach (var pop in speciesPopulations.Values)
        {
            Evolve(pop);
        }
    }
    
    /// <summary>
    /// Force evolution for a specific species.
    /// </summary>
    public void ForceEvolution(EnemySpecies species)
    {
        if (speciesPopulations.TryGetValue(species, out var pop))
        {
            Evolve(pop);
            Debug.Log($"🧬 [GlobalGA] Forced evolution for {species}");
        }
    }
    
    [ContextMenu("Reset Difficulty Modifier")]
    public void ResetDifficultyModifier()
    {
        _currentDifficultyModifier = 1f;
        _playerDeaths = 0;
        _killsSinceLastDeath = 0;
        OnDifficultyChanged?.Invoke(_currentDifficultyModifier);
        Debug.Log("🧬 [GlobalGA] Difficulty modifier reset to 1.0");
    }
#endif
}
