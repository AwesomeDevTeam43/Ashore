using UnityEngine;

[CreateAssetMenu(menuName = "Ashore/Enemies/Salamander Stats")]
public class Salamander_Stats : Enemy_Stats
{
  [Header("Salamander Movement")]
  public float moveSpeed = 2.5f;
  public float retreatSpeed = 3.5f;

  [Header("Ranges")]
  public float shootRange = 8f;
  public float biteRange = 1.5f;

  [Header("Damage")]
  public int biteDamage = 10;
}
