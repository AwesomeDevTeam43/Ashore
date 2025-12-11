"""
Boss AI Training Script
Generates synthetic training data based on game rules and trains a RandomForestClassifier.
Exports the trained model to ONNX format for use with Unity Barracuda.
"""

import pandas as pd
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
import os
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType


def determine_action(distance_to_player, boss_health_pct, player_health_pct, 
                     is_phase2, can_use_laser, time_since_last_attack):
    """
    Determines the boss action based on game rules.
    
    Game Rules:
    - Phase 1 (HP > 50%): ONLY Attack
    - Phase 2 (HP <= 50%): Attack, Combo, Laser based on conditions
    - Attack: Preferred when close to player (distance < 2.5)
    - Combo: Medium range, Phase 2 only
    - Laser: Any range, Phase 2 only, when available or idle for 5+ seconds
    
    Returns:
        str: Action name ("Attack", "Combo", or "Laser")
    """
    # Phase 1: ONLY Attack
    if is_phase2 == 0.0:
        return "Attack"
    
    # Phase 2 logic
    # Laser available conditions: can_use_laser OR idle for 5+ seconds
    laser_available = can_use_laser == 1.0 or time_since_last_attack >= 5.0
    
    # Close range: prefer Attack
    if distance_to_player < 2.5:
        return "Attack"
    
    # Medium range (2.5 - 5.0): prefer Combo
    if distance_to_player < 5.0:
        return "Combo"
    
    # Long range: prefer Laser if available, otherwise Combo
    if laser_available:
        return "Laser"
    else:
        return "Combo"


def generate_training_data(num_samples=10000):
    """
    Generates synthetic training data based on game rules.
    
    Args:
        num_samples: Number of training samples to generate
        
    Returns:
        pd.DataFrame: Training data with features and labels
    """
    print(f"Generating {num_samples} training samples...")
    
    data = []
    
    # Generate roughly 50% Phase 1, 50% Phase 2 samples
    phase1_samples = num_samples // 2
    phase2_samples = num_samples - phase1_samples
    
    # Generate Phase 1 samples (ONLY Attack)
    for _ in range(phase1_samples):
        distance = np.random.uniform(0.5, 10.0)
        boss_hp = np.random.uniform(0.51, 1.0)  # Phase 1: > 50%
        player_hp = np.random.uniform(0.1, 1.0)
        is_phase2 = 0.0
        can_use_laser = float(np.random.choice([0, 1]))
        time_since_attack = np.random.uniform(0.0, 10.0)
        
        action = determine_action(distance, boss_hp, player_hp, 
                                 is_phase2, can_use_laser, time_since_attack)
        
        data.append({
            'distance_to_player': distance,
            'boss_health_pct': boss_hp,
            'player_health_pct': player_hp,
            'is_phase2': is_phase2,
            'can_use_laser': can_use_laser,
            'time_since_last_attack': time_since_attack,
            'action': action
        })
    
    # Generate Phase 2 samples (Attack, Combo, Laser)
    for _ in range(phase2_samples):
        distance = np.random.uniform(0.5, 10.0)
        boss_hp = np.random.uniform(0.1, 0.5)  # Phase 2: <= 50%
        player_hp = np.random.uniform(0.1, 1.0)
        is_phase2 = 1.0
        can_use_laser = float(np.random.choice([0, 1]))
        time_since_attack = np.random.uniform(0.0, 10.0)
        
        action = determine_action(distance, boss_hp, player_hp, 
                                 is_phase2, can_use_laser, time_since_attack)
        
        data.append({
            'distance_to_player': distance,
            'boss_health_pct': boss_hp,
            'player_health_pct': player_hp,
            'is_phase2': is_phase2,
            'can_use_laser': can_use_laser,
            'time_since_last_attack': time_since_attack,
            'action': action
        })
    
    df = pd.DataFrame(data)
    return df


def validate_data(df):
    """
    Validates that training data follows game rules.
    
    Args:
        df: Training data DataFrame
    """
    print("\nData Validation:")
    print(f"  Total samples: {len(df)}")
    
    # Phase 1 and Phase 2 counts
    phase1_df = df[df['is_phase2'] == 0.0]
    phase2_df = df[df['is_phase2'] == 1.0]
    
    print(f"  Phase 1 samples: {len(phase1_df)}")
    print(f"  Phase 2 samples: {len(phase2_df)}")
    
    # Check Phase 1 only has Attack
    phase1_actions = phase1_df['action'].value_counts()
    print("\nPhase 1 actions:")
    for action, count in phase1_actions.items():
        print(f"  {action:10s} {count:5d}  (100% - no Combo or Laser!)" if action == "Attack" 
              else f"  {action:10s} {count:5d}  (ERROR: Should be 0!)")
    
    # Verify no Combo or Laser in Phase 1
    phase1_invalid = phase1_df[phase1_df['action'].isin(['Combo', 'Laser'])]
    if len(phase1_invalid) > 0:
        print(f"\n  ⚠️ WARNING: Found {len(phase1_invalid)} invalid actions in Phase 1!")
    else:
        print("  ✓ Phase 1 validation passed: No Combo or Laser actions")
    
    # Phase 2 action distribution
    phase2_actions = phase2_df['action'].value_counts()
    print("\nPhase 2 actions:")
    for action, count in phase2_actions.items():
        print(f"  {action:10s} {count:5d}")
    
    # Overall action distribution
    print("\nAction distribution:")
    action_counts = df['action'].value_counts()
    for action, count in action_counts.items():
        print(f"  {action:10s} {count:5d}")


