using UnityEngine;

/// <summary>
/// Define multiplicador de dificuldade para uma zona.
/// NÃO é uma população separada - apenas um boost aos genes globais.
/// </summary>
[CreateAssetMenu(fileName = "Zone Difficulty", menuName = "Genetic Algorithm/Zone Difficulty")]
public class ZoneDifficulty : ScriptableObject
{
    [Header("Zone Info")]
    public string zoneName = "Unknown";
    
    [Header("Difficulty Multiplier")]
    [Tooltip("Multiplicador aplicado aos genes. 1.0 = normal, 1.5 = 50% mais forte")]
    [Range(0.5f, 2.5f)]
    public float difficultyMultiplier = 1.0f;
    
    [Header("Visual")]
    public Color gizmoColor = Color.green;
}
