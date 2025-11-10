using UnityEngine;

[CreateAssetMenu(fileName = "MiasmaBloom_Stats", menuName = "Enemies/MiasmaBloom_Stats")]
public class MiasmaBloom_Stats : Enemy_Stats
{
    public float playerDetect = 3f;
    public float attackRange = 2.5f;
    public float attackDelay = 0.6f;
    public float attackRadius = 0.6f;
    public int biteDamage = 2;
    public float attackCooldown = 1.5f;

}