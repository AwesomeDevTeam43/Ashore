using UnityEngine;

/// <summary>
/// Extensão do EnemyBase que integra o sistema de Algoritmo Genético.
/// Herda desta classe em vez de EnemyBase para ter evolução automática.
/// 
/// EXEMPLO DE USO:
/// Em vez de: public class Enemy_Fly : EnemyBase
/// Use: public class Enemy_Fly : GeneticEnemyBase
/// </summary>
[RequireComponent(typeof(EnemyFitnessTracker))]
public abstract class GeneticEnemyBase : EnemyBase
{
    [Header("Genetic Algorithm")]
    [SerializeField] protected bool useGeneticEvolution = true;
    [SerializeField] protected bool debugGeneticStats = false;
    
    /// <summary>
    /// Referência ao tracker de fitness
    /// </summary>
    protected EnemyFitnessTracker fitnessTracker;
    
    /// <summary>
    /// O genoma deste inimigo (null se não usar GA)
    /// </summary>
    public EnemyGenome Genome => fitnessTracker?.Genome;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (useGeneticEvolution)
        {
            // Garante que tem o componente FitnessTracker
            fitnessTracker = GetComponent<EnemyFitnessTracker>();
            if (fitnessTracker == null)
            {
                fitnessTracker = gameObject.AddComponent<EnemyFitnessTracker>();
            }
        }
    }
    
    /// <summary>
    /// Obtém a velocidade de movimento escalada pelo genoma
    /// </summary>
    protected float GetGeneticMovementSpeed(float baseSpeed)
    {
        if (!useGeneticEvolution || fitnessTracker == null)
            return baseSpeed;
        
        return fitnessTracker.GetScaledMovementSpeed(baseSpeed);
    }
    
    /// <summary>
    /// Obtém o dano escalado pelo genoma
    /// </summary>
    protected new int GetScaledDamage()
    {
        float baseDamage = base.GetScaledDamage();
        
        if (!useGeneticEvolution || fitnessTracker == null)
            return Mathf.RoundToInt(baseDamage);
        
        return Mathf.RoundToInt(fitnessTracker.GetScaledDamage(baseDamage));
    }
    
    /// <summary>
    /// Obtém o intervalo de ataque escalado pelo genoma
    /// </summary>
    protected float GetGeneticAttackInterval(float baseInterval)
    {
        if (!useGeneticEvolution || fitnessTracker == null)
            return baseInterval;
        
        return fitnessTracker.GetScaledAttackInterval(baseInterval);
    }
    
    /// <summary>
    /// Obtém o alcance de detecção/agressão escalado pelo genoma
    /// </summary>
    protected float GetGeneticAggressionRange(float baseRange)
    {
        if (!useGeneticEvolution || fitnessTracker == null)
            return baseRange;
        
        return fitnessTracker.GetScaledAggressionRange(baseRange);
    }
    
    /// <summary>
    /// Registra dano causado ao jogador (chamar quando atacar)
    /// </summary>
    protected void RegisterDamageToPlayer(float damage)
    {
        fitnessTracker?.RegisterDamageDealt(damage);
    }
    
    /// <summary>
    /// Retorna se este inimigo é agressivo (gene > 0.5)
    /// </summary>
    protected bool IsAggressive()
    {
        if (Genome == null) return true;
        return Genome.aggressivenessGene > 0.5f;
    }
    
    /// <summary>
    /// Retorna nível de agressividade (0-1)
    /// </summary>
    protected float GetAggressiveness()
    {
        if (Genome == null) return 0.5f;
        return Genome.aggressivenessGene;
    }
    
#if UNITY_EDITOR
    protected virtual void OnGUI()
    {
        if (!debugGeneticStats || Genome == null) return;
        
        // Mostra stats do genoma acima do inimigo
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2);
        if (screenPos.z > 0)
        {
            GUI.Label(
                new Rect(screenPos.x - 75, Screen.height - screenPos.y, 150, 80),
                $"🧬 {name}\n" +
                $"HP: {Genome.healthGene:F2} DMG: {Genome.damageGene:F2}\n" +
                $"SPD: {Genome.movementSpeedGene:F2} AGR: {Genome.aggressivenessGene:F2}"
            );
        }
    }
#endif
}
