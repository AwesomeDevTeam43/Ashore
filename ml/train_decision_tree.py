import json
import numpy as np
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.tree import DecisionTreeClassifier
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

# Example training script.
# Replace data loading with your telemetry CSV containing feature columns and label column.
# Features (example): dist_to_player, vertical_delta, has_los, player_health_ratio, enemy_health_ratio, cooldown
# Label: action_id (0: Roaming, 1: AttackWindup, 2: Lunging, 3: Retreating)

def load_data(csv_path: str):
    df = pd.read_csv(csv_path)
    feature_cols = [c for c in df.columns if c != 'action_id']
    X = df[feature_cols].astype(np.float32).values
    y = df['action_id'].astype(np.int64).values
    return X, y, feature_cols


def train_and_export(csv_path: str, onnx_out: str, meta_out: str, max_depth: int = 6, min_samples_leaf: int = 10):
    X, y, feature_cols = load_data(csv_path)
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

    clf = DecisionTreeClassifier(max_depth=max_depth, min_samples_leaf=min_samples_leaf)
    clf.fit(X_train, y_train)

    # Export to ONNX
    initial_type = [('input', FloatTensorType([None, len(feature_cols)]))]
    onnx_model = convert_sklearn(clf, initial_types=initial_type, target_opset=13)
    with open(onnx_out, 'wb') as f:
        f.write(onnx_model.SerializeToString())

    # Save metadata (feature order mapping)
    meta = {
        'feature_names': feature_cols,
        'action_mapping': {
            '0': 'Roaming',
            '1': 'AttackWindup',
            '2': 'Lunging',
            '3': 'Retreating'
        }
    }
    with open(meta_out, 'w', encoding='utf-8') as f:
        json.dump(meta, f, indent=2)

    # Simple accuracy print
    acc = clf.score(X_test, y_test)
    print(f"Validation accuracy: {acc:.3f}")


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('--csv', required=True, help='Path to telemetry CSV')
    parser.add_argument('--onnx_out', default='decision_policy.onnx')
    parser.add_argument('--meta_out', default='decision_policy.meta.json')
    parser.add_argument('--max_depth', type=int, default=6)
    parser.add_argument('--min_samples_leaf', type=int, default=10)
    args = parser.parse_args()

    train_and_export(args.csv, args.onnx_out, args.meta_out, args.max_depth, args.min_samples_leaf)
