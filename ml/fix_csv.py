import re

with open('boss_training_data.csv', 'r') as f:
    content = f.read()

# Fix pattern:  number,number where both are parts of a decimal
# e.g., "47,37" -> "47.37" and "1,000" -> "1.000"
# Match digits,digits that should be decimals (not followed by another field pattern)

lines = content.strip().split('\n')
fixed_lines = [lines[0]]  # Keep header as is

for line in lines[1:]: 
    # Replace comma-decimals with period-decimals
    # Pattern: digit(s),digit(s) where the second part is 1-3 digits (decimal portion)
    fixed = re.sub(r'(\d+),(\d{1,3})(?=,|$)', r'\1.\2', line)
    fixed_lines.append(fixed)

with open('boss_training_data_fixed.csv', 'w') as f:
    f. write('\n'.join(fixed_lines))

print("Fixed CSV saved to boss_training_data_fixed. csv")
