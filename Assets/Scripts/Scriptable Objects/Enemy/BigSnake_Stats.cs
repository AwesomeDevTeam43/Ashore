using UnityEngine;

[CreateAssetMenu(fileName = "Serpent_Stats", menuName = "Enemies/Serpent_Stats")]
public class Serpent_Stats : ScriptableObject
{
    [Header("Core")]
    public int maxHealth = 30;
    public int xpOnDeath = 3;
    public int dropA = 0;
    public int dropB = 0;
    public int dropC = 0;

    [Header("Ranges")]
    public float distanceToPlayer = 8f;
    public float meleeRange = 1.2f;

    [Header("Combat")]
    public int biteDamage = 2;
    public float startTimeBtwAttack = 1f;

    [Header("Visual")]
    public Vector3 baseScale = new Vector3(2f, 2f, 1f);
}