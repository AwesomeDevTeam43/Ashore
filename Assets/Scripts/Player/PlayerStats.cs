using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Ashore/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Base Stats")]
    [SerializeField] private int baseHealth = 10;
    [SerializeField] private int baseAttackPower = 1;
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float baseJumpForce = 15f;
    [SerializeField] private float baseCriticalChance = 10f; // Base 10%

    [Header("Upgrades Per Level")]
    [SerializeField] private int healthUpgradePerLevel = 1;
    [SerializeField] private int attackPowerUpgradePerLevel = 1;
    [SerializeField] private float moveSpeedUpgradePerLevel = 0.5f;
    [SerializeField] private float jumpForceUpgradePerLevel = 0.5f;
    [SerializeField] private float criticalChanceUpgradePerLevel = 2f; // +2% per level

    [Header("XP Settings")]
    [SerializeField] private int level1XpAmount = 5;
    [SerializeField, Tooltip("Multiplier applied to XP requirements each level (>= 1.0)")]
    private float xpGrowthMultiplier = 1.35f;

    public int GetHealth(int level) => baseHealth + (healthUpgradePerLevel * (level - 1));
    public int GetAttackPower(int level) => baseAttackPower + (attackPowerUpgradePerLevel * (level - 1));
    public float GetMoveSpeed(int level) => baseMoveSpeed + (moveSpeedUpgradePerLevel * (level - 1));
    public float GetJumpForce(int level) => baseJumpForce + (jumpForceUpgradePerLevel * (level - 1));
    public float GetCriticalChance(int level) => Mathf.Min(baseCriticalChance + (criticalChanceUpgradePerLevel * (level - 1)), 100f); // Cap at 100%

    public int Level1XpAmount => level1XpAmount;
    public float XpGrowthMultiplier => xpGrowthMultiplier;
}