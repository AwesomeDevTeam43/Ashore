using UnityEngine;

[CreateAssetMenu(fileName = "FinalBoss_Stats", menuName = "Enemies/FinalBoss_Stats")]
public class FinalBoss_Stats : Enemy_Stats
{
	[Header("Boss Combat")]
	public int projectileDamage = 1;
	public float baseFireInterval = 2f;
	public float projectileSpeed = 6f;

	[Header("Boss Movement")]
	public float floatAmplitude = 0.5f;
	public float floatSpeed = 1f;
}