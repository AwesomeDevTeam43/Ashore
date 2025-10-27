using UnityEngine;

public abstract class Enemy_Stats : ScriptableObject
{
    [Header("Core")]
    public int maxHealth = 5;
    public int xpOnDeath = 1;
    public int dropA = 0;
    public int dropB = 0;
    public int dropC = 0;

    [Header("Visual")]
    public Vector3 baseScale = Vector3.one;
}