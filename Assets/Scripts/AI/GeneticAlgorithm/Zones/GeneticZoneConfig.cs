using UnityEngine;

/// <summary>
/// Define uma zona/área do jogo com os seus próprios parâmetros genéticos.
/// Cada zona pode ter dificuldade base diferente e evoluir independentemente.
/// </summary>
[CreateAssetMenu(fileName = "New Zone Config", menuName = "Genetic Algorithm/Zone Config")]
public class GeneticZoneConfig : ScriptableObject
{
    [Header("Zone Identity")]
    [Tooltip("Nome único da zona (ex: 'forest', 'caves', 'ruins')")]
    public string zoneId = "default";
    
    [Tooltip("Nome de display")]
    public string displayName = "Unknown Zone";
    
    [Header("Base Difficulty")]
    [Tooltip("Multiplicador base de vida dos inimigos nesta zona")]
    [Range(0.5f, 3f)] public float baseHealthMultiplier = 1f;
    
    [Tooltip("Multiplicador base de dano dos inimigos")]
    [Range(0.5f, 3f)] public float baseDamageMultiplier = 1f;
    
    [Tooltip("Multiplicador base de velocidade")]
    [Range(0.5f, 2f)] public float baseSpeedMultiplier = 1f;
    
    [Tooltip("Multiplicador base de agressividade (0 = passivo, 1 = muito agressivo)")]
    [Range(0f, 1f)] public float baseAggressiveness = 0.3f;
    
    [Header("Gene Ranges")]
    [Tooltip("Valor mínimo permitido para genes nesta zona")]
    [Range(0f, 1f)] public float minGeneValue = 0f;
    
    [Tooltip("Valor máximo permitido para genes nesta zona")]
    [Range(0f, 1f)] public float maxGeneValue = 1f;
    
    [Header("Evolution Settings")]
    [Tooltip("Taxa de evolução (1 = normal, 0.5 = lenta, 2 = rápida)")]
    [Range(0.1f, 3f)] public float evolutionRate = 1f;
    
    [Tooltip("Número de mortes para triggerar evolução nesta zona")]
    public int evolveTriggerCount = 10;
    
    [Tooltip("Taxa de mutação específica da zona")]
    [Range(0f, 0.5f)] public float mutationRate = 0.15f;
    
    [Header("Adaptive Difficulty")]
    [Tooltip("Se true, ajusta dificuldade baseado no desempenho do jogador")]
    public bool adaptiveDifficulty = true;
    
    [Tooltip("Força do ajuste adaptativo")]
    [Range(0f, 1f)] public float adaptiveStrength = 0.3f;
    
    /// <summary>
    /// Cria um genoma inicial apropriado para esta zona
    /// </summary>
    public EnemyGenome CreateZoneGenome()
    {
        EnemyGenome genome = new EnemyGenome();
        
        // Genes baseados na dificuldade da zona
        float difficultyFactor = (baseHealthMultiplier + baseDamageMultiplier) / 4f; // 0.25 a 1.5
        
        // Clamp genes dentro dos limites da zona
        genome.healthGene = ClampGene(Random.Range(difficultyFactor * 0.5f, difficultyFactor));
        genome.damageGene = ClampGene(Random.Range(difficultyFactor * 0.5f, difficultyFactor));
        genome.movementSpeedGene = ClampGene(Random.Range(0.3f, 0.7f) * baseSpeedMultiplier);
        genome.attackSpeedGene = ClampGene(Random.Range(0.3f, 0.7f));
        genome.aggressionRangeGene = ClampGene(Random.Range(0.3f, 0.7f));
        genome.aggressivenessGene = ClampGene(baseAggressiveness + Random.Range(-0.2f, 0.2f));
        genome.meleeResistanceGene = ClampGene(Random.Range(0f, difficultyFactor * 0.3f));
        genome.rangedResistanceGene = ClampGene(Random.Range(0f, difficultyFactor * 0.3f));
        
        return genome;
    }
    
    /// <summary>
    /// Aplica os modificadores de zona a um genoma existente
    /// </summary>
    public void ApplyZoneModifiers(EnemyGenome genome)
    {
        genome.healthGene = ClampGene(genome.healthGene * baseHealthMultiplier);
        genome.damageGene = ClampGene(genome.damageGene * baseDamageMultiplier);
        genome.movementSpeedGene = ClampGene(genome.movementSpeedGene * baseSpeedMultiplier);
        
        // Agressividade influenciada pela zona
        genome.aggressivenessGene = ClampGene(
            Mathf.Lerp(genome.aggressivenessGene, baseAggressiveness, 0.5f)
        );
    }
    
    private float ClampGene(float value)
    {
        return Mathf.Clamp(value, minGeneValue, maxGeneValue);
    }
}
