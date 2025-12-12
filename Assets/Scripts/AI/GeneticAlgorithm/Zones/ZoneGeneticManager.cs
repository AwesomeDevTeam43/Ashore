using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gerencia populações genéticas separadas por zona.
/// Cada zona do jogo metroidvania tem a sua própria evolução.
/// 
/// SETUP:
/// 1. Adiciona este componente ao mesmo GameObject do GeneticEnemyEvolver
/// 2. Cria ZoneConfigs para cada área do jogo (Assets > Create > Genetic Algorithm > Zone Config)
/// 3. Adiciona ZoneIdentifier aos GameObjects das zonas ou usa o SetCurrentZone()
/// </summary>
public class ZoneGeneticManager : MonoBehaviour
{
    public static ZoneGeneticManager Instance { get; private set; }
    
    [Header("Zone Configurations")]
    [Tooltip("Lista de todas as zonas do jogo")]
    [SerializeField] private List<GeneticZoneConfig> zoneConfigs = new List<GeneticZoneConfig>();
    
    [Header("Default Zone")]
    [Tooltip("Zona padrão se nenhuma for especificada")]
    [SerializeField] private GeneticZoneConfig defaultZone;
    
    [Header("Population Settings")]
    [Tooltip("Tamanho da população por zona")]
    [SerializeField] private int populationPerZone = 15;
    
    [Tooltip("Número de elites a manter por zona")]
    [SerializeField] private int elitesPerZone = 3;
    
    [Header("Cross-Zone Learning")]
    [Tooltip("Permite que zonas avançadas herdem genes de zonas anteriores")]
    [SerializeField] private bool enableCrossZoneLearning = true;
    
    [Tooltip("% de genes que podem migrar entre zonas adjacentes")]
    [Range(0f, 0.3f)]
    [SerializeField] private float crossZoneMigrationRate = 0.1f;
    
    [Header("Persistence")]
    [SerializeField] private bool saveZoneProgress = true;
    private const string SAVE_PREFIX = "GA_Zone_";
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private string _currentZoneId = "none";
    
    // Populações por zona
    private Dictionary<string, List<EnemyGenome>> zonePopulations = new Dictionary<string, List<EnemyGenome>>();
    
    // Contadores de mortes por zona
    private Dictionary<string, int> zoneDeathCounters = new Dictionary<string, int>();
    
    // Geração por zona
    private Dictionary<string, int> zoneGenerations = new Dictionary<string, int>();
    
    // Zona atual do jogador
    private GeneticZoneConfig currentZone;
    
    // ==================== UNITY LIFECYCLE ====================
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Inicializa todas as zonas
        foreach (var zone in zoneConfigs)
        {
            if (zone != null)
            {
                InitializeZone(zone);
            }
        }
        
