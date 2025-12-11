"""
Boss AI Training Script
Trains a neural network to predict boss actions based on game state features.
Exports the trained model to ONNX format for use with Unity Barracuda.
"""

import pandas as pd
import torch
import torch.nn as nn
import torch.optim as optim
from sklearn.preprocessing import LabelEncoder
from sklearn.model_selection import train_test_split
import numpy as np
import os


class BossAINetwork(nn.Module):
    """
    Simple neural network for boss action prediction.
    
    Architecture:
    - Input layer: 6 features
    - Hidden layer 1: 32 neurons with ReLU
    - Hidden layer 2: 32 neurons with ReLU
    - Output layer: number of action classes
    """
    
    def __init__(self, input_size=6, hidden_size=32, output_size=5):
        super(BossAINetwork, self).__init__()
        self.fc1 = nn.Linear(input_size, hidden_size)
        self.relu1 = nn.ReLU()
        self.fc2 = nn.Linear(hidden_size, hidden_size)
        self.relu2 = nn.ReLU()
        self.fc3 = nn.Linear(hidden_size, output_size)
    
    def forward(self, x):
        x = self.fc1(x)
        x = self.relu1(x)
        x = self.fc2(x)
        x = self.relu2(x)
        x = self.fc3(x)
        return x


def load_data(csv_path):
    """Load and prepare training data from CSV file."""
    print(f"Loading data from {csv_path}...")
    
    if not os.path.exists(csv_path):
        raise FileNotFoundError(f"Training data file not found: {csv_path}")
    
    df = pd.read_csv(csv_path)
    print(f"Loaded {len(df)} samples")
    print(f"Columns: {list(df.columns)}")
    
    # Display class distribution
    print("\nAction distribution:")
    print(df['action'].value_counts())
    
    return df


def prepare_data(df):
    """
    Prepare features and labels for training.
    
    Returns:
        X: Feature array (numpy)
        y: Label array (numpy)
        label_encoder: Fitted LabelEncoder for converting labels back to strings
    """
    # Feature columns in the correct order
    feature_columns = [
        'distance_to_player',
        'boss_health_pct',
        'player_health_pct',
        'is_phase2',
        'can_use_laser',
        'time_since_last_attack'
    ]
    
    # Extract features
    X = df[feature_columns].values
    
    # Encode labels
    label_encoder = LabelEncoder()
    y = label_encoder.fit_transform(df['action'].values)
    
    print(f"\nLabel encoding mapping:")
    for i, label in enumerate(label_encoder.classes_):
        print(f"  {i}: {label}")
    
    return X, y, label_encoder


def train_model(X, y, num_classes, epochs=100, learning_rate=0.001, batch_size=32):
    """
    Train the neural network model.
    
    Args:
        X: Feature array
        y: Label array
        num_classes: Number of action classes
        epochs: Number of training epochs
        learning_rate: Learning rate for optimizer
        batch_size: Batch size for training
    
    Returns:
        Trained model
    """
    # Convert to PyTorch tensors
    X_tensor = torch.FloatTensor(X)
    y_tensor = torch.LongTensor(y)
    
    # Split data into train and validation sets
    X_train, X_val, y_train, y_val = train_test_split(
        X_tensor, y_tensor, test_size=0.2, random_state=42, stratify=y_tensor
    )
    
    print(f"\nTraining set: {len(X_train)} samples")
    print(f"Validation set: {len(X_val)} samples")
    
    # Create model
    model = BossAINetwork(input_size=6, hidden_size=32, output_size=num_classes)
    
    # Loss function and optimizer
    criterion = nn.CrossEntropyLoss()
    optimizer = optim.Adam(model.parameters(), lr=learning_rate)
    
    # Training loop
    print("\nStarting training...")
    for epoch in range(epochs):
        model.train()
        
        # Forward pass
        outputs = model(X_train)
        loss = criterion(outputs, y_train)
        
        # Backward pass and optimization
        optimizer.zero_grad()
        loss.backward()
        optimizer.step()
        
        # Validation
        if (epoch + 1) % 10 == 0:
            model.eval()
            with torch.no_grad():
                val_outputs = model(X_val)
                val_loss = criterion(val_outputs, y_val)
                
                # Calculate accuracy
                _, predicted = torch.max(val_outputs, 1)
                accuracy = (predicted == y_val).sum().item() / len(y_val)
                
                print(f"Epoch [{epoch+1}/{epochs}] "
                      f"Train Loss: {loss.item():.4f} "
                      f"Val Loss: {val_loss.item():.4f} "
                      f"Val Accuracy: {accuracy:.4f}")
    
    # Final evaluation
    model.eval()
    with torch.no_grad():
        train_outputs = model(X_train)
        _, predicted = torch.max(train_outputs, 1)
        train_accuracy = (predicted == y_train).sum().item() / len(y_train)
        
        val_outputs = model(X_val)
        _, predicted = torch.max(val_outputs, 1)
        val_accuracy = (predicted == y_val).sum().item() / len(y_val)
        
        print(f"\nFinal Training Accuracy: {train_accuracy:.4f}")
        print(f"Final Validation Accuracy: {val_accuracy:.4f}")
    
    return model


