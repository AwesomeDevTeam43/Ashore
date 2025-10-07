using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Ashore/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Base Stats")]
    [SerializeField] private int baseHealth = 10;
    [SerializeField] private int baseAttackPower = 1;
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float baseJumpForce = 15f;

    [Header("Upgrades Per Level")]
    [SerializeField] private int healthUpgradePerLevel = 1;
    [SerializeField] private int attackPowerUpgradePerLevel = 1;
    [SerializeField] private float moveSpeedUpgradePerLevel = 0.5f;
    [SerializeField] private float jumpForceUpgradePerLevel = 0.5f;

    [Header("XP Settings")]
    [SerializeField] private int level1XpAmount = 5;
    [SerializeField] private int levelGap = 5;

    public int GetHealth(int level) => baseHealth + (healthUpgradePerLevel * (level - 1));
    public int GetAttackPower(int level) => baseAttackPower + (attackPowerUpgradePerLevel * (level - 1));
    public float GetMoveSpeed(int level) => baseMoveSpeed + (moveSpeedUpgradePerLevel * (level - 1));
    public float GetJumpForce(int level) => baseJumpForce + (jumpForceUpgradePerLevel * (level - 1));

    public int Level1XpAmount => level1XpAmount;
    public int LevelGap => levelGap;
}