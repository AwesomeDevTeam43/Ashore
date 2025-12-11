using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Automated battle simulator for collecting boss AI training data.
/// Attach to an empty GameObject in the boss scene.
/// Runs multiple simulated battles and saves training data to CSV.
/// </summary>
public class BossBattleSimulator : MonoBehaviour
{
    [Header("Simulation Settings")]
    [Tooltip("Enable to run simulation on play")]
    public bool runSimulation = false;

    [Tooltip("Number of battles to simulate")]
    public int battlesToSimulate = 10;

    [Tooltip("Simulation speed multiplier (1-10x)")]
    [Range(1, 10)]
    public int simulationSpeed = 1;

    [Tooltip("Maximum duration for a single battle (seconds)")]
    public float maxBattleDuration = 120f;

    [Tooltip("Use smart AI logic (rule-based) instead of random")]
    public bool useSmartAI = true;

    [Header("References")]
    [Tooltip("Boss GameObject")]
    public GameObject boss;

    [Tooltip("Player GameObject")]
    public GameObject player;

    [Header("Output Settings")]
    [Tooltip("Path to save CSV file (relative to project root)")]
    public string csvFilePath = "ml/boss_training_data.csv";

    // Component references
    private HealthSystem bossHealthSystem;
    private HealthSystem playerHealthSystem;
    private Boss bossScript;
    private Animator bossAnimator;

    // Simulation state
    private bool isSimulating = false;
    private int currentBattle = 0;
    private float battleStartTime = 0f;
    private List<string> collectedData = new List<string>();
    private string statusMessage = "";
    
    // CSV header
    private const string CSV_HEADER = "distance_to_player,boss_health_pct,player_health_pct,is_phase2,can_use_laser,time_since_last_attack,action";

    // Initial positions and states
    private Vector3 initialBossPosition;
    private Vector3 initialPlayerPosition;
    private int initialBossHealth;
    private int initialPlayerHealth;

    // Action cooldown
    private float lastActionTime = 0f;
    private float actionInterval = 2f;  // Time between actions

    void Start()
    {
        if (!runSimulation)
        {
            Debug.Log("BossBattleSimulator: Simulation is disabled");
            return;
        }

        // Validate references
        if (boss == null || player == null)
        {
            Debug.LogError("BossBattleSimulator: Missing boss or player reference!");
            statusMessage = "ERROR: Missing references";
            return;
        }

        // Get component references
        bossHealthSystem = boss.GetComponent<HealthSystem>();
        playerHealthSystem = player.GetComponent<HealthSystem>();
        bossScript = boss.GetComponent<Boss>();
        bossAnimator = boss.GetComponent<Animator>();

        if (bossHealthSystem == null || playerHealthSystem == null || bossScript == null || bossAnimator == null)
        {
            Debug.LogError("BossBattleSimulator: Missing required components!");
            statusMessage = "ERROR: Missing components";
            return;
        }

        // Store initial states
        initialBossPosition = boss.transform.position;
        initialPlayerPosition = player.transform.position;
        initialBossHealth = bossHealthSystem.MaxHealth;
        initialPlayerHealth = playerHealthSystem.MaxHealth;

        // Set simulation speed
        Time.timeScale = simulationSpeed;

        // Start simulation
        StartCoroutine(RunSimulation());
    }

    IEnumerator RunSimulation()
    {
        isSimulating = true;
        statusMessage = "Starting simulation...";

        Debug.Log($"BossBattleSimulator: Starting simulation of {battlesToSimulate} battles");

        for (currentBattle = 1; currentBattle <= battlesToSimulate; currentBattle++)
        {
            statusMessage = $"Battle {currentBattle}/{battlesToSimulate}";
            Debug.Log($"BossBattleSimulator: Starting battle {currentBattle}/{battlesToSimulate}");

            // Reset battle state
            ResetBattle();

            // Run battle
            yield return StartCoroutine(RunBattle());

            // Small delay between battles
            yield return new WaitForSeconds(1f);
        }

        // Simulation complete
        statusMessage = "Simulation complete! Saving data...";
        Debug.Log("BossBattleSimulator: All battles complete. Saving data...");

        SaveDataToFile();

        statusMessage = $"Complete! Collected {collectedData.Count} samples";
        Debug.Log($"BossBattleSimulator: Simulation complete. Collected {collectedData.Count} samples.");

        // Reset time scale
        Time.timeScale = 1f;

        isSimulating = false;
    }

