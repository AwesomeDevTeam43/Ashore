using UnityEngine;

[CreateAssetMenu(fileName = "BigCrab_Stats", menuName = "Enemies/BigCrab_Stats")]
public class BigCrab_Stats : ScriptableObject
{
    public float speed = 2f;
    public float followPlayerRange = 5f;
    public float attackRange = 1f;
    public float startTimeBtwAttack = 1f;
    public int biteDamage = 1;
    public int xpOnDeath = 3;
    public int dropA = 1;
    public int dropB = 2;
    public int dropC = 3;
    public Vector3 baseScale = new Vector3(3.34f, 3.34f, 1f);
}