def export_to_onnx(model, output_path, input_size=6):
    """
    Export the trained model to ONNX format for Unity Barracuda.
    
    Args:
        model: Trained PyTorch model
        output_path: Path to save ONNX file
        input_size: Number of input features
    """
    print(f"\nExporting model to ONNX format: {output_path}")
    
    # Set model to evaluation mode
    model.eval()
    
    # Create dummy input for tracing
    dummy_input = torch.randn(1, input_size)
    
    # Export to ONNX with dynamic batch size
    torch.onnx.export(
        model,
        dummy_input,
        output_path,
        export_params=True,
        opset_version=11,  # Using opset 11 for broad Barracuda compatibility (supports up to 15)
        do_constant_folding=True,
        input_names=['input'],
        output_names=['output'],
        dynamic_axes={
            'input': {0: 'batch_size'},
            'output': {0: 'batch_size'}
        }
    )
    
    print(f"Model exported successfully!")
    print(f"Model file size: {os.path.getsize(output_path) / 1024:.2f} KB")


def main():
    """Main training pipeline."""
    # Configuration
    CSV_PATH = "boss_training_data.csv"
    ONNX_OUTPUT_PATH = "boss_ai.onnx"
    EPOCHS = 100
    LEARNING_RATE = 0.001
    
    print("="*50)
    print("Boss AI Training Pipeline")
    print("="*50)
    
    # Load data
    df = load_data(CSV_PATH)
    
    # Prepare data
    X, y, label_encoder = prepare_data(df)
    num_classes = len(label_encoder.classes_)
    
    print(f"\nFeatures shape: {X.shape}")
    print(f"Labels shape: {y.shape}")
    print(f"Number of classes: {num_classes}")
    
    # Train model
    model = train_model(X, y, num_classes, epochs=EPOCHS, learning_rate=LEARNING_RATE)
    
    # Export to ONNX
    export_to_onnx(model, ONNX_OUTPUT_PATH)
    
    # Print action class mapping for Unity
    print("\n" + "="*50)
    print("ACTION CLASS MAPPING FOR UNITY")
    print("="*50)
    print("Copy this to your BossAIController.cs:")
    print("\nprivate string[] actionLabels = new string[] {")
    for label in label_encoder.classes_:
        print(f'    "{label}",')
    print("};")
    print("="*50)
    
    print("\nTraining complete!")
    print(f"Model saved to: {ONNX_OUTPUT_PATH}")
    print("\nNext steps:")
    print("1. Import boss_ai.onnx into Unity (drag into Assets folder)")
    print("2. Copy the action labels array to BossAIController.cs")
    print("3. Assign the NNModel to the Boss AI Controller component")
    print("4. Enable AI mode on the Boss")


if __name__ == "__main__":
    main()
