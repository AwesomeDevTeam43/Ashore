using UnityEngine;
using Unity.Barracuda;
using System.Linq;

/// <summary>
/// Controls the Boss AI using a trained neural network model.
/// Loads an ONNX model and performs inference to predict boss actions based on game state.
/// </summary>
public class BossAIController : MonoBehaviour
{
    [Header("Model Configuration")]
    [Tooltip("The trained ONNX model (NNModel asset)")]
    public NNModel modelAsset;

    [Header("References")]
    [Tooltip("The player GameObject")]
    public GameObject player;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool debugMode = false;

    // Action labels (must match the order from Python's LabelEncoder - alphabetical by default)
    // Update this array based on the output from train_boss_ai.py
    private string[] actionLabels = new string[] {
        "Attack",
        "Chase",
        "Combo",
        "Idle",
        "Laser"
    };

    // Barracuda runtime components
    private IWorker worker;
    private Model runtimeModel;

    // Component references
    private HealthSystem bossHealthSystem;
    private HealthSystem playerHealthSystem;
    private Boss bossScript;
    private Animator bossAnimator;

    private void Start()
    {
        // Load model
        if (modelAsset != null)
        {
            LoadModel();
        }
        else
        {
            Debug.LogWarning("BossAIController: No model asset assigned. AI decisions will not be available.");
        }

        // Get component references
        bossHealthSystem = GetComponent<HealthSystem>();
        bossScript = GetComponent<Boss>();
        bossAnimator = GetComponent<Animator>();

        if (player != null)
        {
            playerHealthSystem = player.GetComponent<HealthSystem>();
        }

        // Validate references
        if (bossHealthSystem == null || playerHealthSystem == null || bossScript == null || bossAnimator == null)
        {
            Debug.LogWarning("BossAIController: Missing required components. AI may not work correctly.");
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
            
            // Create a worker for inference (ComputePrecompiled is fastest but CPU fallback is more compatible)
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
            
            Debug.Log("BossAIController: Model loaded successfully");
            
            if (debugMode)
            {
                Debug.Log($"Model inputs: {string.Join(", ", runtimeModel.inputs.Select(i => i.name))}");
                Debug.Log($"Model outputs: {string.Join(", ", runtimeModel.outputs)}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BossAIController: Failed to load model: {e.Message}");
            worker = null;
        }
    }

    /// <summary>
    /// Gets an AI decision based on current game state
    /// </summary>
    /// <returns>The predicted action as a string, or null if AI is not available</returns>
    public string GetAIDecision()
    {
        if (worker == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("BossAIController: Worker not available, cannot make AI decision");
            }
            return null;
        }

        // Gather game state features
        float[] features = GatherGameStateFeatures();
        
        if (features == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("BossAIController: Failed to gather game state features");
            }
            return null;
        }

        // Create input tensor
        Tensor inputTensor = new Tensor(1, 6, features);
        
        try
        {
            // Run inference
            worker.Execute(inputTensor);
            
            // Get output
            Tensor outputTensor = worker.PeekOutput();
            
            // Find the predicted class (argmax)
            int predictedClass = GetArgMax(outputTensor);
            
            // Dispose tensors
            inputTensor.Dispose();
            
            // Get action label
            if (predictedClass >= 0 && predictedClass < actionLabels.Length)
            {
                string predictedAction = actionLabels[predictedClass];
                
                if (debugMode)
                {
                    Debug.Log($"BossAIController: AI Decision = {predictedAction} (class {predictedClass})");
                    Debug.Log($"Features: [{string.Join(", ", features.Select(f => f.ToString("F2")))}]");
                }
                
                return predictedAction;
            }
            else
            {
                Debug.LogError($"BossAIController: Invalid predicted class: {predictedClass}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BossAIController: Inference failed: {e.Message}");
            inputTensor.Dispose();
            return null;
        }
    }

    /// <summary>
    /// Gathers current game state features for AI input
    /// Features must be in the same order as training data
    /// </summary>
    private float[] GatherGameStateFeatures()
    {
        if (player == null || bossHealthSystem == null || playerHealthSystem == null || bossScript == null || bossAnimator == null)
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
        
        // Feature 3: is_phase2 (1 or 0)
        features[3] = bossAnimator.GetBool("Phase2") ? 1f : 0f;
        
        // Feature 4: can_use_laser (1 or 0)
        features[4] = bossScript.canUseLaser ? 1f : 0f;
        
        // Feature 5: time_since_last_attack
        if (float.IsNegativeInfinity(bossScript.lastAttackTime))
        {
            features[5] = 999f; // Large value if never attacked
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

    private void OnDestroy()
    {
        // Clean up worker
        if (worker != null)
        {
            worker.Dispose();
            worker = null;
        }
    }

    /// <summary>
    /// Validates if the AI controller is ready to make decisions
    /// </summary>
    public bool IsReady()
    {
        return worker != null && player != null && bossHealthSystem != null && 
               playerHealthSystem != null && bossScript != null && bossAnimator != null;
    }
}
