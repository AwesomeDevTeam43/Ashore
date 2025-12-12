using UnityEngine;
using System;

/// <summary>
/// Represents the chromosome of an enemy with genes that define its attributes.
/// Each gene is a normalized value between 0 and 1 that is scaled to real attributes.
/// 
/// GENES:
/// - healthGene: How much HP the enemy has (scales base health)
/// - damageGene: How much damage the enemy deals (scales base damage)
/// - attackSpeedGene: How fast the enemy attacks (reduces attack interval)
/// - movementSpeedGene: How fast the enemy moves
/// - aggressionRangeGene: Detection/aggression range
/// - aggressivenessGene: Tendency to pursue vs maintain distance
/// - meleeResistanceGene: Resistance to melee damage
/// - rangedResistanceGene: Resistance to ranged damage
/// 
/// FITNESS:
/// - Accumulated based on damage dealt to player and survival time
/// - Higher fitness = more likely to reproduce
/// </summary>
[Serializable]
public class EnemyGenome
{
    // ==================== SPECIES ====================
    
    /// <summary>
    /// The species this genome belongs to.
    /// Used for species-based evolution.
    /// </summary>
    public EnemySpecies species = EnemySpecies.Unknown;
    
    // ==================== GENES (normalized 0-1) ====================
    
    [Header("Combat Genes")]
    [Range(0f, 1f)] public float healthGene = 0.5f;
    [Range(0f, 1f)] public float damageGene = 0.5f;
    [Range(0f, 1f)] public float attackSpeedGene = 0.5f;
    
    [Header("Movement Genes")]
    [Range(0f, 1f)] public float movementSpeedGene = 0.5f;
    [Range(0f, 1f)] public float aggressionRangeGene = 0.5f;
    
    [Header("Defense Genes")]
    [Range(0f, 1f)] public float meleeResistanceGene = 0.5f;
    [Range(0f, 1f)] public float rangedResistanceGene = 0.5f;
    
    [Header("Behavior Genes")]
    [Range(0f, 1f)] public float aggressivenessGene = 0.5f; // Tendency to pursue vs maintain distance
    
    // ==================== FITNESS ====================
    
    /// <summary>
    /// Accumulated fitness of this genome (total performance across all uses).
    /// </summary>
    public float Fitness { get; set; } = 0f;
    
    /// <summary>
    /// Number of times this genome has been used.
    /// </summary>
    public int TimesUsed { get; set; } = 0;
    
    /// <summary>
    /// Average fitness = Fitness / TimesUsed.
    /// </summary>
    public float AverageFitness => TimesUsed > 0 ? Fitness / TimesUsed : 0f;
    
    // ==================== RUNTIME TRACKING ====================
    
    /// <summary>
    /// Damage dealt to player during current life (reset on clone/spawn).
    /// Used for accurate fitness calculation.
    /// </summary>
    [NonSerialized]
    public float damageDealtThisLife = 0f;
    
    // ==================== CONSTRUCTORS ====================
    
    /// <summary>
    /// Creates a genome with default values (0.5 for all genes).
    /// </summary>
    public EnemyGenome() { }
    
    /// <summary>
    /// Creates a genome with random values.
    /// </summary>
    public static EnemyGenome CreateRandom(EnemySpecies species = EnemySpecies.Unknown)
    {
        return new EnemyGenome
        {
            species = species,
            healthGene = UnityEngine.Random.value,
            damageGene = UnityEngine.Random.value,
            attackSpeedGene = UnityEngine.Random.value,
            movementSpeedGene = UnityEngine.Random.value,
            aggressionRangeGene = UnityEngine.Random.value,
            meleeResistanceGene = UnityEngine.Random.value,
            rangedResistanceGene = UnityEngine.Random.value,
            aggressivenessGene = UnityEngine.Random.value
        };
    }
    
    /// <summary>
    /// Creates a copy of this genome.
    /// Resets fitness tracking for the new instance.
    /// </summary>
    public EnemyGenome Clone()
    {
        return new EnemyGenome
        {
            species = this.species,
            healthGene = this.healthGene,
            damageGene = this.damageGene,
            attackSpeedGene = this.attackSpeedGene,
            movementSpeedGene = this.movementSpeedGene,
            aggressionRangeGene = this.aggressionRangeGene,
            meleeResistanceGene = this.meleeResistanceGene,
            rangedResistanceGene = this.rangedResistanceGene,
            aggressivenessGene = this.aggressivenessGene,
            Fitness = 0f,
            TimesUsed = 0,
            damageDealtThisLife = 0f
        };
    }
    
    // ==================== GENETIC OPERATORS ====================
    
