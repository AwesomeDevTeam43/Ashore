using System.Linq;
using UnityEngine;

public class Enemies_SpawnManager : MonoBehaviour
{
    [SerializeField] private bool spawnOnStart = true;

    private EnemySpawnPoint[] spawnPoints;

    void Awake()
    {
        spawnPoints = GetComponentsInChildren<EnemySpawnPoint>(includeInactive: true);
    }

    void Start()
    {
        if (spawnOnStart)
        {
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
        if (sp == null || sp.enemyPrefab == null) return;
        if (sp.singleSpawn && sp.hasSpawned) return;

        GameObject go = Instantiate(sp.enemyPrefab, sp.transform.position, sp.transform.rotation);

        if (sp.enemyStatsOverride != null)
        {
            var spawnStats = sp.enemyStatsOverride;
            var enemyBase = go.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.stats = spawnStats;
            }
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