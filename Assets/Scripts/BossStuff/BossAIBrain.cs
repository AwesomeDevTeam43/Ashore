using UnityEngine;
using Unity.Barracuda;
using System.Linq;

/// <summary>
/// Centralized AI Brain for Boss - takes 100% control when enabled.
/// Loads ONNX model, runs inference, and directly controls the animator.
/// 
/// SETUP INSTRUCTIONS:
/// 1. Add this component to the Boss GameObject
/// 2. Assign the ONNX model to modelAsset field
/// 3. Assign the Player GameObject to player field
/// 4. Add BossAIStateNotifier to all boss animator states (Attack, Combo, Idle, Laser)
/// 5. The old BossAIController can be removed/disabled
/// </summary>
public class BossAIBrain : MonoBehaviour
{
    [Header("Model Configuration")]
    [Tooltip("The trained ONNX model (NNModel asset)")]
    public NNModel modelAsset;

    [Header("References")]
    [Tooltip("The player GameObject")]
    public GameObject player;

    [Header("AI Settings")]
    [Tooltip("Decision interval in seconds")]
    public float decisionInterval = 0.5f;

    [Tooltip("Attack range threshold")]
    public float attackRange = 2f;

    [Header("Debug Settings")]
    [Tooltip("Show on-screen debug HUD")]
    public bool showOnScreenDebug = true;

    [Tooltip("Enable console debug logging")]
    public bool debugMode = true;

    [Header("Runtime Status (Read-Only)")]
    [SerializeField] [Tooltip("Current AI status")] private string _status = "Initializing...";
    [SerializeField] [Tooltip("Last AI decision")] private string _lastDecision = "None";
    [SerializeField] [Tooltip("Total decisions made")] private int _totalDecisionsMade = 0;
    [SerializeField] [Tooltip("Model confidence for last decision")] private float _modelConfidence = 0f;
    [SerializeField] [Tooltip("Is model loaded")] private bool _isModelLoaded = false;

    // Action labels (must match Python's LabelEncoder - alphabetical)
    private readonly string[] actionLabels = { "Attack", "Combo", "Idle", "Laser" };

    // Constant for indicating boss has never attacked (must match BossDataLogger)
    private const float NEVER_ATTACKED_TIME = 999f;

    // Barracuda runtime components
    private IWorker worker;
    private Model runtimeModel;

    // Component references
    private HealthSystem bossHealthSystem;
    private HealthSystem playerHealthSystem;
    private Boss bossScript;
    private Animator bossAnimator;
    private Rigidbody2D rb;

    // AI state
    private float lastDecisionTime = 0f;
    private bool isWaitingForActionComplete = false;
    private string currentAction = "";
    private float[] lastConfidences = new float[4]; // Store confidence for each action

    // Public properties
    public bool AIEnabled => _isModelLoaded && worker != null;
    public string CurrentAIAction => currentAction;

    private void Start()
    {
        // Get component references
        bossHealthSystem = GetComponent<HealthSystem>();
        bossScript = GetComponent<Boss>();
        bossAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (player != null)
        {
            playerHealthSystem = player.GetComponent<HealthSystem>();
        }

        // Validate references
        if (bossHealthSystem == null || playerHealthSystem == null || bossScript == null || bossAnimator == null)
        {
            Debug.LogError("🤖 BossAIBrain: Missing required components. AI cannot function.");
            _status = "ERROR: Missing Components";
            return;
        }

        // Load model
        if (modelAsset != null)
        {
            LoadModel();
        }
        else
        {
            Debug.LogWarning("🤖 BossAIBrain: No model asset assigned. AI is DISABLED.");
            _status = "No Model Assigned";
        }
    }

