using UnityEngine;

/// <summary>
/// Defines all enemy species in the game.
/// Each species evolves independently with its own genetic population.
/// 
/// IMPORTANT: When adding new enemies, add a new entry here!
/// The genetic system uses this to maintain separate evolutionary lines.
/// </summary>
public enum EnemySpecies
{
    Unknown = 0,      // Fallback for enemies without explicit species
    
    // Flying Enemies
    Fly = 10,
    GiantBee = 11,
    BirdBomber = 12,
    
    // Ground Enemies
    BigCrab = 20,
    Golem = 21,
    MiasmaGolem = 22,
    
    // Serpent Family
    Serpent = 30,
    BigSnake = 31,
    Salamander = 32,
    
    // Plant Enemies
    MiasmaBloom = 40,
    
    // Bosses (typically don't evolve, but included for tracking)
    RuinsBoss = 100,
    FinalBoss = 101
}

/// <summary>
/// Helper class for mapping Enemy_Stats types to EnemySpecies.
/// </summary>
public static class EnemySpeciesHelper
{
    /// <summary>
    /// Determines the species from an Enemy_Stats ScriptableObject.
    /// Uses type checking to identify the species.
    /// </summary>
    public static EnemySpecies GetSpeciesFromStats(Enemy_Stats stats)
    {
        if (stats == null) return EnemySpecies.Unknown;
        
        // Match by type name
        string typeName = stats.GetType().Name;
        
        return typeName switch
        {
            "Fly_Stats" => EnemySpecies.Fly,
            "GiantBee_Stats" => EnemySpecies.GiantBee,
            "BirdBomber_Stats" => EnemySpecies.BirdBomber,
            "BigCrab_Stats" => EnemySpecies.BigCrab,
            "Golem_Stats" => EnemySpecies.Golem,
            "MiasmaGolem_Stats" => EnemySpecies.MiasmaGolem,
            "Serpent_Stats" => EnemySpecies.Serpent,
            "BigSnake_Stats" => EnemySpecies.BigSnake,
            "Salamander_Stats" => EnemySpecies.Salamander,
            "MiasmaBloom_Stats" => EnemySpecies.MiasmaBloom,
            "RuinBoss_Stats" => EnemySpecies.RuinsBoss,
            "FinalBoss_Stats" => EnemySpecies.FinalBoss,
            _ => EnemySpecies.Unknown
        };
    }
    
    /// <summary>
    /// Returns whether this species should participate in evolution.
    /// Bosses typically don't evolve to maintain consistent difficulty.
    /// </summary>
    public static bool ShouldEvolve(EnemySpecies species)
    {
        return species switch
        {
            EnemySpecies.RuinsBoss => false,
            EnemySpecies.FinalBoss => false,
            EnemySpecies.Unknown => false,
            _ => true
        };
    }
    
    /// <summary>
    /// Returns a human-readable name for the species.
    /// </summary>
    public static string GetDisplayName(EnemySpecies species)
    {
        return species switch
        {
            EnemySpecies.Fly => "Fly",
            EnemySpecies.GiantBee => "Giant Bee",
            EnemySpecies.BirdBomber => "Bird Bomber",
            EnemySpecies.BigCrab => "Big Crab",
            EnemySpecies.Golem => "Golem",
            EnemySpecies.MiasmaGolem => "Miasma Golem",
            EnemySpecies.Serpent => "Serpent",
            EnemySpecies.BigSnake => "Big Snake",
            EnemySpecies.Salamander => "Salamander",
            EnemySpecies.MiasmaBloom => "Miasma Bloom",
            EnemySpecies.RuinsBoss => "Ruins Boss",
            EnemySpecies.FinalBoss => "Final Boss",
            _ => "Unknown"
        };
    }
}
