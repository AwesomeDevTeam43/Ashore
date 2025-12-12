# Boss AI Training Pipeline

This directory contains the complete AI training pipeline for the boss character, including data generation, model training, and automated battle simulation.

## Overview

The training pipeline provides three methods to collect and train AI models:
1. **Synthetic Data Generation** - Generate rule-based training data
2. **Automated Battle Simulation** - Collect data from simulated battles in Unity
3. **Manual Gameplay** - Log data during actual gameplay

## Files

### `train_boss_ai.py`
Python script that generates synthetic training data based on game rules and trains a RandomForest model.

**Features:**
- Generates 10,000 training samples based on game rules
- Validates Phase 1 only has Attack actions
- Trains a RandomForestClassifier (99%+ accuracy)
- Exports to ONNX format for Unity Barracuda
- Saves training data to CSV for inspection

**Game Rules Encoded:**
- **Phase 1** (HP > 50%): Only Attack
- **Phase 2** (HP ≤ 50%): Attack, Combo, Laser based on:
  - Attack: Close range (distance < 2.5)
  - Combo: Medium range (2.5 - 5.0)
  - Laser: Long range or when available (canUseLaser=true OR idle 5+ seconds)

## Usage

### Option 1: Generate Synthetic Data (Recommended)

Generate training data and train the model without needing Unity:

```bash
cd AI
python train_boss_ai.py
```

**Expected Output:**
```
==================================================
Boss AI Training Script
==================================================

Generating 10000 training samples...

Data Validation:
  Total samples: 10000
  Phase 1 samples: 5000
  Phase 2 samples: 5000

Phase 1 actions:
  Attack      5000  (100% - no Combo or Laser!)
  ✓ Phase 1 validation passed: No Combo or Laser actions

Phase 2 actions:
  Attack      ~1100
  Combo       ~1940
  Laser       ~1960

Action distribution:
  Attack      ~6100
  Combo       ~1940
  Laser       ~1960

Model Performance:
  Train Accuracy: 100.00%
  Test Accuracy: 99.95%

Feature Importance:
  distance_to_player        0.4030
  boss_health_pct           0.2548
  is_phase2                 0.2180
  can_use_laser             0.0540
  time_since_last_attack    0.0645
  player_health_pct         0.0057

Model exported to: boss_ai.onnx
```

**Next Steps:**
1. Copy `boss_ai.onnx` to Unity `Assets` folder
2. Import as NNModel asset in Unity
3. Assign to BossAIBrain component
4. Enable AI mode and play!

### Option 2: Simulate Battles in Unity

Use `BossBattleSimulator` to collect data from automated battles:

1. Open boss scene in Unity
2. Create empty GameObject and add `BossBattleSimulator` component
3. Assign Boss and Player GameObjects
4. Configure settings:
   - `Run Simulation`: Enable (checkbox)
   - `Battles To Simulate`: 10-100
   - `Simulation Speed`: 1-10x
   - `Use Smart AI`: Enable for rule-based data, disable for random
5. Play the scene
6. Wait for simulation to complete (data saved to `ml/boss_training_data.csv`)
7. Run `python train_boss_ai.py` with the collected data

**Features:**
- Automated battle reset (health, positions, animator)
- On-screen GUI showing progress
- Smart AI matches Python training rules
- Collects valid training data

### Option 3: Collect from Manual Play

Log data during actual gameplay:

1. Add `BossDataLogger` component to Boss GameObject (if not already present)
2. Configure settings:
   - `Enable Logging`: True
   - `Player`: Assign Player GameObject
   - `Boss`: Assign Boss GameObject
3. Play normally
4. Data auto-saves to `ml/boss_training_data.csv` on exit
5. Press F6 to manually save at any time
6. Run `python train_boss_ai.py` with collected data

**Features:**
- Validates data (skips Combo/Laser in Phase 1)
- Appends to existing CSV
- Auto-saves on scene exit or application quit
- Manual save with F6 key

## Model Architecture

**RandomForestClassifier:**
- 100 trees
- Max depth: 10
- Features: 6 (distance, boss HP%, player HP%, is_phase2, can_use_laser, time_since_attack)
- Actions: 3 (Attack, Combo, Laser)
- Accuracy: ~100% train, ~99% test

**Input Features:**
1. `distance_to_player` - Distance between boss and player
2. `boss_health_pct` - Boss health percentage (0-1)
3. `player_health_pct` - Player health percentage (0-1)
4. `is_phase2` - Phase indicator (0=Phase1, 1=Phase2)
5. `can_use_laser` - Laser availability (0=false, 1=true)
6. `time_since_last_attack` - Seconds since last attack (999 if never)

**Output:**
- Action prediction (Attack=0, Combo=1, Laser=2)

## Requirements

**Python Dependencies:**
```bash
pip install pandas numpy scikit-learn skl2onnx
```

**Unity:**
- Unity Barracuda package (for ONNX inference)
- BossAIBrain component (for AI control)

## Troubleshooting

**Problem: Model makes bad decisions (Combo in Phase 1)**
- Solution: Regenerate training data with `train_boss_ai.py`
- Verify data validation shows 100% Attack in Phase 1

**Problem: ONNX export fails**
- Solution: Check Python dependencies are installed correctly
- Try: `pip install --upgrade skl2onnx onnx`

**Problem: Low test accuracy**
- Solution: Increase training samples in `train_boss_ai.py` (change `NUM_SAMPLES`)
- Verify game rules match actual boss behavior

**Problem: Simulator doesn't collect data**
- Solution: Check Boss and Player are assigned
- Enable "Run Simulation" checkbox
- Check console for error messages

## Data Format

CSV format with header:
```
distance_to_player,boss_health_pct,player_health_pct,is_phase2,can_use_laser,time_since_last_attack,action
3.90,0.827,0.369,0,0,0.48,Attack
6.84,0.980,0.931,0,1,8.27,Attack
2.15,0.35,0.75,1,1,2.5,Combo
7.89,0.25,0.60,1,1,1.2,Laser
```

## File Locations

- Training script: `AI/train_boss_ai.py`
- Generated model: `AI/boss_ai.onnx` (not committed)
- Training data: `AI/boss_training_data.csv` (not committed)
- Battle simulator: `Assets/Scripts/BossStuff/BossBattleSimulator.cs`
- Data logger: `Assets/Scripts/BossStuff/BossDataLogger.cs`

## Notes

- Generated model files (.onnx, .csv) are excluded from git (see `.gitignore`)
- BossAIBrain automatically uses the ONNX model for inference
- Phase 1/2 rules are enforced in both Python and Unity
- Data logger validates and skips invalid actions (Combo/Laser in Phase 1)