    /// <summary>
    /// Loads the ONNX model and creates a worker for inference
    /// </summary>
    private void LoadModel()
    {
        try
        {
            // Load the model
            runtimeModel = ModelLoader.Load(modelAsset);
            
            // Create a worker for inference
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
            
            _isModelLoaded = true;
            _status = "Active";
            
            // Print startup banner
            Debug.Log("========================================");
            Debug.Log("     🤖 BOSS AI BRAIN ACTIVATED 🤖");
            Debug.Log("========================================");
            Debug.Log($"     Model: {modelAsset.name}");
            Debug.Log($"     Actions: {string.Join(", ", actionLabels)}");
            Debug.Log("     AI is now in FULL CONTROL!");
            Debug.Log("     Random logic is DISABLED!");
            Debug.Log("========================================");
            
            if (debugMode)
            {
                Debug.Log($"🤖 Model inputs: {string.Join(", ", runtimeModel.inputs.Select(i => i.name))}");
                Debug.Log($"🤖 Model outputs: {string.Join(", ", runtimeModel.outputs)}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🤖 BossAIBrain: Failed to load model: {e.Message}");
            _status = "Model Load Failed";
            _isModelLoaded = false;
            worker = null;
        }
    }

    private void Update()
    {
        if (!AIEnabled) return;

        // Only make new decisions if not waiting for current action to complete
        if (isWaitingForActionComplete) return;

        // Check if it's time to make a new decision
        if (Time.time - lastDecisionTime >= decisionInterval)
        {
            MakeDecision();
            lastDecisionTime = Time.time;
        }

        // Handle movement in idle state
        if (currentAction == "Idle" || currentAction == "")
        {
            HandleMovement();
        }
    }

    /// <summary>
    /// Makes an AI decision and executes it
    /// </summary>
    private void MakeDecision()
    {
        // Gather game state features
        float[] features = GatherGameStateFeatures();
        
        if (features == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("🤖 BossAIBrain: Failed to gather game state features");
            }
            return;
        }

        // Create input tensor
        Tensor inputTensor = new Tensor(new TensorShape(1, 6), features);
        
        try
        {
            // Run inference
            worker.Execute(inputTensor);
            
            // Get output
            Tensor outputTensor = worker.PeekOutput();
            
            // Store confidences for HUD display
            for (int i = 0; i < Mathf.Min(actionLabels.Length, outputTensor.length); i++)
            {
                lastConfidences[i] = outputTensor[i];
            }
            
            // Find the predicted class (argmax)
            int predictedClass = GetArgMax(outputTensor);
            _modelConfidence = outputTensor[predictedClass];
            
            // Get action label
            if (predictedClass >= 0 && predictedClass < actionLabels.Length)
            {
                string predictedAction = actionLabels[predictedClass];
                _lastDecision = predictedAction;
                _totalDecisionsMade++;
                
                // Log decision
                if (debugMode)
                {
                    Debug.Log($"🤖 [AI Decision #{_totalDecisionsMade}] {predictedAction} (Confidence: {_modelConfidence * 100:F0}%) | " +
                             $"Dist:{features[0]:F1} BossHP:{features[1] * 100:F0}% PlayerHP:{features[2] * 100:F0}% " +
                             $"Phase2:{features[3]} Laser:{features[4]} TimeSince:{features[5]:F1}");
                }
                
                // Execute the action
                ExecuteAction(predictedAction);
            }
            else
            {
                Debug.LogError($"🤖 BossAIBrain: Invalid predicted class: {predictedClass}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🤖 BossAIBrain: Inference failed: {e.Message}");
        }
        finally
        {
            // Always dispose input tensor
            inputTensor.Dispose();
        }
    }

    /// <summary>
    /// Executes the predicted action by controlling the animator
    /// </summary>
    private void ExecuteAction(string action)
    {
        if (debugMode)
        {
            Debug.Log($"🤖 [AI Execute] {action}");
        }

        currentAction = action;
        isWaitingForActionComplete = true;

        switch (action)
        {
            case "Attack":
                // Check if in range first
                float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
                if (distToPlayer <= attackRange)
                {
                    bossAnimator.SetTrigger("Attack");
                }
                else
                {
                    // Not in range, go back to idle/chase
                    isWaitingForActionComplete = false;
                    currentAction = "Idle";
                }
                break;
                
            case "Combo":
                bossAnimator.ResetTrigger("Combo");
                bossAnimator.SetTrigger("Combo");
                break;
                
            case "Laser":
                // Check if laser is allowed
                if (bossScript.canUseLaser || bossScript.HasBeenIdleLongEnough())
                {
                    bossAnimator.SetTrigger("Laser");
                    bossScript.canUseLaser = false;
                    bossScript.NotifyLaserUsed();
                }
                else
                {
                    // Laser not available, fall back to idle
                    if (debugMode)
                    {
                        Debug.Log("🤖 BossAIBrain: Laser requested but not available");
                    }
                    isWaitingForActionComplete = false;
                    currentAction = "Idle";
                }
                break;
                
            case "Idle":
                // Just stay in idle/chase state
                isWaitingForActionComplete = false;
                break;
                
            default:
                Debug.LogWarning($"🤖 BossAIBrain: Unknown action '{action}'");
                isWaitingForActionComplete = false;
                break;
        }
    }

    /// <summary>
    /// Handles boss movement towards player (used during Idle state)
    /// </summary>
    private void HandleMovement()
    {
        if (player == null || rb == null) return;

        // Look at player
        bossScript.LookAtPlayer(player.transform);

        // Move towards player (use Time.deltaTime since we're in Update)
        Vector2 target = new Vector2(player.transform.position.x, rb.position.y);
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, 3 * Time.deltaTime);
        rb.MovePosition(newPos);
    }

    /// <summary>
    /// Called by BossAIStateNotifier when animations finish
    /// </summary>
    public void OnActionComplete()
    {
        if (debugMode)
        {
            Debug.Log($"🤖 [AI Action Complete] {currentAction}");
        }
        
        isWaitingForActionComplete = false;
        currentAction = "";
    }

