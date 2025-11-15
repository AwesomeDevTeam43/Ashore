using UnityEngine;

public class Player_MeleeManager : MonoBehaviour
{
    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 0.5f;
    private float cooldownTimer = 0f;

    [Header("Weapon Reference")]
    [SerializeField] private MeleeWeapon meleeWeapon;

    private Player_InputHandler inputHandler;
    private Player_Controller playerController;

    private void Awake()
    {
        inputHandler = GetComponent<Player_InputHandler>();
        playerController = GetComponent<Player_Controller>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        CheckInput();
    }

    private void CheckInput()
    {
        // Only allow melee when main weapon selection is Melee
        bool meleeSelected = playerController == null ||
                             playerController.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee;
        if (meleeSelected && inputHandler.AttackTriggered && cooldownTimer <= 0f)
        {
            meleeWeapon.PerformAttack();
            cooldownTimer = cooldownTime;
        }
    }
}
