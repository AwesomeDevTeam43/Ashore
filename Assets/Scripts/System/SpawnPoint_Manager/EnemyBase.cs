using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Assigned by SpawnManager (optional)")]
    public Enemy_Stats stats;

    [Header("Level Info")]
    [SerializeField] private int currentLevel = 1;
    public float currentDamage { get; private set; }
    
    private Enemy_Health enemyHealth;

    void Awake()
    {
        enemyHealth = GetComponent<Enemy_Health>();
    }

    public virtual void SetStats(Enemy_Stats s)
    {
        if (s == null) return;
        
        stats = s;
        currentDamage = s.damage;
        transform.localScale = s.baseScale;
        
        if (gameObject.name.Contains("Crab"))
        {
            Debug.Log($"[CRAB] SetStats called - Health: {s.maxHealth}, Damage: {currentDamage}");
        }
        
        // NÃO inicializar Enemy_Health aqui, será feito em ApplyLevelMultipliers
    }

    public void ApplyLevelMultipliers(
        int level,
        float healthMult,
        float damageMult,
        float xpMult,
        float materialsMult,
        bool applyScaling,
        float sizeMult)
    {
        if (stats == null)
        {
            if (gameObject.name.Contains("Crab"))
            {
                Debug.LogError("[CRAB] Stats is null in ApplyLevelMultipliers!");
            }
            return;
        }
        
        currentLevel = level;
        
        // Calcular multiplicador (level - 1 porque level 1 = sem multiplicador)
        float levelFactor = level - 1;
        
        if (gameObject.name.Contains("Crab"))
        {
            Debug.Log($"[CRAB] Applying Level {level} (factor: {levelFactor})");
        }
        
        // Calcular stats escalados
        int scaledHealth = Mathf.RoundToInt(stats.maxHealth * Mathf.Pow(healthMult, levelFactor));
        currentDamage = stats.damage * Mathf.Pow(damageMult, levelFactor);
        int scaledXP = Mathf.RoundToInt(stats.xpOnDeath * Mathf.Pow(xpMult, levelFactor));
        int scaledWood = Mathf.RoundToInt(stats.woodDrop * Mathf.Pow(materialsMult, levelFactor));
        int scaledStone = Mathf.RoundToInt(stats.stoneDrop * Mathf.Pow(materialsMult, levelFactor));
        int scaledRope = Mathf.RoundToInt(stats.ropeDrop * Mathf.Pow(materialsMult, levelFactor));
        
        if (gameObject.name.Contains("Crab"))
        {
            Debug.Log($"[CRAB] Scaled Stats - Health: {scaledHealth}, Damage: {currentDamage}, XP: {scaledXP}, Wood: {scaledWood}");
        }
        
        // Aplicar ao Enemy_Health APENAS AQUI
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<Enemy_Health>();
        }
        
        if (enemyHealth != null)
        {
            // Forçar reinicialização
            enemyHealth.Initialize(
                scaledHealth,
                scaledXP,
                scaledWood,
                scaledStone,
                scaledRope,
                stats.meleeResistance,
                stats.rangedResistance
            );
            
            if (gameObject.name.Contains("Crab"))
            {
                Debug.Log($"[CRAB] Enemy_Health.Initialize() called with Health={scaledHealth}");
                
                // Tentar acessar via reflection para debug
                var healthField = typeof(Enemy_Health).GetField("maxHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (healthField != null)
                {
                    var actualHealth = healthField.GetValue(enemyHealth);
                    Debug.Log($"[CRAB] Actual maxHealth after Initialize: {actualHealth}");
                }
            }
        }
        else
        {
            if (gameObject.name.Contains("Crab"))
            {
                Debug.LogError("[CRAB] Enemy_Health component not found!");
            }
        }
        
        // Aplicar escala visual se configurado
        if (applyScaling)
        {
            float scaleFactor = Mathf.Pow(sizeMult, levelFactor);
            transform.localScale = stats.baseScale * scaleFactor;
            
            if (gameObject.name.Contains("Crab"))
            {
                Debug.Log($"[CRAB] Visual scale applied: {transform.localScale}");
            }
        }
        else
        {
            transform.localScale = stats.baseScale;
        }
    }

    public int GetLevel() => currentLevel;
}