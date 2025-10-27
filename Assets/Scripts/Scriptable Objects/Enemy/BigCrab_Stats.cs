using UnityEngine;

[CreateAssetMenu(fileName = "BigCrab_Stats", menuName = "Enemies/BigCrab_Stats")]
public class BigCrab_Stats : Enemy_Stats
{
    public float speed = 2f;
    public float followPlayerRange = 5f;
    public float attackRange = 1f;
    public float startTimeBtwAttack = 1f;
    public int biteDamage = 1;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
}