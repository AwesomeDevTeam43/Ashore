using System.Linq;
using UnityEngine;

public class Enemies_SpawnManager : MonoBehaviour
{
    [SerializeField] private bool spawnOnStart = true;
    
    [Header("Level System")]
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private float healthMultiplierPerLevel = 1.2f;
    [SerializeField] private float damageMultiplierPerLevel = 1.15f;
    [SerializeField] private float xpMultiplierPerLevel = 1.25f;
    [SerializeField] private float materialsMultiplierPerLevel = 1.1f;
    [SerializeField] private bool scaleVisualSize = false;
    [SerializeField] private float sizeMultiplierPerLevel = 1.05f;

    private EnemySpawnPoint[] spawnPoints;
    private HealthSystem entityHealth;

    void Awake()
    {
        spawnPoints = GetComponentsInChildren<EnemySpawnPoint>(includeInactive: true);
        Debug.Log($"[SpawnManager] Found {spawnPoints.Length} spawn points");
    }

    void Start()
    {
        if (spawnOnStart)
        {
            Debug.Log($"[SpawnManager] Spawning enemies with Level: {enemyLevel}");
            foreach (var sp in spawnPoints)
            {
                if (sp != null && sp.spawnOnStart && (!sp.singleSpawn || !sp.hasSpawned))
                {
                    SpawnAt(sp);
                }
            }
        }
    }

    public void SpawnAt(EnemySpawnPoint sp)
    {
        if (sp == null || sp.enemyPrefab == null)
        {
            Debug.LogWarning("[SpawnManager] Spawn point or prefab is null!");
            return;
        }
        if (sp.singleSpawn && sp.hasSpawned) return;

        GameObject go = Instantiate(sp.enemyPrefab, sp.transform.position, sp.transform.rotation);
        Debug.Log($"[SpawnManager] Enemy spawned: {go.name}");

        if (sp.enemyStatsOverride != null)
        {
            // Criar cópia dos stats
            var spawnStats = Instantiate(sp.enemyStatsOverride);
            Debug.Log($"[SpawnManager] Base Stats - Health: {spawnStats.maxHealth}, Damage: {spawnStats.damage}, XP: {spawnStats.xpOnDeath}");
            
            var enemyBase = go.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.stats = spawnStats;
                enemyBase.SetStats(spawnStats);
                
                Debug.Log($"[SpawnManager] Applying Level {enemyLevel} multipliers...");
                enemyBase.ApplyLevelMultipliers(
                    enemyLevel,
                    healthMultiplierPerLevel,
                    damageMultiplierPerLevel,
                    xpMultiplierPerLevel,
                    materialsMultiplierPerLevel,
                    scaleVisualSize,
                    sizeMultiplierPerLevel
                );
                
                Debug.Log($"[SpawnManager] After multipliers - Damage: {enemyBase.currentDamage}");
            }
            else
            {
                Debug.LogError($"[SpawnManager] EnemyBase component not found on {go.name}!");
            }
        }
        else
        {
            Debug.LogWarning("[SpawnManager] enemyStatsOverride is null!");
        }

        sp.hasSpawned = true;
    }

    public void SpawnAtIndex(int index)
    {
        if (spawnPoints == null) return;
        if (index < 0 || index >= spawnPoints.Length) return;
        SpawnAt(spawnPoints[index]);
    }
}