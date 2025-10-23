using UnityEngine;

public class Player_MeleeManager : MonoBehaviour
{
    [Header("Player Melee Settings")]
    public float movementTime = 0.2f;

    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 0.5f;
    private float cooldownTimer = 0f;

    [Header("Weapon Reference")]
    [SerializeField] private MeleeWeapon meleeWeapon;

    private Player_InputHandler inputHandler;

    private void Awake()
    {
        inputHandler = GetComponent<Player_InputHandler>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        CheckInput();
    }

    private void CheckInput()
    {
        if (inputHandler.AttackTriggered && cooldownTimer <= 0f)
        {
            meleeWeapon.PerformAttack();
            cooldownTimer = cooldownTime;
        }
    }
}
