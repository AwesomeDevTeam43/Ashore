using UnityEngine;

public class Player_Atributes : MonoBehaviour
{
    private XP_System xpSystem;

    //player status
    private int health;
    private int health_UpgradePerLVL = 1;

    private int attackPower;
    private int attackPower_UpgradePerLVL = 1;

    private float moveSpeed;
    private float moveSpeed_UpgradePerLVL = 0.5f;

    private float jumpForce;
    private readonly float jumpForce_UpgradePerLVL = 0.5f;

    //xp
    private readonly int lVL1XpAmount = 5;
    private readonly int lvlGap = 5;

    //inventory available spaces
    

    void Awake()
    {
        health = 10;
        attackPower = 1;
        moveSpeed = 5f;
        jumpForce = 15f;

        xpSystem = GetComponent<XP_System>();

        if (xpSystem != null) { xpSystem.OnLevelUp += UpgradeStatus; }    
    }

    private void UpgradeStatus(int lvl)
    {
        health += health_UpgradePerLVL;
        attackPower += attackPower_UpgradePerLVL;
        moveSpeed += moveSpeed_UpgradePerLVL;
        jumpForce += jumpForce_UpgradePerLVL;

        Debug.Log($"Stats upgraded! Level {lvl}: HP={health}, ATK={attackPower}, Speed={moveSpeed}, Jump={jumpForce}");
    }

    public int Health => health;
    public int AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float JumpForce => jumpForce;
    public int LVL1XpAmount => lVL1XpAmount;
    public int LvlGap => lvlGap;
}