    /// <summary>
    /// Crossover of two genomes (combines genes from two parents).
    /// Uses uniform crossover: each gene has 50% chance from each parent.
    /// </summary>
    public static EnemyGenome Crossover(EnemyGenome parent1, EnemyGenome parent2)
    {
        if (parent1 == null || parent2 == null)
        {
            Debug.LogWarning("Crossover called with null parent(s)");
            return parent1?.Clone() ?? parent2?.Clone() ?? new EnemyGenome();
        }
        
        // Ensure different parents (reference check)
        if (ReferenceEquals(parent1, parent2))
        {
            Debug.LogWarning("Crossover called with same parent twice. Using mutation instead.");
            var clone = parent1.Clone();
            clone.Mutate(0.3f, 0.2f);
            return clone;
        }
        
        EnemyGenome child = new EnemyGenome
        {
            // Inherit species from parent1 (they should be same species anyway)
            species = parent1.species
        };
        
        // Uniform crossover: each gene has 50% chance from each parent
        child.healthGene = UnityEngine.Random.value > 0.5f ? parent1.healthGene : parent2.healthGene;
        child.damageGene = UnityEngine.Random.value > 0.5f ? parent1.damageGene : parent2.damageGene;
        child.attackSpeedGene = UnityEngine.Random.value > 0.5f ? parent1.attackSpeedGene : parent2.attackSpeedGene;
        child.movementSpeedGene = UnityEngine.Random.value > 0.5f ? parent1.movementSpeedGene : parent2.movementSpeedGene;
        child.aggressionRangeGene = UnityEngine.Random.value > 0.5f ? parent1.aggressionRangeGene : parent2.aggressionRangeGene;
        child.meleeResistanceGene = UnityEngine.Random.value > 0.5f ? parent1.meleeResistanceGene : parent2.meleeResistanceGene;
        child.rangedResistanceGene = UnityEngine.Random.value > 0.5f ? parent1.rangedResistanceGene : parent2.rangedResistanceGene;
        child.aggressivenessGene = UnityEngine.Random.value > 0.5f ? parent1.aggressivenessGene : parent2.aggressivenessGene;
        
        return child;
    }
    
    /// <summary>
    /// Applies random mutation to genes.
    /// </summary>
    /// <param name="mutationRate">Probability of each gene mutating (0-1)</param>
    /// <param name="mutationStrength">Maximum change per mutation (0-1)</param>
    public void Mutate(float mutationRate = 0.1f, float mutationStrength = 0.2f)
    {
        if (UnityEngine.Random.value < mutationRate)
            healthGene = MutateGene(healthGene, mutationStrength);
        
        if (UnityEngine.Random.value < mutationRate)
            damageGene = MutateGene(damageGene, mutationStrength);
        
        if (UnityEngine.Random.value < mutationRate)
            attackSpeedGene = MutateGene(attackSpeedGene, mutationStrength);
        
        if (UnityEngine.Random.value < mutationRate)
            movementSpeedGene = MutateGene(movementSpeedGene, mutationStrength);
        
        if (UnityEngine.Random.value < mutationRate)
            aggressionRangeGene = MutateGene(aggressionRangeGene, mutationStrength);
        
        if (UnityEngine.Random.value < mutationRate)
            meleeResistanceGene = MutateGene(meleeResistanceGene, mutationStrength);
        
        if (UnityEngine.Random.value < mutationRate)
            rangedResistanceGene = MutateGene(rangedResistanceGene, mutationStrength);
        
        if (UnityEngine.Random.value < mutationRate)
            aggressivenessGene = MutateGene(aggressivenessGene, mutationStrength);
    }
    
    private float MutateGene(float gene, float strength)
    {
        float mutation = UnityEngine.Random.Range(-strength, strength);
        return Mathf.Clamp01(gene + mutation);
    }
    
    /// <summary>
    /// Clamps all genes to a maximum value.
    /// Used to enforce difficulty ceiling.
    /// </summary>
    /// <param name="maxValue">Maximum allowed gene value (typically 0.85-0.95)</param>
    public void ClampGenes(float maxValue)
    {
        healthGene = Mathf.Min(healthGene, maxValue);
        damageGene = Mathf.Min(damageGene, maxValue);
        attackSpeedGene = Mathf.Min(attackSpeedGene, maxValue);
        movementSpeedGene = Mathf.Min(movementSpeedGene, maxValue);
        aggressionRangeGene = Mathf.Min(aggressionRangeGene, maxValue);
        meleeResistanceGene = Mathf.Min(meleeResistanceGene, maxValue);
        rangedResistanceGene = Mathf.Min(rangedResistanceGene, maxValue);
        aggressivenessGene = Mathf.Min(aggressivenessGene, maxValue);
    }
    
    // ==================== CONVERSION TO REAL VALUES ====================
    
