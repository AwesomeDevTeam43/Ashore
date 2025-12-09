using UnityEngine;
using UnityEngine.Serialization;

public abstract class Enemy_Stats : ScriptableObject
{
    [Header("Core")]
    public int maxHealth = 5;
    public float damage = 1f; // Adicionar esta linha
    public int xpOnDeath = 1;
    [FormerlySerializedAs("dropA")]
    [Tooltip("Amount of wood that drops when this enemy dies.")]
    public int woodDrop = 0;
    [FormerlySerializedAs("dropB")]
    [Tooltip("Amount of stone that drops when this enemy dies.")]
    public int stoneDrop = 0;
    [FormerlySerializedAs("dropC")]
    [Tooltip("Amount of rope that drops when this enemy dies.")]
    public int ropeDrop = 0;

    [Header("Visual")]
    public Vector3 baseScale = Vector3.one;

    [Header("Damage Resistances")]
    [Range(0f, 1f), Tooltip("Percent of incoming melee damage to reduce (0 = none, 1 = immune).")]
    public float meleeResistance = 0f;
    [Range(0f, 1f), Tooltip("Percent of incoming ranged damage to reduce (0 = none, 1 = immune).")]
    public float rangedResistance = 0f;

    [Header("Audio")]
    [Tooltip("Sound to play when the enemy is hit.")]
    public AudioClip hitSound;
    [Range(0f, 1f), Tooltip("Volume multiplier for hit sound.")]
    public float hitVolume = 1f;
    [Tooltip("Sound to play when the enemy dies.")]
    public AudioClip deathSound;
    [Range(0f, 1f), Tooltip("Volume multiplier for death sound.")]
    public float deathVolume = 1f;
}