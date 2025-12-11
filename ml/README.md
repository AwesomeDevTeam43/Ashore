# Boss AI System - Machine Learning Integration

This system implements a machine learning-based decision system for the Boss enemy that replaces the random-based action selection with a neural network trained on gameplay data.

## Overview

The Boss AI system consists of three main components:

1. **Data Logger (Unity)**: Captures gameplay data during boss fights
2. **Training Script (Python)**: Trains a neural network on the captured data
3. **AI Controller (Unity)**: Uses the trained model to make real-time decisions

## System Architecture

```
Gameplay → BossDataLogger.cs → boss_training_data.csv
                                        ↓
                                train_boss_ai.py
                                        ↓
                                  boss_ai.onnx
                                        ↓
                              BossAIController.cs → Boss Actions
```

## Getting Started

### Prerequisites

- Unity with Barracuda package installed (`com.unity.barracuda`)
- Python 3.8+ with uv package manager
- Training data from gameplay

### Installing Unity Barracuda

1. Open Unity Package Manager (Window → Package Manager)
2. Click the "+" button and select "Add package by name..."
3. Enter: `com.unity.barracuda`
4. Click "Add"

### Installing Python Dependencies

```bash
cd ml
uv sync
```

This will install:
- PyTorch
- Pandas
- Scikit-learn
- ONNX

## Step 1: Collecting Training Data

### Setting up the Data Logger

1. **Add BossDataLogger component to the Boss GameObject**:
   - In Unity, select the Boss GameObject in the hierarchy
   - Click "Add Component" and search for "Boss Data Logger"
   - Assign the Player and Boss GameObjects to the component fields

2. **Configure the logger**:
   - `Enable Logging`: Check this to start logging data
   - `Player`: Drag the Player GameObject here
   - `Boss`: Drag the Boss GameObject here
   - `CSV File Path`: Default is `ml/boss_training_data.csv`

3. **Play the game**:
   - Enter Play Mode and fight the boss
   - The logger will capture game state and actions
   - Data is buffered and written when you exit Play Mode

4. **Repeat for more data**:
   - Play multiple boss fights to collect diverse training data
   - Different strategies and scenarios improve the model
   - Aim for at least 100-200 samples of each action type

### Data Format

The CSV file contains these features:

| Feature | Description | Range |
|---------|-------------|-------|
| `distance_to_player` | Distance between boss and player | 0-∞ |
| `boss_health_pct` | Boss health percentage | 0.0-1.0 |
| `player_health_pct` | Player health percentage | 0.0-1.0 |
| `is_phase2` | Whether boss is in phase 2 | 0 or 1 |
| `can_use_laser` | Whether laser is available | 0 or 1 |
| `time_since_last_attack` | Seconds since last attack | 0-∞ |
| `action` | Boss action performed | Attack/Chase/Combo/Idle/Laser |

## Step 2: Training the Model

### Running the Training Script

```bash
cd ml
uv run train_boss_ai.py
```

The script will:
1. Load data from `boss_training_data.csv`
2. Split into training and validation sets
3. Train a neural network for 100 epochs
4. Export the trained model to `boss_ai.onnx`
5. Print the action label mapping for Unity

### Training Output

The script displays:
- Data statistics and class distribution
- Training progress (loss and accuracy)
- Final model performance
- Action label mapping to copy into Unity

Example output:
```
ACTION CLASS MAPPING FOR UNITY
==================================================
Copy this to your BossAIController.cs:

private string[] actionLabels = new string[] {
    "Attack",
    "Chase",
    "Combo",
    "Idle",
    "Laser",
};
==================================================
```

### Training Parameters

You can adjust these in `train_boss_ai.py`:

- `EPOCHS`: Number of training iterations (default: 100)
- `LEARNING_RATE`: Learning rate for optimizer (default: 0.001)
- `hidden_size`: Number of neurons in hidden layers (default: 32)

## Step 3: Using the Trained Model in Unity

### Importing the Model

1. **Locate the ONNX file**:
   - Find `ml/boss_ai.onnx` in your project directory

2. **Import into Unity**:
   - Drag `boss_ai.onnx` into Unity's Assets folder (e.g., `Assets/ML/boss_ai.onnx`)
   - Unity will automatically convert it to an NNModel asset

### Setting up the AI Controller

1. **Add BossAIController component to the Boss GameObject**:
   - Select the Boss GameObject
   - Click "Add Component" and search for "Boss AI Controller"

2. **Configure the component**:
   - `Model Asset`: Drag the imported NNModel asset here
   - `Player`: Assign the Player GameObject
   - `Debug Mode`: Enable to see AI decisions in the console

