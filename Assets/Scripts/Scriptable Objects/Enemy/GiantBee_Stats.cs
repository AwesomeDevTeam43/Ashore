using UnityEngine;

[CreateAssetMenu(fileName = "GiantBee_Stats", menuName = "Enemies/GiantBee_Stats")]
public class GiantBee_Stats : ScriptableObject
{
    public int maxHealth = 5;
    
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

    [Header("Visual")]
    public Vector3 baseScale = new Vector3(2f, 2f, 1f);

    [Header("Rewards")]
    public int xpOnDeath = 3;
    public int dropA = 1;
    public int dropB = 2;
    public int dropC = 3;
}