    /// <summary>
    /// Converts the health gene to actual HP.
    /// </summary>
    /// <param name="baseHealth">Base health from Enemy_Stats</param>
    /// <param name="minMult">Minimum multiplier (default 0.5x)</param>
    /// <param name="maxMult">Maximum multiplier (default 2.0x)</param>
    public int GetScaledHealth(int baseHealth, float minMult = 0.5f, float maxMult = 2.0f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, healthGene);
        return Mathf.RoundToInt(baseHealth * multiplier);
    }
    
    /// <summary>
    /// Converts the damage gene to actual damage.
    /// </summary>
    public float GetScaledDamage(float baseDamage, float minMult = 0.5f, float maxMult = 2.0f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, damageGene);
        return baseDamage * multiplier;
    }
    
    /// <summary>
    /// Converts the attack speed gene to actual attack interval.
    /// Note: Higher gene = faster attacks = lower interval.
    /// </summary>
    public float GetScaledAttackInterval(float baseInterval, float minMult = 0.5f, float maxMult = 1.5f)
    {
        // Invert: higher gene = faster = lower interval
        float multiplier = Mathf.Lerp(maxMult, minMult, attackSpeedGene);
        return baseInterval * multiplier;
    }
    
    /// <summary>
    /// Converts the movement speed gene to actual speed.
    /// </summary>
    public float GetScaledMovementSpeed(float baseSpeed, float minMult = 0.7f, float maxMult = 1.5f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, movementSpeedGene);
        return baseSpeed * multiplier;
    }
    
    /// <summary>
    /// Converts the aggression range gene to actual detection distance.
    /// </summary>
    public float GetScaledAggressionRange(float baseRange, float minMult = 0.5f, float maxMult = 2.0f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, aggressionRangeGene);
        return baseRange * multiplier;
    }
    
    /// <summary>
    /// Returns scaled melee resistance (capped to prevent immunity).
    /// </summary>
    /// <param name="maxResistance">Maximum resistance allowed (default 0.5 = 50%)</param>
    public float GetScaledMeleeResistance(float maxResistance = 0.5f)
    {
        return meleeResistanceGene * maxResistance;
    }
    
    /// <summary>
    /// Returns scaled ranged resistance (capped to prevent immunity).
    /// </summary>
    public float GetScaledRangedResistance(float maxResistance = 0.5f)
    {
        return rangedResistanceGene * maxResistance;
    }
    
    // ==================== SIMILARITY CHECK ====================
    
    /// <summary>
    /// Calculates how similar this genome is to another.
    /// Returns a value from 0 (identical) to ~8 (completely different).
    /// </summary>
    public float SimilarityTo(EnemyGenome other)
    {
        if (other == null) return float.MaxValue;
        
        return Mathf.Abs(healthGene - other.healthGene) +
               Mathf.Abs(damageGene - other.damageGene) +
               Mathf.Abs(attackSpeedGene - other.attackSpeedGene) +
               Mathf.Abs(movementSpeedGene - other.movementSpeedGene) +
               Mathf.Abs(aggressionRangeGene - other.aggressionRangeGene) +
               Mathf.Abs(meleeResistanceGene - other.meleeResistanceGene) +
               Mathf.Abs(rangedResistanceGene - other.rangedResistanceGene) +
               Mathf.Abs(aggressivenessGene - other.aggressivenessGene);
    }
    
    // ==================== SERIALIZATION ====================
    
    /// <summary>
    /// Converts the genome to JSON string for saving.
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    
    /// <summary>
    /// Creates a genome from JSON string.
    /// </summary>
    public static EnemyGenome FromJson(string json)
    {
        try
        {
            return JsonUtility.FromJson<EnemyGenome>(json);
        }
        catch
        {
            Debug.LogWarning("Failed to parse genome JSON, returning default");
            return new EnemyGenome();
        }
    }
    
    public override string ToString()
    {
        string speciesName = EnemySpeciesHelper.GetDisplayName(species);
        return $"Genome[{speciesName}|HP:{healthGene:F2} DMG:{damageGene:F2} SPD:{movementSpeedGene:F2} AGR:{aggressivenessGene:F2} FIT:{AverageFitness:F2}]";
    }
    
    // ==================== DIFFICULTY METRICS ====================
    
    /// <summary>
    /// Returns a rough "power level" for this genome.
    /// Useful for debugging and UI display.
    /// </summary>
    public float GetPowerLevel()
    {
        // Weight combat stats more heavily
        return (healthGene * 1.5f) + 
               (damageGene * 2.0f) + 
               (attackSpeedGene * 1.0f) +
               (movementSpeedGene * 0.5f) +
               (aggressivenessGene * 1.0f) +
               (meleeResistanceGene * 0.5f) +
               (rangedResistanceGene * 0.5f);
    }
}