def train_model(df):
    """
    Trains a RandomForestClassifier on the training data.
    
    Args:
        df: Training data DataFrame
        
    Returns:
        tuple: (trained model, label encoder, train accuracy, test accuracy)
    """
    print("\nPreparing data for training...")
    
    # Feature columns
    feature_columns = [
        'distance_to_player',
        'boss_health_pct',
        'player_health_pct',
        'is_phase2',
        'can_use_laser',
        'time_since_last_attack'
    ]
    
    X = df[feature_columns].values
    y = df['action'].values
    
    # Encode labels
    label_encoder = LabelEncoder()
    y_encoded = label_encoder.fit_transform(y)
    
    print(f"\nLabel encoding mapping:")
    for i, label in enumerate(label_encoder.classes_):
        print(f"  {i}: {label}")
    
    # Split data
    X_train, X_test, y_train, y_test = train_test_split(
        X, y_encoded, test_size=0.2, random_state=42, stratify=y_encoded
    )
    
    print(f"\nTraining set: {len(X_train)} samples")
    print(f"Test set: {len(X_test)} samples")
    
    # Train RandomForestClassifier
    print("\nTraining RandomForestClassifier...")
    model = RandomForestClassifier(
        n_estimators=100,
        max_depth=10,
        random_state=42,
        n_jobs=-1
    )
    model.fit(X_train, y_train)
    
    # Evaluate
    train_accuracy = model.score(X_train, y_train)
    test_accuracy = model.score(X_test, y_test)
    
    print(f"\nModel Performance:")
    print(f"  Train Accuracy: {train_accuracy:.2%}")
    print(f"  Test Accuracy: {test_accuracy:.2%}")
    
    # Feature importance
    print(f"\nFeature Importance:")
    for i, importance in enumerate(model.feature_importances_):
        print(f"  {feature_columns[i]:25s} {importance:.4f}")
    
    return model, label_encoder, train_accuracy, test_accuracy


def export_to_onnx(model, label_encoder, output_path):
    """
    Exports the trained RandomForest model to ONNX format.
    
    Args:
        model: Trained sklearn model
        label_encoder: Label encoder used for training
        output_path: Path to save ONNX file
    """
    print(f"\nExporting model to ONNX format: {output_path}")
    
    # Define input type
    initial_type = [('float_input', FloatTensorType([None, 6]))]
    
    # Convert to ONNX
    onx = convert_sklearn(
        model,
        initial_types=initial_type,
        target_opset=11
    )
    
    # Save ONNX file
    with open(output_path, "wb") as f:
        f.write(onx.SerializeToString())
    
    print(f"Model exported successfully!")
    print(f"Model file size: {os.path.getsize(output_path) / 1024:.2f} KB")


def main():
    """Main training pipeline."""
    print("=" * 50)
    print("Boss AI Training Script")
    print("=" * 50)
    
    # Configuration
    NUM_SAMPLES = 10000
    CSV_OUTPUT_PATH = "boss_training_data.csv"
    ONNX_OUTPUT_PATH = "boss_ai.onnx"
    
    # Generate training data
    df = generate_training_data(NUM_SAMPLES)
    
    # Validate data
    validate_data(df)
    
    # Save training data to CSV
    print(f"\nSaving training data to {CSV_OUTPUT_PATH}...")
    df.to_csv(CSV_OUTPUT_PATH, index=False)
    print(f"Training data saved!")
    
    # Train model
    model, label_encoder, train_acc, test_acc = train_model(df)
    
    # Export to ONNX
    export_to_onnx(model, label_encoder, ONNX_OUTPUT_PATH)
    
    # Print summary
    print("\n" + "=" * 50)
    print("Training Complete!")
    print("=" * 50)
    print(f"\nFiles created:")
    print(f"  - {CSV_OUTPUT_PATH} ({len(df)} samples)")
    print(f"  - {ONNX_OUTPUT_PATH}")
    
    print(f"\nModel exported to: {ONNX_OUTPUT_PATH}")
    print("\nNext steps:")
    print("1. Copy boss_ai.onnx to Unity Assets folder")
    print("2. Import as NNModel asset in Unity")
    print("3. Assign to BossAIBrain component")
    print("4. The model will respect Phase 1/2 rules automatically!")


if __name__ == "__main__":
    main()