    IEnumerator RunBattle()
    {
        battleStartTime = Time.time;
        lastActionTime = Time.time;

        while (Time.time - battleStartTime < maxBattleDuration)
        {
            // Check if battle is over
            if (bossHealthSystem.CurrentHealth <= 0 || playerHealthSystem.CurrentHealth <= 0)
            {
                Debug.Log($"BossBattleSimulator: Battle ended. Boss HP: {bossHealthSystem.CurrentHealth}, Player HP: {playerHealthSystem.CurrentHealth}");
                break;
            }

            // Make boss decision at intervals
            if (Time.time - lastActionTime >= actionInterval)
            {
                MakeBossDecision();
                lastActionTime = Time.time;
            }

            yield return null;
        }

        if (Time.time - battleStartTime >= maxBattleDuration)
        {
            Debug.Log("BossBattleSimulator: Battle timeout reached");
        }
    }

    void MakeBossDecision()
    {
        // Gather game state
        float distance = Vector3.Distance(boss.transform.position, player.transform.position);
        float bossHealthPct = (float)bossHealthSystem.CurrentHealth / bossHealthSystem.MaxHealth;
        float playerHealthPct = (float)playerHealthSystem.CurrentHealth / playerHealthSystem.MaxHealth;
        bool isPhase2 = bossAnimator.GetBool("Phase2");
        bool canUseLaser = bossScript.canUseLaser;
        float timeSinceLastAttack = float.IsNegativeInfinity(bossScript.lastAttackTime) 
            ? 999f 
            : Time.time - bossScript.lastAttackTime;

        string action;

        if (useSmartAI)
        {
            // Smart AI: Use rule-based logic matching Python training script
            action = DetermineSmartAction(distance, bossHealthPct, isPhase2, canUseLaser, timeSinceLastAttack);
        }
        else
        {
            // Random AI: Random valid actions
            action = DetermineRandomAction(isPhase2, canUseLaser, timeSinceLastAttack);
        }

        // Log the data
        LogGameState(distance, bossHealthPct, playerHealthPct, isPhase2, canUseLaser, timeSinceLastAttack, action);

        // Execute the action
        ExecuteAction(action);
    }

    string DetermineSmartAction(float distance, float bossHealthPct, bool isPhase2, bool canUseLaser, float timeSinceLastAttack)
    {
        // Phase 1: ONLY Attack
        if (!isPhase2)
        {
            return "Attack";
        }

        // Phase 2 logic
        bool laserAvailable = canUseLaser || timeSinceLastAttack >= 5.0f;

        // Close range: prefer Attack
        if (distance < 2.5f)
        {
            return "Attack";
        }

        // Medium range: prefer Combo
        if (distance < 5.0f)
        {
            return "Combo";
        }

        // Long range: prefer Laser if available, otherwise Combo
        if (laserAvailable)
        {
            return "Laser";
        }
        else
        {
            return "Combo";
        }
    }

    string DetermineRandomAction(bool isPhase2, bool canUseLaser, float timeSinceLastAttack)
    {
        if (!isPhase2)
        {
            // Phase 1: Only Attack
            return "Attack";
        }
        else
        {
            // Phase 2: Random between Attack, Combo, Laser
            List<string> validActions = new List<string> { "Attack", "Combo" };

            // Add Laser if available
            if (canUseLaser || timeSinceLastAttack >= 5.0f)
            {
                validActions.Add("Laser");
            }

            return validActions[Random.Range(0, validActions.Count)];
        }
    }

