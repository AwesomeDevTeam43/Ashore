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

    [Header("Jump Navigation")]
    public float hopCheckDistance = 0.45f;
    public float maxStepHeight = 0.6f;
    [Tooltip("Ignore tiny rises under this height so the boss does not jump unnecessarily.")]
    public float minStepRise = 0.08f;
    public float hopForceY = 6f;
    public float hopForceX = 2.5f;
    public float groundCheckDistance = 0.18f;
    public float tallJumpForceY = 10f;
    public float tallJumpForceX = 3.5f;
    public float chestHeight = 0.8f;
    public float headClearanceCheck = 0.4f;
    public float returnStuckJumpDelay = 0.25f;
    public bool debugJumpLogs = false;
}