3. **Update action labels** (if needed):
   - Open `BossAIController.cs`
   - Copy the action labels from the training script output
   - Replace the `actionLabels` array in the script

### Enabling AI Mode

1. **Configure Boss_RunIdle behavior**:
   - Select the Boss GameObject
   - Find the Boss_RunIdle state in the Animator Controller
   - In the Inspector, find the Boss_RunIdle script properties
   - Check the `Use AI` checkbox

2. **Test the AI**:
   - Enter Play Mode
   - The boss will now use AI decisions instead of random actions
   - Watch the console for debug messages (if debug mode is enabled)

## How It Works

### Decision Flow

When the boss enters the Run/Idle state:

1. **AI Mode Enabled**:
   - Boss_RunIdle calls `BossAIController.GetAIDecision()`
   - AI Controller gathers current game state features
   - Creates input tensor from features
   - Runs inference through the neural network
   - Returns predicted action (e.g., "Combo", "Laser")
   - Boss_RunIdle executes the predicted action

2. **AI Mode Disabled** (fallback):
   - Uses original random behavior (~33% Combo, ~33% Laser, ~33% Chase)

### Neural Network Architecture

```
Input Layer (6 features)
    ↓
Hidden Layer 1 (32 neurons + ReLU)
    ↓
Hidden Layer 2 (32 neurons + ReLU)
    ↓
Output Layer (5 classes)
    ↓
Softmax → Predicted Action
```

## Troubleshooting

### Issue: No training data collected

**Solution**: 
- Ensure BossDataLogger component is attached to Boss
- Check "Enable Logging" is checked
- Verify Player and Boss references are assigned
- Play the game and perform boss actions

### Issue: Training script fails to load data

**Solution**:
- Check that `boss_training_data.csv` exists in the `ml/` folder
- Ensure the CSV has the correct header row
- Verify you have collected some training data

### Issue: Model not loading in Unity

**Solution**:
- Verify Unity Barracuda package is installed
- Check that the ONNX file was imported correctly (should show as NNModel)
- Ensure the model asset is assigned in BossAIController

### Issue: AI makes poor decisions

**Solution**:
- Collect more training data (especially for underrepresented actions)
- Try different gameplay strategies during data collection
- Retrain the model with more epochs
- Adjust neural network architecture (more hidden units)

### Issue: Barracuda errors during inference

**Solution**:
- Ensure ONNX export uses opset version 11 or lower
- Check that input tensor shape matches model expectations (1, 6)
- Verify action labels array matches the training output exactly

## Advanced Usage

### Collecting Better Training Data

To train a more effective model:

1. **Diverse scenarios**: Fight the boss with different strategies
2. **Both phases**: Collect data from phase 1 and phase 2
3. **Health variations**: Capture states with varying health levels
4. **Balanced actions**: Ensure all actions are well-represented
5. **Multiple sessions**: Combine data from multiple gameplay sessions

### Fine-tuning the Model

You can experiment with:

- **Network size**: Increase hidden layer size for more capacity
- **Training time**: More epochs for better convergence
- **Learning rate**: Lower for more stable training, higher for faster convergence
- **Architecture**: Add more hidden layers or different activation functions

### Hybrid Approach

You can implement a hybrid system that:
- Uses AI for strategic decisions (Combo/Laser timing)
- Falls back to random for exploration
- Adds randomness to AI decisions for unpredictability

Example in Boss_RunIdle.cs:
```csharp
// 80% AI decision, 20% random
if (Random.value < 0.8f && useAI && aiController.IsReady())
{
    string aiDecision = aiController.GetAIDecision();
    ExecuteAction(animator, aiDecision);
}
else
{
    ExecuteRandomAction(animator);
}
```

## Performance Considerations

- **Inference speed**: Barracuda inference is fast enough for real-time decisions
- **Memory usage**: The model is small (~5-10 KB) and has minimal memory overhead
- **CPU vs GPU**: Barracuda can use GPU acceleration if available
- **Model optimization**: Use ComputePrecompiled worker type for best performance

## Future Improvements

Potential enhancements:

1. **Reinforcement Learning**: Train the model through self-play
2. **Dynamic difficulty**: Adjust AI behavior based on player skill
3. **Multi-agent systems**: Coordinate multiple boss phases
4. **Behavioral cloning**: Train on expert player strategies
5. **Explainable AI**: Visualize why the AI makes certain decisions

## References

- [Unity Barracuda Documentation](https://docs.unity3d.com/Packages/com.unity.barracuda@3.0/manual/index.html)
- [PyTorch ONNX Export](https://pytorch.org/docs/stable/onnx.html)
- [ONNX Format](https://onnx.ai/)

## License

This AI system is part of the Ashore game project.
