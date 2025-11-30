using UnityEngine;

public class Player_MeleeManager : MonoBehaviour
{
    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 0.5f;
    private float cooldownTimer = 0f;

    [Header("Weapon Reference")]
    [SerializeField] private MeleeWeapon meleeWeapon;

    [Header("Attack Buffering")]
    [SerializeField] private bool enableInputBuffer = true;
    [SerializeField] private float bufferTime = 0.15f;
    private float bufferTimer = 0f;
    private bool attackBuffered = false;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] swingSounds;

    private Player_InputHandler inputHandler;
    private Player_Controller playerController;

    private void Awake()
    {
        inputHandler = GetComponent<Player_InputHandler>();
        playerController = GetComponent<Player_Controller>();
        
        if (meleeWeapon == null)
            meleeWeapon = GetComponentInChildren<MeleeWeapon>();
            
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        bufferTimer -= Time.deltaTime;
        
        CheckInput();
        ProcessBufferedAttack();
    }

    private void CheckInput()
    {
        bool meleeSelected = playerController == null ||
                             playerController.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee;

        if (meleeSelected && inputHandler.AttackTriggered)
        {
            if (cooldownTimer <= 0f)
            {
                PerformAttack();
            }
            else if (enableInputBuffer)
            {
                // Buffer the input
                attackBuffered = true;
                bufferTimer = bufferTime;
            }
        }
    }

    private void ProcessBufferedAttack()
    {
        if (attackBuffered && bufferTimer > 0f && cooldownTimer <= 0f)
        {
            PerformAttack();
            attackBuffered = false;
        }
        else if (bufferTimer <= 0f)
        {
            attackBuffered = false;
        }
    }

    private void PerformAttack()
    {
        if (meleeWeapon == null) return;

        meleeWeapon.PerformAttack();
        PlaySwingSound();
        cooldownTimer = cooldownTime;
    }

    private void PlaySwingSound()
    {
        if (swingSounds == null || swingSounds.Length == 0 || audioSource == null)
            return;

        AudioClip clip = swingSounds[Random.Range(0, swingSounds.Length)];
        audioSource.PlayOneShot(clip);
    }

    // Public methods for UI/feedback
    public bool IsOnCooldown() => cooldownTimer > 0f;
    public float GetCooldownProgress() => Mathf.Clamp01(1f - (cooldownTimer / cooldownTime));

    // Power-up method
    public void ModifyCooldown(float multiplier)
    {
        cooldownTime /= multiplier;
        cooldownTime = Mathf.Max(0.1f, cooldownTime);
    }
}