        // Define zona padrão
        if (defaultZone != null)
        {
            currentZone = defaultZone;
            _currentZoneId = defaultZone.zoneId;
        }
        else if (zoneConfigs.Count > 0)
        {
            currentZone = zoneConfigs[0];
            _currentZoneId = currentZone.zoneId;
        }
    }
    
    private void Start()
    {
        if (saveZoneProgress)
        {
            LoadAllZones();
        }
        
        if (debugMode)
        {
            Debug.Log($"🗺️ [ZoneGA] Initialized {zonePopulations.Count} zones");
        }
    }
    
    private void OnApplicationQuit()
    {
        if (saveZoneProgress)
        {
            SaveAllZones();
        }
    }
    
    // ==================== API PÚBLICA ====================
    
    /// <summary>
    /// Define a zona atual do jogador
    /// </summary>
    public void SetCurrentZone(string zoneId)
    {
        var zone = zoneConfigs.Find(z => z.zoneId == zoneId);
        if (zone != null)
        {
            currentZone = zone;
            _currentZoneId = zoneId;
            
            if (debugMode)
            {
                Debug.Log($"🗺️ [ZoneGA] Entered zone: {zone.displayName}");
            }
        }
        else
        {
            Debug.LogWarning($"🗺️ [ZoneGA] Zone '{zoneId}' not found!");
        }
    }
    
    /// <summary>
    /// Define a zona atual pelo config
    /// </summary>
    public void SetCurrentZone(GeneticZoneConfig zone)
    {
        if (zone != null)
        {
            currentZone = zone;
            _currentZoneId = zone.zoneId;
            
            // Garante que a zona está inicializada
            if (!zonePopulations.ContainsKey(zone.zoneId))
            {
                InitializeZone(zone);
            }
        }
    }
    
    /// <summary>
    /// Obtém um genoma para um inimigo na zona atual
    /// </summary>
    public EnemyGenome GetGenomeForZone(string zoneId = null)
    {
        string targetZone = zoneId ?? currentZone?.zoneId ?? "default";
        var zone = GetZoneConfig(targetZone);
        
        if (!zonePopulations.ContainsKey(targetZone))
        {
            InitializeZone(zone);
        }
        
        var population = zonePopulations[targetZone];
        
        // Seleção por torneio
        EnemyGenome selected = TournamentSelect(population, 3);
        EnemyGenome genome = selected.Clone();
        
        // Aplica modificadores da zona
        zone.ApplyZoneModifiers(genome);
        
        // Pequena mutação
        genome.Mutate(zone.mutationRate * 0.5f, 0.1f);
        
        if (debugMode)
        {
            Debug.Log($"🗺️ [ZoneGA] Assigned genome from zone '{zone.displayName}': {genome}");
        }
        
        return genome;
    }
    
    /// <summary>
    /// Registra morte de inimigo na zona específica
    /// </summary>
    public void RegisterDeathInZone(string zoneId, EnemyGenome genome, float fitness)
    {
        if (!zonePopulations.ContainsKey(zoneId)) return;
        
        var zone = GetZoneConfig(zoneId);
        
        // Atualiza fitness na população
        var population = zonePopulations[zoneId];
        var similar = FindMostSimilar(genome, population);
        if (similar != null)
        {
            similar.Fitness += fitness * zone.evolutionRate;
            similar.TimesUsed++;
        }
        
        // Incrementa contador
        if (!zoneDeathCounters.ContainsKey(zoneId))
            zoneDeathCounters[zoneId] = 0;
        
        zoneDeathCounters[zoneId]++;
        
        // Verifica se deve evoluir
        if (zoneDeathCounters[zoneId] >= zone.evolveTriggerCount)
        {
            EvolveZone(zoneId);
        }
    }
    
    /// <summary>
    /// Força evolução de uma zona específica
    /// </summary>
    public void EvolveZone(string zoneId)
    {
        if (!zonePopulations.ContainsKey(zoneId)) return;
        
        var zone = GetZoneConfig(zoneId);
        var population = zonePopulations[zoneId];
        
        // Incrementa geração
        if (!zoneGenerations.ContainsKey(zoneId))
            zoneGenerations[zoneId] = 0;
        zoneGenerations[zoneId]++;
        
        zoneDeathCounters[zoneId] = 0;
        
        if (debugMode)
        {
            float avgFitness = population.Average(g => g.AverageFitness);
            Debug.Log($"🗺️ [ZoneGA] ═══ EVOLVING ZONE '{zone.displayName}' TO GEN {zoneGenerations[zoneId]} ═══");
            Debug.Log($"🗺️ [ZoneGA] Avg Fitness: {avgFitness:F2}");
        }
        
        List<EnemyGenome> newPopulation = new List<EnemyGenome>();
        
        // Elitismo
        var elites = population
            .OrderByDescending(g => g.AverageFitness)
            .Take(elitesPerZone)
            .ToList();
        
        foreach (var elite in elites)
        {
            var eliteClone = elite.Clone();
            eliteClone.Fitness = elite.AverageFitness * 0.5f;
            eliteClone.TimesUsed = 1;
            newPopulation.Add(eliteClone);
        }
        
        // Cross-zone learning: importa genes de zonas anteriores
        if (enableCrossZoneLearning)
        {
            ImportFromPreviousZones(zoneId, newPopulation);
        }
        
        // Preenche resto com crossover/mutação
        while (newPopulation.Count < populationPerZone)
        {
            EnemyGenome child;
            
            if (Random.value < 0.7f && population.Count >= 2)
            {
                var p1 = TournamentSelect(population, 3);
                var p2 = TournamentSelect(population, 3);
                child = EnemyGenome.Crossover(p1, p2);
            }
            else
            {
                child = zone.CreateZoneGenome();
            }
            
            child.Mutate(zone.mutationRate, 0.2f);
            zone.ApplyZoneModifiers(child);
            
            newPopulation.Add(child);
        }
        
        zonePopulations[zoneId] = newPopulation;
    }
    
    /// <summary>
    /// Obtém estatísticas de uma zona
    /// </summary>
    public ZoneStats GetZoneStats(string zoneId)
    {
        if (!zonePopulations.ContainsKey(zoneId))
            return new ZoneStats();
        
        var population = zonePopulations[zoneId];
        var zone = GetZoneConfig(zoneId);
        
        return new ZoneStats
        {
            zoneId = zoneId,
            displayName = zone?.displayName ?? zoneId,
            generation = zoneGenerations.ContainsKey(zoneId) ? zoneGenerations[zoneId] : 0,
            populationSize = population.Count,
            averageFitness = population.Average(g => g.AverageFitness),
            bestFitness = population.Max(g => g.AverageFitness),
            deathsSinceEvolution = zoneDeathCounters.ContainsKey(zoneId) ? zoneDeathCounters[zoneId] : 0
        };
    }
    
    /// <summary>
    /// Obtém a zona atual
    /// </summary>
    public GeneticZoneConfig GetCurrentZone() => currentZone;
    
    /// <summary>
    /// Obtém todas as zonas configuradas
    /// </summary>
    public List<GeneticZoneConfig> GetAllZones() => zoneConfigs;
    
    // ==================== MÉTODOS INTERNOS ====================
    
    private void InitializeZone(GeneticZoneConfig zone)
    {
        if (zone == null || zonePopulations.ContainsKey(zone.zoneId)) return;
        
        List<EnemyGenome> population = new List<EnemyGenome>();
        
        for (int i = 0; i < populationPerZone; i++)
        {
            population.Add(zone.CreateZoneGenome());
        }
        
        zonePopulations[zone.zoneId] = population;
        zoneDeathCounters[zone.zoneId] = 0;
        zoneGenerations[zone.zoneId] = 0;
        
        if (debugMode)
        {
            Debug.Log($"🗺️ [ZoneGA] Initialized zone '{zone.displayName}' with {populationPerZone} genomes");
        }
    }
    
    private GeneticZoneConfig GetZoneConfig(string zoneId)
    {
        var zone = zoneConfigs.Find(z => z.zoneId == zoneId);
        return zone ?? defaultZone ?? zoneConfigs.FirstOrDefault();
    }
    
    private EnemyGenome TournamentSelect(List<EnemyGenome> population, int tournamentSize)
    {
        EnemyGenome best = null;
        float bestFitness = float.MinValue;
        
        for (int i = 0; i < tournamentSize; i++)
        {
            var candidate = population[Random.Range(0, population.Count)];
            float fitness = candidate.TimesUsed > 0 ? candidate.AverageFitness : 1f;
            
            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                best = candidate;
            }
        }
        
        return best ?? population[0];
    }
    
    private EnemyGenome FindMostSimilar(EnemyGenome target, List<EnemyGenome> population)
    {
        EnemyGenome best = null;
        float bestDiff = float.MaxValue;
        
        foreach (var genome in population)
        {
            float diff = Mathf.Abs(target.healthGene - genome.healthGene) +
                        Mathf.Abs(target.damageGene - genome.damageGene) +
                        Mathf.Abs(target.movementSpeedGene - genome.movementSpeedGene);
            
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = genome;
            }
        }
        
        return best;
    }
    
    private void ImportFromPreviousZones(string currentZoneId, List<EnemyGenome> newPopulation)
    {
        int currentIndex = zoneConfigs.FindIndex(z => z.zoneId == currentZoneId);
        if (currentIndex <= 0) return;
        
        // Busca genomas da zona anterior
        var previousZone = zoneConfigs[currentIndex - 1];
        if (!zonePopulations.ContainsKey(previousZone.zoneId)) return;
        
        var previousPopulation = zonePopulations[previousZone.zoneId];
        int toImport = Mathf.FloorToInt(populationPerZone * crossZoneMigrationRate);
        
        var bestFromPrevious = previousPopulation
            .OrderByDescending(g => g.AverageFitness)
            .Take(toImport);
        
        foreach (var genome in bestFromPrevious)
        {
            var migrant = genome.Clone();
            migrant.Mutate(0.2f, 0.15f); // Mutação ao migrar
            
            // Aplica modificadores da nova zona
            var currentZoneConfig = GetZoneConfig(currentZoneId);
            currentZoneConfig.ApplyZoneModifiers(migrant);
            
            newPopulation.Add(migrant);
        }
        
        if (debugMode && toImport > 0)
        {
            Debug.Log($"🗺️ [ZoneGA] Imported {toImport} genomes from '{previousZone.displayName}'");
        }
    }
    
    // ==================== PERSISTÊNCIA ====================
    
    private void SaveAllZones()
    {
        foreach (var kvp in zonePopulations)
        {
            SaveZone(kvp.Key);
        }
    }
    
    private void LoadAllZones()
    {
        foreach (var zone in zoneConfigs)
        {
            LoadZone(zone.zoneId);
        }
    }
    
    private void SaveZone(string zoneId)
    {
        if (!zonePopulations.ContainsKey(zoneId)) return;
        
        var data = new ZoneSaveData
        {
            generation = zoneGenerations.ContainsKey(zoneId) ? zoneGenerations[zoneId] : 0,
            genomes = zonePopulations[zoneId].Select(g => g.ToJson()).ToArray()
        };
        
        PlayerPrefs.SetString(SAVE_PREFIX + zoneId, JsonUtility.ToJson(data));
    }
    
    private void LoadZone(string zoneId)
    {
        string key = SAVE_PREFIX + zoneId;
        if (!PlayerPrefs.HasKey(key)) return;
        
        try
        {
            var data = JsonUtility.FromJson<ZoneSaveData>(PlayerPrefs.GetString(key));
            
            zoneGenerations[zoneId] = data.generation;
            zonePopulations[zoneId] = data.genomes.Select(EnemyGenome.FromJson).ToList();
            
            if (debugMode)
            {
                Debug.Log($"🗺️ [ZoneGA] Loaded zone '{zoneId}' (Gen {data.generation})");
            }
        }
        catch
        {
            Debug.LogWarning($"🗺️ [ZoneGA] Failed to load zone '{zoneId}'");
        }
    }
    
    [System.Serializable]
    private class ZoneSaveData
    {
        public int generation;
        public string[] genomes;
    }
    
    public struct ZoneStats
    {
        public string zoneId;
        public string displayName;
        public int generation;
        public int populationSize;
        public float averageFitness;
        public float bestFitness;
        public int deathsSinceEvolution;
    }
}
