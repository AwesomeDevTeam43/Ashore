using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Tooltip("Prefab do inimigo a instanciar")]
    public GameObject enemyPrefab;

    [Tooltip("Opcional: override do ScriptableObject de stats para este spawnpoint")]
    public Enemy_Stats enemyStatsOverride;

    [Tooltip("Spawn automático no Start?")]
    public bool spawnOnStart = true;

    [Tooltip("Se true, spawna apenas uma vez")]
    public bool singleSpawn = true;


    [HideInInspector] public bool hasSpawned = false;
}