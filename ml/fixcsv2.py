import pandas as pd

# The problem:  some rows use comma as decimal separator
# "47,37,1,000,1,000,0,1,999,00,Idle" should be "47.37,1.000,1.000,0,1,999.00,Idle"

with open('boss_training_data.csv', 'r') as f:
    lines = f.readlines()

header = lines[0]. strip()
fixed_lines = [header]

for line in lines[1:]:
    line = line.strip()
    if not line:
        continue
    
    parts = line.split(',')
    
    # Correct format has 7 columns:  6 numbers + 1 action
    # Broken format has 13 columns due to comma decimals
    
    if len(parts) == 7:
        # Already correct format
        fixed_lines. append(line)
    elif len(parts) == 13:
        # Broken format:  need to merge pairs
        # 47,37 -> 47.37
        # 1,000 -> 1.000
        # etc.
        try:
            distance = f"{parts[0]}. {parts[1]}"          # 47,37 -> 47.37
            boss_hp = f"{parts[2]}. {parts[3]}"           # 1,000 -> 1.000
            player_hp = f"{parts[4]}. {parts[5]}"         # 1,000 -> 1.000
            is_phase2 = parts[6]                          # 0
            can_laser = parts[7]                          # 1
            time_since = f"{parts[8]}.{parts[9]}"        # 999,00 -> 999.00
            action = parts[10] if len(parts) > 10 else ""  # Idle
            
            fixed_line = f"{distance},{boss_hp},{player_hp},{is_phase2},{can_laser},{time_since},{action}"
            fixed_lines.append(fixed_line)
        except Exception as e:
            print(f"Skipping malformed line: {line[: 50]}...  Error: {e}")
    else:
        print(f"Skipping line with {len(parts)} columns: {line[:50]}...")

# Write fixed file
with open('boss_training_data_fixed.csv', 'w') as f:
    f.write('\n'.join(fixed_lines))

print(f"\nFixed {len(fixed_lines)-1} data rows")
print("Saved to:  boss_training_data_fixed.csv")

# Verify it works
print("\nVerifying fixed file...")
df = pd.read_csv('boss_training_data_fixed. csv')
print(f"Loaded {len(df)} rows successfully!")
print(f"Columns: {list(df.columns)}")
print(f"\nAction distribution:")
print(df['action'].value_counts())
print(f"\nAny NaN values:  {df.isna().any().any()}")
