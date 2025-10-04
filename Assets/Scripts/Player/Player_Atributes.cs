using UnityEngine;

public class Player_Atributes : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    private XP_System xpSystem;

    private int currentHealth;
    private int currentAttackPower;
    private float currentMoveSpeed;
    private float currentJumpForce;

    private void OnEnable()
    {
        if (playerStats != null)
        {
            UpdateStats(1);
        }
    }

    void Awake()
    {
        xpSystem = GetComponent<XP_System>();

        if (xpSystem != null)
        {
            xpSystem.OnLevelUp += UpdateStats;
        }
    }

    private void UpdateStats(int level)
    {
        currentHealth = playerStats.GetHealth(level);
        currentAttackPower = playerStats.GetAttackPower(level);
        currentMoveSpeed = playerStats.GetMoveSpeed(level);
        currentJumpForce = playerStats.GetJumpForce(level);

        Debug.Log($"Stats updated! Level {level}: HP={currentHealth}, ATK={currentAttackPower}, Speed={currentMoveSpeed}, Jump={currentJumpForce}");
    }

    public int Health => currentHealth;
    public int AttackPower => currentAttackPower;
    public float MoveSpeed => currentMoveSpeed;
    public float JumpForce => currentJumpForce;
    public int LVL1XpAmount => playerStats != null ? playerStats.Level1XpAmount : 0;
    public int LvlGap => playerStats != null ? playerStats.LevelGap : 0;
}