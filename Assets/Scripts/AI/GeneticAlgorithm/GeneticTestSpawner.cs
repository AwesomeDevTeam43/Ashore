using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawner de teste para o Algoritmo Genético.
/// Spawna inimigos continuamente para testar a evolução.
/// 
/// SETUP:
/// 1. Cria um GameObject vazio chamado "GA_TestSpawner"
/// 2. Adiciona este script
/// 3. Arrasta o prefab do inimigo para "Enemy Prefab"
/// 4. Arrasta o Enemy_Stats para "Enemy Stats"
/// 5. Play e observa a evolução!
/// </summary>
public class GeneticTestSpawner : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("Prefab do inimigo a spawnar")]
    [SerializeField] private GameObject enemyPrefab;
    
    [Tooltip("Stats base do inimigo")]
    [SerializeField] private Enemy_Stats enemyStats;
    
    [Header("Spawn Settings")]
    [Tooltip("Número máximo de inimigos vivos ao mesmo tempo")]
    [SerializeField] private int maxAliveEnemies = 5;
    
    [Tooltip("Intervalo entre spawns (segundos)")]
    [SerializeField] private float spawnInterval = 2f;
    
    [Tooltip("Raio de spawn ao redor deste objeto")]
    [SerializeField] private float spawnRadius = 5f;
    
    [Tooltip("Começar a spawnar automaticamente")]
    [SerializeField] private bool autoStart = true;
    
    [Header("Test Options")]
    [Tooltip("Nível dos inimigos spawnados")]
    [SerializeField] private int enemyLevel = 1;
    
    [Tooltip("Mostrar área de spawn no editor")]
    [SerializeField] private bool showSpawnArea = true;
    
    [Header("Runtime Info (Read-Only)")]
    [SerializeField] private int _totalSpawned = 0;
    [SerializeField] private int _currentAlive = 0;
    [SerializeField] private bool _isSpawning = false;
    
    // Lista de inimigos ativos
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    private void Start()
    {
        // Verifica se o GlobalGeneticEvolver existe
        if (GlobalGeneticEvolver.Instance == null)
        {
            Debug.LogError("🧪 [TestSpawner] GlobalGeneticEvolver não encontrado! Adiciona-o à cena.");
            return;
        }
        
        if (enemyPrefab == null)
        {
            Debug.LogError("🧪 [TestSpawner] Enemy Prefab não atribuído!");
            return;
        }
        
        if (autoStart)
        {
            StartSpawning();
        }
    }
    
    private void Update()
    {
        // Limpa referências a inimigos destruídos
        activeEnemies.RemoveAll(e => e == null);
        _currentAlive = activeEnemies.Count;
        
        // Controlos de teste
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_isSpawning)
                StopSpawning();
            else
                StartSpawning();
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            KillAllEnemies();
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            SpawnSingleEnemy();
        }
    }
    
    /// <summary>
    /// Inicia o spawn contínuo de inimigos
    /// </summary>
    public void StartSpawning()
    {
        if (_isSpawning) return;
        
        _isSpawning = true;
        StartCoroutine(SpawnRoutine());
        Debug.Log("🧪 [TestSpawner] Spawn iniciado!");
    }
    
    /// <summary>
    /// Para o spawn de inimigos
    /// </summary>
    public void StopSpawning()
    {
        _isSpawning = false;
        StopAllCoroutines();
        Debug.Log("🧪 [TestSpawner] Spawn parado!");
    }
    
    /// <summary>
    /// Mata todos os inimigos ativos
    /// </summary>
    public void KillAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                var health = enemy.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(9999);
                }
                else
                {
                    Destroy(enemy);
                }
            }
        }
        activeEnemies.Clear();
        Debug.Log("🧪 [TestSpawner] Todos os inimigos mortos!");
    }
    
    /// <summary>
    /// Spawna um único inimigo
    /// </summary>
    public void SpawnSingleEnemy()
    {
        if (enemyPrefab == null) return;
        
        Vector2 spawnPos = GetRandomSpawnPosition();
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        
        SetupEnemy(enemy);
        
        activeEnemies.Add(enemy);
        _totalSpawned++;
        
        Debug.Log($"🧪 [TestSpawner] Inimigo spawnado! Total: {_totalSpawned}, Vivos: {_currentAlive + 1}");
    }
    
    private IEnumerator SpawnRoutine()
    {
        while (_isSpawning)
        {
            // Espera se já tem muitos inimigos
            while (activeEnemies.Count >= maxAliveEnemies)
            {
                yield return new WaitForSeconds(0.5f);
                activeEnemies.RemoveAll(e => e == null);
            }
            
            SpawnSingleEnemy();
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    private void SetupEnemy(GameObject enemy)
    {
        // Garante que tem o FitnessTracker
        var tracker = enemy.GetComponent<EnemyFitnessTracker>();
        if (tracker == null)
        {
            tracker = enemy.AddComponent<EnemyFitnessTracker>();
        }
        
        // Configura stats se disponível
        if (enemyStats != null)
        {
            var enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                var statsCopy = Instantiate(enemyStats);
                enemyBase.stats = statsCopy;
                enemyBase.SetStats(statsCopy);
                
                if (enemyLevel > 1)
                {
                    enemyBase.ApplyLevelMultipliers(
                        enemyLevel,
                        1.2f, // health mult
                        1.15f, // damage mult
                        1.25f, // xp mult
                        1.1f, // materials mult
                        false, // scale size
                        1.05f // size mult
                    );
                }
            }
        }
    }
    
    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 center = transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        return center + randomOffset;
    }
    
    private void OnDrawGizmos()
    {
        if (!showSpawnArea) return;
        
        // Área de spawn
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, spawnRadius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
    
    private void OnGUI()
    {
        // Instruções no ecrã
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleLeft;
        style.fontSize = 14;
        
        string status = _isSpawning ? "<color=green>SPAWNING</color>" : "<color=red>PAUSED</color>";
        string text = 
            $"🧪 TEST SPAWNER - {status}\n" +
            $"Vivos: {_currentAlive}/{maxAliveEnemies} | Total: {_totalSpawned}\n" +
            $"[SPACE] Toggle | [S] Spawn 1 | [X] Matar todos";
        
        GUI.Box(new Rect(Screen.width - 350, 10, 340, 70), text, style);
    }
}
