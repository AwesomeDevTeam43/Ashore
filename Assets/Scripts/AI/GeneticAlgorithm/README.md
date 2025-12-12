# 🧬 Genetic Algorithm System for Enemy Evolution

A sophisticated **species-based genetic algorithm** system for dynamically evolving enemy difficulty in Unity games. Each enemy species maintains its own evolutionary population, ensuring that killing flies makes flies harder, not bees.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Key Features](#key-features)
3. [Architecture](#architecture)
4. [Quick Start](#quick-start)
5. [Configuration Guide](#configuration-guide)
6. [Species System](#species-system)
7. [Fitness & Evolution](#fitness--evolution)
8. [Adaptive Difficulty](#adaptive-difficulty)
9. [Damage Attribution](#damage-attribution)
10. [Persistence & Saving](#persistence--saving)
11. [Debug Tools](#debug-tools)
12. [Best Practices](#best-practices)
13. [Troubleshooting](#troubleshooting)
14. [API Reference](#api-reference)

---

## Overview

This genetic algorithm system creates **emergent difficulty scaling** based on player performance. Instead of static difficulty settings, enemies evolve over time based on how effective they are against the player.

### Core Concept

```
Player kills enemies → Fitness recorded → Population evolves → Stronger enemies spawn
                              ↓
                    Player struggles → Difficulty reduces
                    Player dominates → Difficulty increases
```

### Why Genetic Algorithms?

- **Natural Progression**: Difficulty increases organically through gameplay
- **Species Isolation**: Each enemy type evolves independently
- **Player-Responsive**: Automatically adapts to player skill
- **Emergent Behavior**: Evolution can discover optimal stat combinations

---

## Key Features

### ✅ Species-Based Evolution
Each enemy species (Fly, Crab, Bee, etc.) has its own genetic population. Killing many flies will evolve flies, but won't affect crabs.

### ✅ Hard Difficulty Ceiling
Configurable maximum difficulty prevents the game from becoming unbeatable. Multiple layers of caps ensure bounded difficulty.

### ✅ Adaptive Difficulty
The system monitors player deaths and adjusts the difficulty modifier in real-time. Players who struggle get easier enemies.

### ✅ Accurate Damage Attribution
Only the enemy that actually dealt damage gets fitness credit. No more proxy-based attribution errors.

### ✅ File-Based Persistence
Progress is saved to a JSON file (not PlayerPrefs), supporting larger populations and more reliable saves.

### ✅ Zone Multipliers
Different game zones can have difficulty multipliers that stack with evolution.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    GlobalGeneticEvolver                      │
│  (Singleton - persists across scenes)                       │
│                                                              │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ Fly Population  │  │ Crab Population │  ... (per species)│
│  │ - 20 genomes    │  │ - 20 genomes    │                   │
│  │ - Generation 5  │  │ - Generation 3  │                   │
│  │ - Avg Fit: 42   │  │ - Avg Fit: 28   │                   │
│  └─────────────────┘  └─────────────────┘                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    EnemyFitnessTracker                       │
│  (Attached to each enemy)                                   │
│                                                              │
│  - Detects species from Enemy_Stats                         │
│  - Requests genome from GlobalGeneticEvolver                │
│  - Applies genome to enemy stats                            │
│  - Tracks damage dealt                                      │
│  - Reports fitness on death                                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       EnemyGenome                            │
│                                                              │
│  Genes (0-1 normalized):                                    │
│  - healthGene        → Scales max HP                        │
│  - damageGene        → Scales attack damage                 │
│  - attackSpeedGene   → Scales attack interval               │
│  - movementSpeedGene → Scales movement speed                │
│  - aggressionRangeGene → Scales detection range             │
│  - aggressivenessGene → Behavior tendency                   │
│  - meleeResistanceGene → Melee damage reduction             │
│  - rangedResistanceGene → Ranged damage reduction           │
└─────────────────────────────────────────────────────────────┘
```

---

## Quick Start

### 1. Add GlobalGeneticEvolver to Your Scene

Create an empty GameObject and add the `GlobalGeneticEvolver` component. This object will persist across scenes.

```
Hierarchy:
└── GameManagers
    └── GlobalGeneticEvolver (add component)
    └── GeneticDebugController (add component, optional)
```

### 2. Add EnemyFitnessTracker to Enemy Prefabs

Add the `EnemyFitnessTracker` component to each enemy prefab that should evolve.

```csharp
// The tracker automatically:
// - Detects species from EnemyBase.stats
// - Gets a genome from the evolver
// - Applies genome to enemy stats
// - Reports fitness on death
```

### 3. Add GeneticDamageIntegration to Player

Add the `GeneticDamageIntegration` component to the **Player** GameObject:

```csharp
// This component automatically:
// - Subscribes to player's HealthSystem.OnDamageTaken
// - Extracts the enemy that dealt damage
// - Calls RegisterDamageDealt() on the enemy's tracker
// - Handles player death attribution
```

> **Note**: Enemy scripts must pass `gameObject` as the damage source:
> ```csharp
> playerHealth.TakeDamage(damage, gameObject);
> ```

### 4. (Optional) Add Debug Components

- **GlobalGeneticDebugUI**: Shows evolution stats panel. Press G to toggle.
- **GeneticDebugController**: Provides keybinds for debugging (see Debug Tools section).

---

## Debug Controls 🎮

When `GeneticDebugController` is in the scene:

| Key | Action |
|-----|--------|
| **H** | Toggle genome HUD above all enemies |
| **Ctrl+R** | Respawn all enemies with current genes |
| **G** | Toggle main debug stats panel |
| **Tab** | Toggle detailed/compact HUD view |
| **F5** | Force evolution for all species |

---

## Configuration Guide

### GlobalGeneticEvolver Settings

| Setting | Default | Description |
|---------|---------|-------------|
| **Population Settings** | | |
| `populationSize` | 20 | Genomes per species. Higher = more diversity, slower evolution |
| `evolveTriggerCount` | 5 | Kills of same species before evolution |
| **Selection** | | |
| `eliteCount` | 4 | Top performers that pass unchanged |
| `tournamentSize` | 3 | Candidates per selection |
| **Genetic Operators** | | |
| `mutationRate` | 0.15 | Chance for each gene to mutate |
| `mutationStrength` | 0.2 | Maximum mutation delta |
| `crossoverRate` | 0.7 | Chance to use crossover vs cloning |
| **Difficulty Ceiling** | | |
| `absoluteMaxDifficulty` | 2.5 | Hard cap on total difficulty multiplier |
| `maxGeneValue` | 0.85 | Maximum individual gene value |
| `generationScaling` | 1.01 | Per-generation multiplier (+1% per gen) |
| `maxScalingGeneration` | 50 | Generation cap for scaling |
| **Adaptive Difficulty** | | |
| `adaptiveDifficulty` | true | Enable player-responsive difficulty |
| `deathsBeforeReduction` | 3 | Player deaths before reducing difficulty |
| `deathPenaltyStrength` | 0.1 | How much to reduce per death |
| `killsBeforeIncrease` | 20 | Kills without dying to increase difficulty |
| `dominationBonusStrength` | 0.05 | How much to increase when dominating |
| **Target Difficulty** | | |
| `useTargetDifficulty` | false | Evolve toward target instead of max |
| `targetFitness` | 50 | Target fitness to aim for |
| **Persistence** | | |
| `persistProgress` | true | Save to file |
| `saveFileName` | `genetic_evolution_save.json` | Save file name |

### Recommended Presets

#### Easy Mode (Casual Games)
```
absoluteMaxDifficulty = 1.8
generationScaling = 1.005
deathsBeforeReduction = 2
deathPenaltyStrength = 0.15
```

#### Normal Mode (Balanced)
```
absoluteMaxDifficulty = 2.5
generationScaling = 1.01
deathsBeforeReduction = 3
deathPenaltyStrength = 0.1
```

#### Hard Mode (Souls-like)
```
absoluteMaxDifficulty = 3.5
generationScaling = 1.02
deathsBeforeReduction = 5
deathPenaltyStrength = 0.05
```

---

## Species System

### How Species Are Detected

The system automatically detects species from the enemy's `Enemy_Stats` type:

```csharp
// In EnemySpeciesHelper.GetSpeciesFromStats():
"Fly_Stats" → EnemySpecies.Fly
"GiantBee_Stats" → EnemySpecies.GiantBee
"BigCrab_Stats" → EnemySpecies.BigCrab
// etc.
```

### Adding New Species

1. **Add to EnemySpecies enum** (`EnemySpecies.cs`):

```csharp
public enum EnemySpecies
{
    // ... existing species ...
    
    // Add your new species
    NewEnemy = 50,
}
```

2. **Add to GetSpeciesFromStats()** (`EnemySpecies.cs`):

```csharp
return typeName switch
{
    // ... existing mappings ...
    "NewEnemy_Stats" => EnemySpecies.NewEnemy,
    _ => EnemySpecies.Unknown
};
```

3. **Add display name** in `GetDisplayName()`:

```csharp
EnemySpecies.NewEnemy => "New Enemy",
```

### Species That Don't Evolve

Some species (like bosses) shouldn't evolve:

```csharp
public static bool ShouldEvolve(EnemySpecies species)
{
    return species switch
    {
        EnemySpecies.RuinsBoss => false,
        EnemySpecies.FinalBoss => false,
        // Add more non-evolving species here
        _ => true
    };
}
```

---

## Fitness & Evolution

### Fitness Calculation

Fitness determines which genomes reproduce:

```csharp
Fitness = (damageDealt × 10) + (survivalTime × 0.3)

// Bonus for actually dealing damage:
if (damageDealt > 0) Fitness *= 1.5
```

### Evolution Process

When a species reaches `evolveTriggerCount` kills:

1. **Elitism**: Top `eliteCount` genomes pass unchanged
2. **Selection**: Tournament selection picks parents
3. **Crossover**: 70% chance to combine two parents
4. **Mutation**: Random changes to genes
5. **Gene Capping**: Enforce `maxGeneValue` ceiling

### Gene to Stat Conversion

Each gene is a 0-1 value that scales base stats:

| Gene | Conversion | Example |
|------|------------|---------|
| `healthGene` | `baseHP × Lerp(0.5, 2.0, gene)` | 100 HP × 0.7 gene = 120 HP |
| `damageGene` | `baseDMG × Lerp(0.5, 2.0, gene)` | 10 DMG × 0.8 gene = 16 DMG |
| `attackSpeedGene` | `baseInterval × Lerp(1.5, 0.5, gene)` | 1s × 0.6 gene = 0.8s |
| `movementSpeedGene` | `baseSpeed × Lerp(0.7, 1.5, gene)` | 5 × 0.5 gene = 5.5 speed |

---

## Adaptive Difficulty

The system monitors player performance and adjusts a global difficulty modifier.

### Difficulty Reduction (Player Struggling)

```
Player dies → _playerDeaths++
If _playerDeaths >= deathsBeforeReduction:
    _currentDifficultyModifier -= deathPenaltyStrength × (deaths - threshold)
    Minimum: 0.6 (40% easier)
```

### Difficulty Increase (Player Dominating)

```
Player kills enemy → _killsSinceLastDeath++
If _killsSinceLastDeath >= killsBeforeIncrease:
    _currentDifficultyModifier += dominationBonusStrength
    Maximum: absoluteMaxDifficulty
```

### Fitness Adjustment

Aggressive genomes are penalized when the player is struggling:

```csharp
if (_playerDeaths > deathsBeforeReduction)
{
    float penalty = genome.aggressivenessGene × deathPenaltyStrength × excessDeaths;
    fitness *= (1 - penalty);
}
```

---

## Damage Attribution

### The Old Problem

Previously, damage was attributed by proximity:

```csharp
// BAD: All nearby enemies get credit
if (distanceToPlayer < 5f) 
    creditedDamage = damage × (1 - distance/5f);
```

This meant a weak enemy standing near an attacking strong enemy got rewarded.

### The New Solution

Damage must be explicitly registered by the attacker:

```csharp
// GOOD: Only the actual attacker gets credit
public void RegisterDamageDealt(float damage)
{
    tracker.RegisterDamageDealt(damage);
}
```

### Automatic Registration (Recommended)

With `GeneticDamageIntegration` on the player, damage is tracked automatically:

```csharp
// In your enemy attack script - just pass gameObject as damage source:
void OnPlayerHit(float damage)
{
    playerHealth.TakeDamage(damage, gameObject);  // That's it!
}
```

The `GeneticDamageIntegration` component will automatically call `RegisterDamageDealt()` on the enemy's tracker.

---

## Persistence & Saving

### Save Location

Saves are stored in `Application.persistentDataPath`:
- **Windows**: `C:\Users\<user>\AppData\LocalLow\<company>\<product>\genetic_evolution_save.json`
- **Mac**: `~/Library/Application Support/<company>/<product>/genetic_evolution_save.json`
- **Linux**: `~/.config/unity3d/<company>/<product>/genetic_evolution_save.json`

### Save Structure

```json
{
    "totalKills": 150,
    "playerDeaths": 5,
    "difficultyModifier": 1.15,
    "species": [
        {
            "speciesId": 10,
            "generation": 12,
            "killsSinceEvolution": 3,
            "averageFitness": 45.5,
            "genomes": [
                "{\"healthGene\":0.65,\"damageGene\":0.58,...}",
                ...
            ]
        },
        ...
    ]
}
```

### When Saves Occur

- On scene change
- On rest point use
- On application quit
- Manual: `GlobalGeneticEvolver.Instance.SaveProgress()` (via reflection in editor)

### Resetting Progress

```csharp
GlobalGeneticEvolver.Instance.ResetAll();
```

---

## Debug Tools

### In-Game Debug UI

Add `GlobalGeneticDebugUI` component. Press **G** to toggle.

Shows:
- Current difficulty modifier (color-coded bar)
- Total kills and player deaths
- Per-species: generation, average fitness, population size

Press **Tab** to toggle detailed view.

### GeneticDebugController (In-Game)

Add the `GeneticDebugController` component for powerful in-game controls:

| Keybind | Action | Description |
|---------|--------|-------------|
| **H** | Toggle Enemy HUD | Show/hide genome stats above each enemy |
| **Ctrl+R** | Respawn Enemies | Destroy all enemies and respawn them |
| **G** | Toggle Debug Panel | Show/hide the main stats panel |
| **Tab** | Toggle HUD Detail | Switch between detailed/compact HUD |
| **F5** | Force Evolution | Trigger evolution for all species |

The controller also shows a hint bar at the top of the screen with available controls.

### EnemyGenomeUI (Per-Enemy HUD)

Each enemy with `EnemyFitnessTracker` automatically gets a floating HUD showing:

**Detailed View:**
- Species name (e.g., "Giant Bee")
- Generation number and Power Level percentage
- Gene bars: HP, DMG, SPD, ATK, AGR, RES
- Damage dealt to player this life
- Survival time

**Compact View:**
- Species name
- Generation and Power Level

### Editor Context Menu

Right-click the `GlobalGeneticEvolver` component:
- **Log All Metrics**: Prints comprehensive stats to console
- **Force Evolution (All Species)**: Triggers evolution for all species
- **Reset Difficulty Modifier**: Resets adaptive difficulty to 1.0

### Console Logging

Set `debugMode = true` to see evolution logs:

```
🧬 [GlobalGA] Scene loaded: MainLevel
🧬 [Fly] Genome assigned (Gen 5, ×1.25): Genome[Fly|HP:0.62 DMG:0.58 ...]
🧬 [Fly] Kill #45. Fitness: 32.5. Progress: 3/5
🧬 [Fly] ══════ EVOLUTION TO GEN 6 ══════
🧬 [Fly] Best fitness: 48.2
🧬 [Fly] New avg fitness: 38.7
```

---

## Best Practices

### 1. Start Easy
Initialize populations with low gene values (0.2-0.4) so early game is approachable.

### 2. Cap Everything
Use multiple layers of difficulty caps:
- `maxGeneValue` caps individual genes
- `maxScalingGeneration` caps generation scaling
- `absoluteMaxDifficulty` caps total multiplier

### 3. Test Extensively
Use the `GeneticTestSpawner` to simulate many generations quickly.

### 4. Monitor Metrics
Add logging or analytics to track real-player difficulty curves.

### 5. Provide Escape Valves
- Rest points that reset player death counter
- Items that temporarily reduce difficulty
- Difficulty settings that adjust `absoluteMaxDifficulty`

### 6. Consider New Players
New players might quit before adaptive difficulty helps them. Consider:
- First-time player detection
- Starting with lower difficulty
- Tutorial area without evolution

---

## Troubleshooting

### "Enemies are too hard too quickly"

1. Reduce `generationScaling` (try 1.005)
2. Reduce `absoluteMaxDifficulty` (try 2.0)
3. Increase `deathsBeforeReduction` sensitivity

### "Enemies aren't evolving"

1. Check that `EnemyFitnessTracker` is on enemy prefabs
2. Verify `GlobalGeneticEvolver` exists in scene
3. Check that species is detected (enable `showDebugInfo`)
4. Ensure enemies are being killed by player (not scene changes)

### "Damage attribution seems wrong"

1. Verify you're calling `tracker.RegisterDamageDealt(damage)` in attack scripts
2. Remove any old proximity-based damage detection
3. Enable `logEveryKill` to see damage tracking logs

### "Save not loading"

1. Check `Application.persistentDataPath` for the save file
2. Look for JSON parsing errors in console
3. Delete corrupted save file to start fresh

### "Species not detected"

1. Verify `Enemy_Stats` subclass name matches pattern (e.g., `Fly_Stats`)
2. Add mapping in `EnemySpeciesHelper.GetSpeciesFromStats()`
3. Check `EnemyBase.stats` is assigned

### "Memory usage is high"

1. Reduce `populationSize` (try 15)
2. Reduce number of active species
3. Call `SaveProgress()` less frequently

---

## API Reference

### GlobalGeneticEvolver

```csharp
// Singleton access
GlobalGeneticEvolver.Instance

// Get genome for new enemy
EnemyGenome GetGenome(int enemyId, EnemySpecies species, float zoneMult = 1f)

// Register damage dealt by enemy
void RegisterDamageDealt(int attackerInstanceId, float damage)

// Register enemy death
void RegisterKill(int enemyId, float survivalTime, bool killedByPlayer = true)

// Register player death
void RegisterPlayerDeath()

// Called on rest point use
void OnRestPoint()

// Reset player death counter
void ResetPlayerDeathCounter()

// Full reset
void ResetAll()

// Get best genome for species
EnemyGenome GetBestGenome(EnemySpecies species)

// Get all species stats
Dictionary<EnemySpecies, (int gen, float fitness, int popSize)> GetAllSpeciesStats()

// Properties
int TotalKills { get; }
int PlayerDeaths { get; }
float CurrentDifficultyModifier { get; }
int GetGeneration(EnemySpecies species)
float GetAverageFitness(EnemySpecies species)

// Events
Action<EnemySpecies, int> OnSpeciesEvolved
Action OnRestPointUsed
Action<float> OnDifficultyChanged
```

### EnemyFitnessTracker

```csharp
// Get assigned genome
EnemyGenome Genome { get; }

// Get species
EnemySpecies Species { get; }

// MUST CALL when enemy deals damage
void RegisterDamageDealt(float damage)

// Get scaled stats from genome
float GetScaledDamage(float baseDamage)
float GetScaledMovementSpeed(float baseSpeed)
float GetScaledAttackInterval(float baseInterval)
float GetScaledAggressionRange(float baseRange)

// Properties
float DamageDealtToPlayer { get; }
float SurvivalTime { get; }
```

### EnemyGenome

```csharp
// Genes (0-1)
float healthGene
float damageGene
float attackSpeedGene
float movementSpeedGene
float aggressionRangeGene
float aggressivenessGene
float meleeResistanceGene
float rangedResistanceGene

// Fitness
float Fitness { get; set; }
int TimesUsed { get; set; }
float AverageFitness { get; }

// Operations
EnemyGenome Clone()
void Mutate(float rate, float strength)
void ClampGenes(float maxValue)
static EnemyGenome Crossover(EnemyGenome p1, EnemyGenome p2)

// Stat conversion
int GetScaledHealth(int baseHealth, float minMult = 0.5f, float maxMult = 2f)
float GetScaledDamage(float baseDamage, float minMult = 0.5f, float maxMult = 2f)
float GetScaledAttackInterval(float baseInterval, float minMult = 0.5f, float maxMult = 1.5f)
float GetScaledMovementSpeed(float baseSpeed, float minMult = 0.7f, float maxMult = 1.5f)
float GetScaledAggressionRange(float baseRange, float minMult = 0.5f, float maxMult = 2f)
float GetScaledMeleeResistance(float maxResistance = 0.5f)
float GetScaledRangedResistance(float maxResistance = 0.5f)

// Utility
float GetPowerLevel()
float SimilarityTo(EnemyGenome other)
string ToJson()
static EnemyGenome FromJson(string json)
```

### EnemySpeciesHelper

```csharp
// Get species from stats
static EnemySpecies GetSpeciesFromStats(Enemy_Stats stats)

// Check if species should evolve
static bool ShouldEvolve(EnemySpecies species)

// Get display name
static string GetDisplayName(EnemySpecies species)
```

---

## License

This genetic algorithm system is part of the Ashore project.

---

## Version History

- **v2.0** - Species-based evolution, difficulty ceiling, file persistence
- **v1.0** - Initial global genetic algorithm implementation
