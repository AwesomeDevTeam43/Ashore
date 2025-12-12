"""
Boss AI Training Script using PyTorch
Exports clean ONNX compatible with Unity Barracuda! 
"""

import numpy as np
import pandas as pd
import torch
import torch. nn as nn
import torch.optim as optim
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
import os

# Configuration
ONNX_FILE = "boss_ai. onnx"
GENERATE_SYNTHETIC = False
NUM_SYNTHETIC_SAMPLES = 10000


# ============ Neural Network Model ============
class BossAIModel(nn.Module):
    def __init__(self, input_size, hidden_size, num_classes):
        super(BossAIModel, self).__init__()
        self.layer1 = nn. Linear(input_size, hidden_size)
        self.relu1 = nn.ReLU()
        self.layer2 = nn. Linear(hidden_size, hidden_size // 2)
        self.relu2 = nn.ReLU()
        self.layer3 = nn.Linear(hidden_size // 2, num_classes)
    
    def forward(self, x):
        x = self. layer1(x)
        x = self.relu1(x)
        x = self.layer2(x)
        x = self.relu2(x)
        x = self.layer3(x)
        return x


# ============ Data Generation ============
def generate_synthetic_data(num_samples:  int) -> pd.DataFrame:
    """Generate training data based on game rules."""
    data = []
    
    for _ in range(num_samples):
        distance = np.random. uniform(0.5, 15.0)
        boss_hp = np.random. uniform(0.1, 1.0)
        player_hp = np. random.uniform(0.1, 1.0)
        is_phase2 = 1.0 if boss_hp <= 0.5 else 0.0
        can_use_laser = float(np.random.choice([0, 1]))
        time_since_attack = np.random. uniform(0.0, 15.0)
        
        action = determine_action(distance, is_phase2, can_use_laser, time_since_attack)
        
        data.append({
            'distance_to_player': distance,
            'boss_health_pct': boss_hp,
            'player_health_pct': player_hp,
            'is_phase2': is_phase2,
            'can_use_laser': can_use_laser,
            'time_since_last_attack': time_since_attack,
            'action': action
        })
    
    return pd. DataFrame(data)


def determine_action(distance:  float, is_phase2: float, can_use_laser: float, time_since_attack:  float) -> str:
    """Determine optimal action based on game state."""
    
    # Phase 1: ONLY Attack
    if is_phase2 == 0.0:
        return "Attack"
    
    # Phase 2 logic
    laser_available = (can_use_laser == 1.0) or (time_since_attack >= 5.0)
    
    if distance <= 2.5:
        return "Attack" if np.random.random() < 0.6 else "Combo"
    elif distance <= 5.0:
        if laser_available and np.random.random() < 0.4:
            return "Laser"
        return "Combo" if np.random. random() < 0.5 else "Attack"
    else: 
        if laser_available and np.random.random() < 0.7:
            return "Laser"
        return "Attack" if np.random. random() < 0.7 else "Combo"


def validate_and_clean(df: pd.DataFrame) -> pd.DataFrame:
    """Validate and clean training data."""
    
    # Remove invalid Phase 1 actions
    invalid = df[(df['is_phase2'] == 0) & (df['action']. isin(['Combo', 'Laser']))]
    if len(invalid) > 0:
        print(f"Removing {len(invalid)} invalid Phase 1 actions")
        df = df[~((df['is_phase2'] == 0) & (df['action'].isin(['Combo', 'Laser'])))]
    
    # Remove any "Idle" actions
    if 'Idle' in df['action'].values:
        idle_count = len(df[df['action'] == 'Idle'])
        print(f"Removing {idle_count} Idle actions")
        df = df[df['action'] != 'Idle']
    
    df = df.dropna()
    
    print(f"\nCleaned data: {len(df)} samples")
    print(f"\nAction distribution:")
    print(df['action']. value_counts())
    
    return df


# ============ Training ============
def train_model(df: pd.DataFrame):
    """Train PyTorch model."""
    
    feature_cols = [
        'distance_to_player', 'boss_health_pct', 'player_health_pct',
        'is_phase2', 'can_use_laser', 'time_since_last_attack'
    ]
    
    X = df[feature_cols]. values. astype(np.float32)
    y = df['action']. values
    
    # Encode labels
    label_encoder = LabelEncoder()
    y_encoded = label_encoder. fit_transform(y)
    
    print(f"\nLabels: {list(label_encoder. classes_)}")
    print(f"Encoding: {dict(zip(label_encoder.classes_, range(len(label_encoder.classes_))))}")
    
    # Split
    X_train, X_test, y_train, y_test = train_test_split(
        X, y_encoded, test_size=0.2, random_state=42
    )
    
    # Convert to tensors
    X_train_t = torch.FloatTensor(X_train)
    y_train_t = torch.LongTensor(y_train)
    X_test_t = torch.FloatTensor(X_test)
    y_test_t = torch. LongTensor(y_test)
    
    # Create model
    input_size = 6
    hidden_size = 64
    num_classes = len(label_encoder.classes_)
    
    model = BossAIModel(input_size, hidden_size, num_classes)
    
    # Loss and optimizer
    criterion = nn.CrossEntropyLoss()
    optimizer = optim.Adam(model.parameters(), lr=0.001)
    
    # Training loop
    epochs = 200
    print(f"\nTraining for {epochs} epochs...")
    
    for epoch in range(epochs):
        model.train()
        optimizer.zero_grad()
        
        outputs = model(X_train_t)
        loss = criterion(outputs, y_train_t)
        
        loss.backward()
        optimizer.step()
        
        if (epoch + 1) % 50 == 0:
            model.eval()
            with torch.no_grad():
                train_pred = model(X_train_t).argmax(dim=1)
                train_acc = (train_pred == y_train_t).float().mean()
                
                test_pred = model(X_test_t).argmax(dim=1)
                test_acc = (test_pred == y_test_t).float().mean()
            
            print(f"  Epoch {epoch+1}/{epochs} - Loss: {loss.item():.4f} - Train Acc: {train_acc:.2%} - Test Acc: {test_acc:.2%}")
    
    # Final evaluation
    model. eval()
    with torch.no_grad():
        train_pred = model(X_train_t).argmax(dim=1)
        train_acc = (train_pred == y_train_t).float().mean()
        
        test_pred = model(X_test_t).argmax(dim=1)
        test_acc = (test_pred == y_test_t).float().mean()
    
    print(f"\nFinal Accuracy: Train={train_acc:.2%}, Test={test_acc:.2%}")
    
    return model, label_encoder


# ============ ONNX Export ============
def export_onnx(model, label_encoder, output_path: str):
    """Export to ONNX format compatible with Unity Barracuda."""
    
    model.eval()
    
    # Dummy input for tracing
    dummy_input = torch.randn(1, 6)
    
    # Export
    torch.onnx.export(
        model,
        dummy_input,
        output_path,
        export_params=True,
        opset_version=9,  # Barracuda works well with opset 9
        do_constant_folding=True,
        input_names=['float_input'],
        output_names=['output'],
        dynamic_axes={
            'float_input': {0: 'batch_size'},
            'output': {0: 'batch_size'}
        }
    )
    
    # Build the action labels string
    action_labels_str = '", "'.join(label_encoder.classes_)
    
    print(f"\n✅ Exported to:  {output_path}")
    print(f"Number of classes: {len(label_encoder.classes_)}")
    print(f"Actions (alphabetical order): {list(label_encoder. classes_)}")
    print("\n⚠️  IMPORTANT: Update BossAIBrain.cs actionLabels to match:")
    print(f'    private readonly string[] actionLabels = {{ "{action_labels_str}" }};')


# ============ Main ============
def main():
    print("=" * 50)
    print("BOSS AI TRAINING (PyTorch)")
    print("=" * 50)
    
    # Get data
    if GENERATE_SYNTHETIC:
        print(f"\nGenerating {NUM_SYNTHETIC_SAMPLES} synthetic samples...")
        df = generate_synthetic_data(NUM_SYNTHETIC_SAMPLES)
        df. to_csv("boss_training_data.csv", index=False)
        print("Saved to boss_training_data.csv")
    else:
        df = pd.read_csv("boss_training_data.csv")
        print(f"Loaded {len(df)} samples")
    
    # Clean
    df = validate_and_clean(df)
    
    # Train
    model, label_encoder = train_model(df)
    
    # Export
    export_onnx(model, label_encoder, ONNX_FILE)
    
    print("\n" + "=" * 50)
    print("DONE! Copy boss_ai.onnx to Unity Assets folder")
    print("=" * 50)


if __name__ == "__main__": 
    main()
