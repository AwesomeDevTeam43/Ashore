using UnityEngine;

[CreateAssetMenu(fileName = "GiantBee_Stats", menuName = "Enemies/GiantBee_Stats")]
public class GiantBee_Stats : Enemy_Stats
{
    [Header("Movement")]
    public float roamSpeed = 4f;
    public float lungingForce = 7f;
    public float retreatSpeed = 6f;

    [Header("Detection")]
    public float playerDetect = 8f;
    public float lungeRange = 4f;
    public float retreatRange = 5f;

    [Header("Timers")]
    public float lungeCooldown = 2f;
    public float lungeDuration = 1f;

    [Header("Combat")]
    public int stingDamage = 2;
}