using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Assigned by SpawnManager (optional)")]
    public Enemy_Stats stats;

    [Header("Level Info")]
    [SerializeField] private int currentLevel = 1;
    public float currentDamage { get; private set; }
    
    protected Enemy_Health enemyHealth; // Tornar protected para classes filhas usarem

    protected virtual void Awake() // Mudar para protected virtual
    {
        enemyHealth = GetComponent<Enemy_Health>();
    }

    public virtual void SetStats(Enemy_Stats s)
    {
        if (s == null) return;
        
        stats = s;
        currentDamage = s.damage;
        transform.localScale = s.baseScale;
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
            return;
        }
        
        currentLevel = level;
        
        float levelFactor = level - 1;

        
        int scaledHealth = Mathf.RoundToInt(stats.maxHealth * Mathf.Pow(healthMult, levelFactor));
        currentDamage = stats.damage * Mathf.Pow(damageMult, levelFactor);
        int scaledXP = Mathf.RoundToInt(stats.xpOnDeath * Mathf.Pow(xpMult, levelFactor));
        int scaledWood = Mathf.RoundToInt(stats.woodDrop * Mathf.Pow(materialsMult, levelFactor));
        int scaledStone = Mathf.RoundToInt(stats.stoneDrop * Mathf.Pow(materialsMult, levelFactor));
        int scaledRope = Mathf.RoundToInt(stats.ropeDrop * Mathf.Pow(materialsMult, levelFactor));
        
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<Enemy_Health>();
        }
        
        if (enemyHealth != null)
        {
            enemyHealth.Initialize(
                scaledHealth,
                scaledXP,
                scaledWood,
                scaledStone,
                scaledRope,
                stats.meleeResistance,
                stats.rangedResistance
            );
    
        }
        
        if (applyScaling)
        {
            float scaleFactor = Mathf.Pow(sizeMult, levelFactor);
            transform.localScale = stats.baseScale * scaleFactor;
            
        }
        else
        {
            transform.localScale = stats.baseScale;
        }
    }

    public int GetLevel() => currentLevel;
}