    void ExecuteAction(string action)
    {
        switch (action)
        {
            case "Attack":
                bossAnimator.SetTrigger("Attack");
                break;

            case "Combo":
                bossAnimator.ResetTrigger("Combo");
                bossAnimator.SetTrigger("Combo");
                break;

            case "Laser":
                if (bossScript.canUseLaser || bossScript.HasBeenIdleLongEnough())
                {
                    bossAnimator.SetTrigger("Laser");
                    bossScript.canUseLaser = false;
                    bossScript.NotifyLaserUsed();
                }
                break;
        }
    }

    void LogGameState(float distance, float bossHealthPct, float playerHealthPct, 
                      bool isPhase2, bool canUseLaser, float timeSinceLastAttack, string action)
    {
        string csvRow = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0:F2},{1:F3},{2:F3},{3},{4},{5:F2},{6}",
            distance,
            bossHealthPct,
            playerHealthPct,
            isPhase2 ? 1 : 0,
            canUseLaser ? 1 : 0,
            timeSinceLastAttack,
            action);

        collectedData.Add(csvRow);
    }

    void ResetBattle()
    {
        // Reset health
        bossHealthSystem.Initialize(initialBossHealth);
        playerHealthSystem.Initialize(initialPlayerHealth);

        // Reset positions
        boss.transform.position = initialBossPosition;
        player.transform.position = initialPlayerPosition;

        // Reset boss state
        bossScript.canUseLaser = true;
        bossScript.lastAttackTime = -Mathf.Infinity;
        bossAnimator.SetBool("Phase2", false);

        // Reset animator
        bossAnimator.Rebind();
        bossAnimator.Update(0f);

        Debug.Log("BossBattleSimulator: Battle state reset");
    }

    void SaveDataToFile()
    {
        if (collectedData.Count == 0)
        {
            Debug.LogWarning("BossBattleSimulator: No data to save");
            return;
        }

        try
        {
            // Get full path
            string fullPath = Path.Combine(Application.dataPath, "..", csvFilePath);

            // Ensure directory exists
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if file exists to determine if we need header
            bool fileExists = File.Exists(fullPath);

            StringBuilder sb = new StringBuilder();

            // Add header if file doesn't exist
            if (!fileExists)
            {
                sb.AppendLine(CSV_HEADER);
            }

            // Add all collected data
            foreach (string row in collectedData)
            {
                sb.AppendLine(row);
            }

            // Write to file (append mode)
            File.AppendAllText(fullPath, sb.ToString());

            Debug.Log($"BossBattleSimulator: Saved {collectedData.Count} samples to {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BossBattleSimulator: Failed to save data: {e.Message}");
        }
    }

    void OnGUI()
    {
        if (!runSimulation || !isSimulating) return;

        // Draw status box
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 16;
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.normal.textColor = Color.white;
        boxStyle.padding = new RectOffset(10, 10, 10, 10);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;

        float width = 300;
        float height = 120;
        float x = Screen.width - width - 10;
        float y = 10;

        GUI.Box(new Rect(x, y, width, height), "", boxStyle);

        float yPos = y + 10;
        float xPos = x + 10;

        GUI.Label(new Rect(xPos, yPos, width - 20, 25), "<b>Battle Simulator</b>", labelStyle);
        yPos += 30;

        GUI.Label(new Rect(xPos, yPos, width - 20, 20), statusMessage, labelStyle);
        yPos += 25;

        GUI.Label(new Rect(xPos, yPos, width - 20, 20), $"Samples: {collectedData.Count}", labelStyle);
        yPos += 25;

        GUI.Label(new Rect(xPos, yPos, width - 20, 20), $"Speed: {simulationSpeed}x", labelStyle);
    }

    void OnDestroy()
    {
        // Reset time scale when destroyed
        Time.timeScale = 1f;
    }
}
