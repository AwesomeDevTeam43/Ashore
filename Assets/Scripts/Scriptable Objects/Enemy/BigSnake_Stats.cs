using UnityEngine;

[CreateAssetMenu(fileName = "Serpent_Stats", menuName = "Enemies/Serpent_Stats")]
public class Serpent_Stats : Enemy_Stats
{
    [Header("Ranges")]
    public float distanceToPlayer = 8f;
    public float meleeRange = 1.2f;

    [Header("Combat")]
    public int biteDamage = 2;
    public int venomDamage = 5;
    public float startTimeBtwAttack = 1f;
}