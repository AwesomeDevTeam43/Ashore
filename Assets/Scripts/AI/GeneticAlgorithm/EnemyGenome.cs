using UnityEngine;
using System;

/// <summary>
/// Representa o cromossomo de um inimigo com genes que definem seus atributos.
/// Cada gene é um valor normalizado entre 0 e 1 que será escalado para o atributo real.
/// </summary>
[Serializable]
public class EnemyGenome
{
    // ==================== GENES (valores normalizados 0-1) ====================
    
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
    [Range(0f, 1f)] public float aggressivenessGene = 0.5f; // Tendência a perseguir vs manter distância
    
    // ==================== FITNESS ====================
    
    /// <summary>
    /// Fitness acumulado deste genoma (quanto dano causou ao jogador, sobrevivência, etc.)
    /// </summary>
    public float Fitness { get; set; } = 0f;
    
    /// <summary>
    /// Número de vezes que este genoma foi usado (para calcular fitness médio)
    /// </summary>
    public int TimesUsed { get; set; } = 0;
    
    /// <summary>
    /// Fitness médio = Fitness / TimesUsed
    /// </summary>
    public float AverageFitness => TimesUsed > 0 ? Fitness / TimesUsed : 0f;
    
    // ==================== CONSTRUTORES ====================
    
    /// <summary>
    /// Cria um genoma com valores padrão (0.5 para todos os genes)
    /// </summary>
    public EnemyGenome() { }
    
    /// <summary>
    /// Cria um genoma com valores aleatórios
    /// </summary>
    public static EnemyGenome CreateRandom()
    {
        return new EnemyGenome
        {
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
    /// Cria uma cópia deste genoma
    /// </summary>
    public EnemyGenome Clone()
    {
        return new EnemyGenome
        {
            healthGene = this.healthGene,
            damageGene = this.damageGene,
            attackSpeedGene = this.attackSpeedGene,
            movementSpeedGene = this.movementSpeedGene,
            aggressionRangeGene = this.aggressionRangeGene,
            meleeResistanceGene = this.meleeResistanceGene,
            rangedResistanceGene = this.rangedResistanceGene,
            aggressivenessGene = this.aggressivenessGene,
            Fitness = 0f,
            TimesUsed = 0
        };
    }
    
    // ==================== OPERADORES GENÉTICOS ====================
    
    /// <summary>
    /// Crossover de dois genomas (combina genes de dois pais)
    /// </summary>
    public static EnemyGenome Crossover(EnemyGenome parent1, EnemyGenome parent2)
    {
        EnemyGenome child = new EnemyGenome();
        
        // Crossover uniforme: cada gene tem 50% de chance de vir de cada pai
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
    /// Aplica mutação aleatória aos genes
    /// </summary>
    /// <param name="mutationRate">Probabilidade de cada gene mutar (0-1)</param>
    /// <param name="mutationStrength">Intensidade da mutação (0-1)</param>
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
    
    // ==================== CONVERSÃO PARA VALORES REAIS ====================
    
    /// <summary>
    /// Converte o gene de vida para HP real
    /// </summary>
    public int GetScaledHealth(int baseHealth, float minMult = 0.5f, float maxMult = 2.0f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, healthGene);
        return Mathf.RoundToInt(baseHealth * multiplier);
    }
    
    /// <summary>
    /// Converte o gene de dano para dano real
    /// </summary>
    public float GetScaledDamage(float baseDamage, float minMult = 0.5f, float maxMult = 2.0f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, damageGene);
        return baseDamage * multiplier;
    }
    
    /// <summary>
    /// Converte o gene de velocidade de ataque para intervalo real
    /// </summary>
    public float GetScaledAttackInterval(float baseInterval, float minMult = 0.5f, float maxMult = 1.5f)
    {
        // Menor gene = ataque mais rápido
        float multiplier = Mathf.Lerp(maxMult, minMult, attackSpeedGene);
        return baseInterval * multiplier;
    }
    
    /// <summary>
    /// Converte o gene de velocidade de movimento para velocidade real
    /// </summary>
    public float GetScaledMovementSpeed(float baseSpeed, float minMult = 0.7f, float maxMult = 1.5f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, movementSpeedGene);
        return baseSpeed * multiplier;
    }
    
    /// <summary>
    /// Converte o gene de alcance de agressão para distância real
    /// </summary>
    public float GetScaledAggressionRange(float baseRange, float minMult = 0.5f, float maxMult = 2.0f)
    {
        float multiplier = Mathf.Lerp(minMult, maxMult, aggressionRangeGene);
        return baseRange * multiplier;
    }
    
    /// <summary>
    /// Retorna resistência melee escalada (0-0.5 para não ser imune)
    /// </summary>
    public float GetScaledMeleeResistance(float maxResistance = 0.5f)
    {
        return meleeResistanceGene * maxResistance;
    }
    
    /// <summary>
    /// Retorna resistência ranged escalada (0-0.5 para não ser imune)
    /// </summary>
    public float GetScaledRangedResistance(float maxResistance = 0.5f)
    {
        return rangedResistanceGene * maxResistance;
    }
    
    // ==================== SERIALIZAÇÃO ====================
    
    /// <summary>
    /// Converte o genoma para string (para salvar)
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    
    /// <summary>
    /// Cria genoma a partir de string JSON
    /// </summary>
    public static EnemyGenome FromJson(string json)
    {
        return JsonUtility.FromJson<EnemyGenome>(json);
    }
    
    public override string ToString()
    {
        return $"Genome[HP:{healthGene:F2} DMG:{damageGene:F2} SPD:{movementSpeedGene:F2} AGR:{aggressivenessGene:F2} FIT:{AverageFitness:F2}]";
    }
}
