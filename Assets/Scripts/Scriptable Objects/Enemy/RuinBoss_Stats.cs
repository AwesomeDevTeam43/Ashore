using UnityEngine;

[CreateAssetMenu(menuName = "Ashore/Enemies/Ruin Boss Stats")]
public class RuinBoss_Stats : Enemy_Stats
{
    [Header("Movement")]
    public float moveSpeed = 2.0f;

    [Header("Ranges")]
    public float followPlayerRange = 8f;
    public float attackRange = 1.5f; // also used as melee radius fallback

    [Header("Attack")]
    public float startTimeBtwAttack = 2f;
    public int attackDamage = 20;
    public float knockbackForce = 6f;
    public float knockbackDuration = 0.35f;
}
