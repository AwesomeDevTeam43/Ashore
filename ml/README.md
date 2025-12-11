# Boss AI System - Machine Learning Integration

This system implements a machine learning-based decision system for the Boss enemy that replaces the random-based action selection with a neural network trained on gameplay data.

## Overview

The Boss AI system consists of three main components:

1. **Data Logger (Unity)**: Captures gameplay data during boss fights
2. **Training Script (Python)**: Trains a neural network on the captured data
3. **AI Brain (Unity)**: Centralized AI controller that takes 100% control when enabled

## System Architecture

```
Gameplay → BossDataLogger.cs → boss_training_data.csv
                                        ↓
                                train_boss_ai.py
                                        ↓
                                  boss_ai.onnx
                                        ↓
                              BossAIBrain.cs → Boss Actions
                                        ↑
                              BossAIStateNotifier.cs (attached to all animator states)
```

## Key Components

- **BossAIBrain.cs**: Centralized AI controller that runs in `Update()`, makes decisions every 0.5s, and directly controls the animator
- **BossAIStateNotifier.cs**: StateMachineBehaviour attached to all boss animator states to notify the AI when animations complete
- **Boss_RunIdle.cs**: Modified to check if AI is enabled and skip all logic when AI Brain is in control
- **BossDataLogger.cs**: Captures gameplay data for training
- **train_boss_ai.py**: Python script that trains the neural network

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
Copy this to your BossAIBrain.cs:

private readonly string[] actionLabels = { "Attack", "Combo", "Idle", "Laser" };

