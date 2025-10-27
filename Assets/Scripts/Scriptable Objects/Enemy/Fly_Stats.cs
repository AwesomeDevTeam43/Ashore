using UnityEngine;

[CreateAssetMenu(fileName = "Fly_Stats", menuName = "Enemies/Fly_Stats")]
public class Fly_Stats : Enemy_Stats
{
    [Header("Movement")]
    public float speed = 3f;
    public float directionChangeInterval = 2f;
    public float maxRadius = 5f;
    public float obstacleAvoidanceDistance = 1f;

    [Header("Combat")]
    public int collisionDamage = 1;
    public float damageInterval = 0.5f;
}