    /// <summary>
    /// Gathers current game state features for AI input
    /// Features must be in the same order as training data
    /// </summary>
    private float[] GatherGameStateFeatures()
    {
        if (player == null || bossHealthSystem == null || playerHealthSystem == null || 
            bossScript == null || bossAnimator == null)
        {
            return null;
        }

        float[] features = new float[6];
        
        // Feature 0: distance_to_player
        features[0] = Vector3.Distance(transform.position, player.transform.position);
        
        // Feature 1: boss_health_pct
        features[1] = bossHealthSystem.MaxHealth > 0 
            ? (float)bossHealthSystem.CurrentHealth / bossHealthSystem.MaxHealth 
            : 1f;
        
        // Feature 2: player_health_pct
        features[2] = playerHealthSystem.MaxHealth > 0 
            ? (float)playerHealthSystem.CurrentHealth / playerHealthSystem.MaxHealth 
            : 1f;
        
        // Feature 3: is_phase2 (0 or 1)
        features[3] = bossAnimator.GetBool("Phase2") ? 1f : 0f;
        
        // Feature 4: can_use_laser (0 or 1)
        features[4] = bossScript.canUseLaser ? 1f : 0f;
        
        // Feature 5: time_since_last_attack
        // Boss.cs initializes lastAttackTime to -Mathf.Infinity, which equals float.NegativeInfinity
        if (float.IsNegativeInfinity(bossScript.lastAttackTime))
        {
            features[5] = NEVER_ATTACKED_TIME;
        }
        else
        {
            features[5] = Time.time - bossScript.lastAttackTime;
        }

        return features;
    }

    /// <summary>
    /// Finds the index of the maximum value in a tensor (argmax operation)
    /// </summary>
    private int GetArgMax(Tensor tensor)
    {
        int maxIndex = 0;
        float maxValue = float.MinValue;
        
        for (int i = 0; i < tensor.length; i++)
        {
            float value = tensor[i];
            if (value > maxValue)
            {
                maxValue = value;
                maxIndex = i;
            }
        }
        
        return maxIndex;
    }

    /// <summary>
    /// Draws on-screen debug HUD
    /// </summary>
    private void OnGUI()
    {
        if (!showOnScreenDebug) return;

        // Setup GUI style
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.normal.textColor = Color.white;
        boxStyle.padding = new RectOffset(10, 10, 10, 10);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.white;

        // Draw HUD box
        float hudWidth = 350;
        float hudHeight = 220;
        float padding = 10;
        Rect hudRect = new Rect(padding, padding, hudWidth, hudHeight);
        
        GUI.Box(hudRect, "", boxStyle);

        // Content
        float yPos = padding + 10;
        float xPos = padding + 10;

        GUI.Label(new Rect(xPos, yPos, hudWidth - 20, 25), "🤖 <b>BOSS AI BRAIN</b>", labelStyle);
        yPos += 25;

        GUI.Label(new Rect(xPos, yPos, hudWidth - 20, 20), $"Status: <b>{_status}</b>", labelStyle);
        yPos += 20;

        if (_isModelLoaded)
        {
            GUI.Label(new Rect(xPos, yPos, hudWidth - 20, 20), $"Model: {modelAsset.name}", labelStyle);
            yPos += 20;

            GUI.Label(new Rect(xPos, yPos, hudWidth - 20, 20), 
                     $"Last Decision: <b>{_lastDecision}</b> ({_modelConfidence * 100:F0}%)", labelStyle);
            yPos += 20;

            GUI.Label(new Rect(xPos, yPos, hudWidth - 20, 20), $"Total Decisions: {_totalDecisionsMade}", labelStyle);
            yPos += 25;

            // Confidence bars
            GUI.Label(new Rect(xPos, yPos, hudWidth - 20, 20), "<b>Action Confidences:</b>", labelStyle);
            yPos += 20;

            for (int i = 0; i < actionLabels.Length; i++)
            {
                float confidence = i < lastConfidences.Length ? lastConfidences[i] : 0f;
                DrawConfidenceBar(xPos, yPos, hudWidth - 20, 15, actionLabels[i], confidence);
                yPos += 18;
            }
        }
        else
        {
            GUI.Label(new Rect(xPos, yPos, hudWidth - 20, 20), "<color=yellow>AI Model Not Loaded</color>", labelStyle);
        }
    }

    /// <summary>
    /// Draws a confidence bar for a specific action
    /// </summary>
    private void DrawConfidenceBar(float x, float y, float width, float height, string label, float value)
    {
        // Normalize value to 0-1 range
        float normalizedValue = Mathf.Clamp01(value);

        // Background
        GUI.Box(new Rect(x, y, width, height), "");

        // Label
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 10;
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        
        GUI.Label(new Rect(x + 3, y, 60, height), label, labelStyle);

        // Bar
        float barX = x + 65;
        float barWidth = width - 135;
        Color barColor = normalizedValue > 0.5f ? Color.green : (normalizedValue > 0.3f ? Color.yellow : Color.red);
        
        GUI.color = barColor;
        GUI.Box(new Rect(barX, y + 2, barWidth * normalizedValue, height - 4), "");
        GUI.color = Color.white;

        // Percentage
        labelStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(barX + barWidth + 5, y, 60, height), $"{normalizedValue * 100:F0}%", labelStyle);
    }

    private void OnDestroy()
    {
        // Clean up worker
        if (worker != null)
        {
            worker.Dispose();
            worker = null;
        }
    }
}