NOTE: Action labels MUST be in alphabetical order to match Python's LabelEncoder!
==================================================
```

**IMPORTANT**: The action labels are now: **Attack, Combo, Idle, Laser** (4 actions, alphabetical order).
The "Chase" action has been removed - chasing behavior is handled as part of the "Idle" state.

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

### Setting up the Centralized AI Brain

1. **Add BossAIBrain component to the Boss GameObject**:
   - Select the Boss GameObject
   - Click "Add Component" and search for "Boss AI Brain"

2. **Configure the AI Brain component**:
   - `Model Asset`: Drag the imported NNModel asset here
   - `Player`: Assign the Player GameObject
   - `Decision Interval`: Leave at 0.5 (AI makes decisions every 0.5 seconds)
   - `Attack Range`: Leave at 2.0 (distance threshold for attacks)
   - `Show On Screen Debug`: Enable to see AI status HUD in-game
   - `Debug Mode`: Enable to see AI decisions with 🤖 emoji in the console

3. **Add BossAIStateNotifier to ALL boss animator states**:
   - Open the Boss Animator Controller
   - For EACH state (Attack, Combo, Laser, Idle, etc.):
     - Click on the state
     - In Inspector, click "Add Behaviour"
     - Select "Boss AI State Notifier"
   - This notifies the AI Brain when animations finish

4. **Verify action labels** (should already be correct):
   - Open `BossAIBrain.cs`
   - Verify: `private readonly string[] actionLabels = { "Attack", "Combo", "Idle", "Laser" };`
   - Labels MUST be in alphabetical order!

5. **Old components (optional cleanup)**:
   - The old `BossAIController.cs` is no longer used and can be removed/disabled
   - `Boss_RunIdle.cs` now automatically detects and defers to the AI Brain

### Testing the AI Brain

1. **Enter Play Mode**:
   - You should see a startup banner in the console:
   ```
   ========================================
        🤖 BOSS AI BRAIN ACTIVATED 🤖
   ========================================
        Model: boss_ai
        Actions: Attack, Combo, Idle, Laser
        AI is now in FULL CONTROL!
        Random logic is DISABLED!
   ========================================
   ```

2. **Watch the AI work**:
   - If `showOnScreenDebug` is enabled, you'll see an on-screen HUD showing:
     - AI status (Active/Disabled)
     - Model name
     - Last decision with confidence percentage
     - Total decisions made
     - Confidence bars for each action
   - If `debugMode` is enabled, you'll see console logs with 🤖 emoji:
     ```
     🤖 [AI Decision #1] Attack (Confidence: 72%) | Dist:3.2 BossHP:100% PlayerHP:80% ...
     🤖 [AI Execute] ATTACK
     🤖 [AI Action Complete] Attack
     ```

3. **AI is now in control**:
   - The boss will use ONLY AI decisions (no random logic)
   - Boss_RunIdle automatically skips its logic when AI is enabled
   - The AI Brain handles movement, decision-making, and action execution

## How It Works

### Centralized AI Brain Decision Flow

The new **BossAIBrain** runs independently in `Update()` and takes 100% control:

1. **Every frame (Update)**:
   - Check if it's time to make a decision (every 0.5 seconds by default)
   - Check if waiting for current action to complete
   - If idle, handle movement towards player

2. **When making a decision**:
   - Gather 6 game state features:
     - `distance_to_player`
     - `boss_health_pct`
     - `player_health_pct`
     - `is_phase2` (0 or 1)
     - `can_use_laser` (0 or 1)
     - `time_since_last_attack`
   - Create input tensor (1, 6)
   - Run inference through the neural network
   - Get predicted action (argmax of output)
   - Log decision with 🤖 emoji
   - Execute action by controlling animator
   - Wait for action to complete

3. **Action execution**:
   - Directly trigger animator states: Attack, Combo, Laser, or Idle
   - Validate prerequisites (e.g., laser availability, attack range)
   - Handle movement during idle state
   - Set `isWaitingForActionComplete = true`

4. **Action completion**:
   - BossAIStateNotifier on each animator state calls `OnActionComplete()`
   - AI Brain sets `isWaitingForActionComplete = false`
   - Ready to make next decision

5. **Boss_RunIdle behavior**:
   - Checks if `BossAIBrain.AIEnabled` is true
   - If true: immediately returns (AI Brain handles everything)
   - If false: uses fallback random behavior (~33% Combo, ~33% Laser, ~33% Chase)

This ensures **zero conflict** between the AI Brain and state machine behaviors.

### Neural Network Architecture

```
Input Layer (6 features)
    ↓
Hidden Layer 1 (32 neurons + ReLU)
    ↓
Hidden Layer 2 (32 neurons + ReLU)
    ↓
Output Layer (4 classes: Attack, Combo, Idle, Laser)
    ↓
Softmax → Predicted Action
```

**Input Features** (must be in this exact order):
1. `distance_to_player` - Distance between boss and player
2. `boss_health_pct` - Boss health as percentage (0.0-1.0)
3. `player_health_pct` - Player health as percentage (0.0-1.0)
4. `is_phase2` - Whether boss is in phase 2 (0 or 1)
5. `can_use_laser` - Whether laser is available (0 or 1)
6. `time_since_last_attack` - Seconds since last attack (999 if never attacked)

**Output Classes** (alphabetical order - CRITICAL!):
1. Attack
2. Combo
3. Idle
4. Laser

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
- Ensure the model asset is assigned in BossAIBrain
- Check console for 🤖 startup banner - if missing, model didn't load
- Verify model file is not corrupted

### Issue: AI not making decisions

**Solution**:
- Check that BossAIBrain component is attached to Boss GameObject
- Verify all required references are assigned (Player, model asset)
- Enable `debugMode` to see decision logs
- Check Inspector read-only fields: `_status` should be "Active", `_isModelLoaded` should be true
- Verify BossAIStateNotifier is attached to ALL animator states

### Issue: Boss still using random behavior

**Solution**:
- Check that BossAIBrain.AIEnabled is true (visible in Inspector)
- Verify model loaded successfully (check console for startup banner)
- Ensure Boss_RunIdle is detecting the AI Brain (should log "AI Brain is in control")
- Check that all required components exist: HealthSystem, Boss script, Animator

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
- Verify action labels array is exactly: `{ "Attack", "Combo", "Idle", "Laser" }` in alphabetical order
- Check that feature order matches training data (6 features in correct order)
- Verify NEVER_ATTACKED_TIME constant is 999f in both BossAIBrain and BossDataLogger

### Issue: Actions not completing / AI stuck

**Solution**:
- Verify BossAIStateNotifier is attached to ALL animator states
- Check that OnStateExit is being called (can add debug logs)
- Ensure animator transitions are working correctly
- Check that `isWaitingForActionComplete` is being reset (visible in debugger)

## Advanced Usage

### On-Screen Debug HUD

When `showOnScreenDebug` is enabled in BossAIBrain, you'll see a real-time HUD showing:

- **AI Status**: Active, Disabled, or Error state
- **Model Name**: The name of the loaded ONNX model
- **Last Decision**: Most recent action with confidence percentage
- **Total Decisions**: Counter of all decisions made
- **Confidence Bars**: Visual bars showing confidence for each action
  - Green: >50% confidence
  - Yellow: 30-50% confidence
  - Red: <30% confidence

This provides clear visual feedback that the ONNX model is running and making decisions.

### Console Logging

When `debugMode` is enabled, all AI activity is logged with the 🤖 emoji for easy identification:

```
🤖 [AI Decision #1] Attack (Confidence: 72%) | Dist:3.2 BossHP:100% PlayerHP:80% Phase2:0 Laser:1 TimeSince:5.2
🤖 [AI Execute] ATTACK
🤖 [AI Action Complete] Attack
```

This makes it easy to:
- Verify the model is running
- Debug decision-making patterns
- Understand why specific actions were chosen
- Track AI performance over time

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

### Hybrid Approach (Not Recommended with BossAIBrain)

The new BossAIBrain is designed for 100% AI control. However, if you want to disable the AI and use random behavior:

1. **Remove or disable BossAIBrain component**:
   - This will cause Boss_RunIdle to fall back to random behavior
   - Boss_RunIdle automatically detects when AI is disabled

2. **Adjust decision interval** for more/less frequent decisions:
   - Change `decisionInterval` in BossAIBrain (default: 0.5 seconds)
   - Lower values = more reactive AI
   - Higher values = more deliberate/strategic AI

**Note**: The centralized architecture is designed to avoid the hybrid approach issues that existed in the old mixed AI/random system. It's recommended to use 100% AI or 100% random, not a mix.

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
