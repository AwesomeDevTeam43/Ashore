using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Logs boss gameplay data to CSV for training machine learning models.
/// Captures game state features and boss actions during gameplay.
/// Data is buffered and written to ml/boss_training_data.csv on application quit.
/// </summary>
public class BossDataLogger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player GameObject to track")]
    public GameObject player;
    
    [Tooltip("The boss GameObject to track")]
    public GameObject boss;

    [Header("Settings")]
    [Tooltip("Enable/disable data logging")]
    public bool enableLogging = true;

    [Tooltip("Path to save CSV file (relative to project root)")]
    public string csvFilePath = "ml/boss_training_data.csv";

    [Header("Auto-Save Settings")]
[Tooltip("Auto-save interval in seconds (0 to disable)")]
public float autoSaveInterval = 30f;

private float lastSaveTime = 0f;

    // Buffer for storing logged data entries
    private List<string> dataBuffer = new List<string>();
    
    // CSV header
    private const string CSV_HEADER = "distance_to_player,boss_health_pct,player_health_pct,is_phase2,can_use_laser,time_since_last_attack,action";

    // Component references
    private HealthSystem bossHealthSystem;
    private HealthSystem playerHealthSystem;
    private Boss bossScript;
    private Animator bossAnimator;

    // Track last known state for detecting state changes
    private string lastAnimatorState = "";
    
    // Constant for indicating boss has never attacked
    private const float NEVER_ATTACKED_TIME = 999f;

    private void Start()
    {
        if (!enableLogging)
        {
            Debug.Log("BossDataLogger: Logging is disabled");
            return;
        }

        // Get references
        if (boss != null)
        {
            bossHealthSystem = boss.GetComponent<HealthSystem>();
            bossScript = boss.GetComponent<Boss>();
            bossAnimator = boss.GetComponent<Animator>();
        }
        
        if (player != null)
        {
            playerHealthSystem = player.GetComponent<HealthSystem>();
        }

        // Validate references
        if (bossHealthSystem == null || playerHealthSystem == null || bossScript == null || bossAnimator == null)
        {
            Debug.LogWarning("BossDataLogger: Missing required components. Logging disabled.");
            enableLogging = false;
            return;
        }

        Debug.Log("BossDataLogger: Initialized and ready to log data");
    }

    private void Update()
    {
    if (! enableLogging) return;

    // Press F5 to manually save data at any time
    if (Input.GetKeyDown(KeyCode.F6))
    {
        Debug.Log("BossDataLogger: Manual save triggered!");
        WriteDataToFile();
    }

        if (autoSaveInterval > 0 && Time. time - lastSaveTime >= autoSaveInterval)
    {
        if (dataBuffer.Count > 0)
        {
            Debug.Log("BossDataLogger: Auto-saving.. .");
            WriteDataToFile();
            dataBuffer.Clear();  // Clear buffer after saving
            lastSaveTime = Time.time;
        }
    }

    // Detect animator state changes to log actions
    DetectAndLogAction();
    }

    /// <summary>
    /// Detects when the boss enters a new action state and logs the data
    /// </summary>
    private void DetectAndLogAction()
    {
        if (bossAnimator == null) return;

        // Get current state info
        AnimatorStateInfo stateInfo = bossAnimator.GetCurrentAnimatorStateInfo(0);
        string currentState = GetStateName(stateInfo);

        // Only log when we transition to a new action state
        if (currentState != lastAnimatorState && IsActionState(currentState))
        {
            LogGameState(currentState);
            lastAnimatorState = currentState;
        }

        
    }

    /// <summary>
    /// Gets a readable name from the animator state
    /// </summary>
