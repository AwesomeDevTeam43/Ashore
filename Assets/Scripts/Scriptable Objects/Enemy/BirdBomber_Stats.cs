using UnityEngine;

[CreateAssetMenu(fileName = "BirdBomber_Stats", menuName = "Enemies/BirdBomber_Stats")]
public class BirdBomber_Stats : Enemy_Stats
{
    [Header("Movement")]
    public float patrolSpeed = 2.5f;
    public float leftBoundaryOffset = -5f;
    public float rightBoundaryOffset = 5f;
    public bool startMovingRight = true;
    public bool flipSpriteOnDirection = true;
    public bool boundsRelativeToSpawn = true;
    public bool clampToBoundariesOnStart = true;

    [Header("Dropping Attack")]
    public float dropInterval = 3.0f;
    public float randomIntervalJitter = 0.4f;
    public bool requireLineBelow = false;
    public float linecastDistance = 30f;
}
