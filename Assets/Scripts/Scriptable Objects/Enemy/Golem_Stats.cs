using UnityEngine;

[CreateAssetMenu(fileName = "Golem_Stats", menuName = "Enemies/Golem_Stats")]
public class Golem_Stats : Enemy_Stats
{
    [Header("Movement")]
    public float moveSpeed = 1.2f;
    public float hopCheckDistance = 0.45f;
    public float maxStepHeight = 0.6f;
    [Tooltip("Ignore tiny rises under this height (world units) so the golem doesn't jump too early on small slopes")]
    public float minStepRise = 0.08f;
    public float hopForceY = 6f;
    public float hopForceX = 2.5f;
    public float groundCheckDistance = 0.18f;
    public float tallJumpForceY = 10f;
    public float tallJumpForceX = 3.5f;
    public float chestHeight = 0.8f; // height (in world units) above feet to test mid obstacle
    public float headClearanceCheck = 0.4f; // required free space above head to allow tall jump

    [Header("Sensing/Combat Ranges")]
    public float detectRange = 6f;
    public float slamRange = 1.6f;

    [Header("Leash/Return")]
    public float leashDistance = 12f;          // if player is farther than this from the golem, return to spawn
    public float returnStopDistance = 0.5f;    // distance from spawn to consider arrived
    public float returnSpeed = 1.2f;           // speed while returning (fallbacks to moveSpeed if 0)

    [Header("Slam Attack")]
    public float slamWindup = 0.5f;
    public float slamCooldown = 2.2f;
    public int slamDamage = 6;
    public float slamRadius = 2.75f;

    [Header("Debug")]
    public bool debugJumpLogs = false;
}