private string GetStateName(AnimatorStateInfo stateInfo)
{
    // Updated to match actual animator state names
    if (stateInfo. IsName("BossAttack")) return "Attack";
    if (stateInfo.IsName("AttackCombo")) return "Combo";
    if (stateInfo. IsName("lasershoot")) return "Laser";
    if (stateInfo. IsName("BossIdle")) return "Idle";
    if (stateInfo. IsName("IntroTest")) return "Idle";  // Treat intro as idle
    return "Unknown";
}

    /// <summary>
    /// Checks if the state is an action we want to log
    /// </summary>
    private bool IsActionState(string stateName)
    {
        return stateName == "Attack" || stateName == "Combo" || stateName == "Laser" || 
               stateName == "Chase" || stateName == "Idle";
    }

    /// <summary>
    /// Logs the current game state with the given action
    /// </summary>
    /// <param name="action">The boss action being performed</param>
    public void LogGameState(string action)
    {
        if (!enableLogging) return;

        // Calculate features
        float distanceToPlayer = CalculateDistanceToPlayer();
        float bossHealthPct = CalculateBossHealthPct();
        float playerHealthPct = CalculatePlayerHealthPct();
        int isPhase2 = IsPhase2() ? 1 : 0;
        int canUseLaser = bossScript.canUseLaser ? 1 : 0;
        float timeSinceLastAttack = CalculateTimeSinceLastAttack();

        // Create CSV row
    string csvRow = string.Format(System.Globalization.CultureInfo.InvariantCulture,
    "{0:F2},{1:F3},{2:F3},{3},{4},{5:F2},{6}",
    distanceToPlayer,
    bossHealthPct,
    playerHealthPct,
    isPhase2,
    canUseLaser,
    timeSinceLastAttack,
    action);

        // Add to buffer
        dataBuffer.Add(csvRow);
        
        Debug.Log($"BossDataLogger: Logged {action} - Distance: {distanceToPlayer:F2}, BossHP: {bossHealthPct:F2}, PlayerHP: {playerHealthPct:F2}");
    }
    
    private float CalculateDistanceToPlayer()
    {
        if (boss == null || player == null) return 0f;
        return Vector3.Distance(boss.transform.position, player.transform.position);
    }

    private float CalculateBossHealthPct()
    {
        if (bossHealthSystem == null) return 1f;
        if (bossHealthSystem.MaxHealth <= 0) return 1f;
        return (float)bossHealthSystem.CurrentHealth / bossHealthSystem.MaxHealth;
    }

    private float CalculatePlayerHealthPct()
    {
        if (playerHealthSystem == null) return 1f;
        if (playerHealthSystem.MaxHealth <= 0) return 1f;
        return (float)playerHealthSystem.CurrentHealth / playerHealthSystem.MaxHealth;
    }

    private bool IsPhase2()
    {
        if (bossAnimator == null) return false;
        return bossAnimator.GetBool("Phase2");
    }

    private float CalculateTimeSinceLastAttack()
    {
        if (bossScript == null) return 0f;
        
        // If lastAttackTime is -Infinity, the boss hasn't attacked yet
        if (float.IsNegativeInfinity(bossScript.lastAttackTime))
        {
            return NEVER_ATTACKED_TIME;
        }
        
        return Time.time - bossScript.lastAttackTime;
    }

    /// <summary>
    /// Writes the buffered data to CSV file
    /// </summary>
    private void WriteDataToFile()
    {
        if (dataBuffer.Count == 0)
        {
            Debug.Log("BossDataLogger: No data to write");
            return;
        }

        try
        {
            // Get the full path (relative to project root)
            string fullPath = Path.Combine(Application.dataPath, "..", csvFilePath);
            
            // Ensure the directory exists
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if file exists to determine if we need to write header
            bool fileExists = File.Exists(fullPath);
            
            StringBuilder sb = new StringBuilder();
            
            // Add header if file doesn't exist
            if (!fileExists)
            {
                sb.AppendLine(CSV_HEADER);
            }
            
            // Add all buffered data
            foreach (string row in dataBuffer)
            {
                sb.AppendLine(row);
            }
            
            // Write to file (append mode)
            File.AppendAllText(fullPath, sb.ToString());
            
            Debug.Log($"BossDataLogger: Wrote {dataBuffer.Count} rows to {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BossDataLogger: Failed to write data to file: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        if (enableLogging && dataBuffer.Count > 0)
        {
            Debug.Log("BossDataLogger: Application quitting, writing buffered data...");
            WriteDataToFile();
        }
    }

    private void OnDestroy()
    {
        // Also write data when the logger is destroyed (e.g., scene change)
        if (enableLogging && dataBuffer.Count > 0)
        {
            Debug.Log("BossDataLogger: Logger destroyed, writing buffered data...");
            WriteDataToFile();
        }
    }
}
