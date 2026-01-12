using UnityEngine;

public class VenomShooting : EnemyBase
{
    private Serpent_Stats typedStats;
    public GameObject venomPrefab;
    public Transform shootPoint;
    private Transform player;

    [Header("Audio")]
    [SerializeField] private AudioClip idleClip;
    [Tooltip("Usado tanto para PreparingToShoot quanto Shooting (mesmo clip).")]
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip biteClip;
    [SerializeField] private float idleIntervalSeconds = 3f;
    [SerializeField] private float hearDistance = 12f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField, Range(0f, 1f)] private float idleVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    private float idleTimer;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool useAttackTrigger = true;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string attackBool = "isAttacking";

    public enum EnemyState { Idle, Biting, PreparingToShoot, Shooting }
    private EnemyState currentState;
    private float meleeCooldownTimer;
    private float shootCooldownTimer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public Color chargeColor = Color.green;
    private float currentChargeTime;
    public const float chargeDuration = 0.5f;
    public float animationAttackCooldown = 1.0f;

    protected override void Start()
    {
        base.Start();
        typedStats = stats as Serpent_Stats;
        enemyHealth = GetComponent<Enemy_Health>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        if (stats == null)
        {
            enabled = false;
            return;
        }
        
        transform.localScale = stats.baseScale;
        meleeCooldownTimer = 0f;
        shootCooldownTimer = 0f;
        currentState = EnemyState.Idle;
        idleTimer = Mathf.Max(0.01f, idleIntervalSeconds);
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        EnsureAudioSources();
    }

    void Update()
    {
        if (player == null || stats == null) return;
        float distance = Vector2.Distance(player.position, transform.position);
        if (meleeCooldownTimer > 0f) meleeCooldownTimer -= Time.deltaTime;
        if (shootCooldownTimer > 0f) shootCooldownTimer -= Time.deltaTime;
        HandleStateTransitions(distance);
        HandleStateActions(distance);

        HandleIdleAudio(distance);

        if (currentState != EnemyState.Idle)
        {
            FacePlayer();
        }
    }

    void HandleStateTransitions(float distance)
    {
        if (distance < typedStats.meleeRange)
        {
            SetState(EnemyState.Biting);
            return;
        }
        if (currentState == EnemyState.Biting)
        {
            SetState(EnemyState.Idle);
        }
        if (currentState == EnemyState.PreparingToShoot || currentState == EnemyState.Shooting)
        {
            return;
        }
        if (distance < typedStats.distanceToPlayer)
        {
            if (shootCooldownTimer <= 0f)
            {
                SetState(EnemyState.PreparingToShoot);
                currentChargeTime = chargeDuration;
                return;
            }
            else
            {
                SetState(EnemyState.Idle);
            }
        }
        else
        {
            SetState(EnemyState.Idle);
        }
    }

    void HandleStateActions(float distance)
    {
        switch (currentState)
        {
            case EnemyState.Biting:
                Bite();
                break;
            case EnemyState.PreparingToShoot:
                currentChargeTime -= Time.deltaTime;
                float t = 1f - (currentChargeTime / GetChargeDuration());
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(originalColor, chargeColor, t);
                }
                if (currentChargeTime <= 0f)
                {
                    SetState(EnemyState.Shooting);
                }
                break;
            case EnemyState.Shooting:
                if (animator != null)
                {
                    if (useAttackTrigger)
                    {
                        animator.SetTrigger(attackTrigger);
                        
                    } 
                    else
                    {
                        animator.SetBool(attackBool, true);
                    }
                    shootCooldownTimer = Mathf.Max(shootCooldownTimer, animationAttackCooldown);
                }
                else
                {
                    Shoot();
                    shootCooldownTimer = typedStats.startTimeBtwAttack;
                }
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
                SetState(EnemyState.Idle);
                break;
            case EnemyState.Idle:
                break;
        }
    }

    private float GetChargeDuration()
    {
        return chargeDuration;
    }

    public void Shoot()
    {
        if (venomPrefab != null && shootPoint != null)
        {
            Instantiate(venomPrefab, shootPoint.position, Quaternion.identity);
        }
        if (animator != null && !useAttackTrigger)
        {
            animator.SetBool(attackBool, false);
        }
    }

    void Bite()
    {
        if (meleeCooldownTimer <= 0f)
        {
            if (player.TryGetComponent<HealthSystem>(out var ph) || (ph = player.GetComponentInParent<HealthSystem>()) != null)
            {
                if (currentDamage <= 0) Debug.LogWarning("VenomShooting: currentDamage <= 0");
                ph.TakeDamage((int)currentDamage);
                PlaySfx(biteClip);
                Debug.Log("Serpent Bite! Player hit.");
            }
            else
            {
                Debug.LogWarning("VenomShooting: Player HealthSystem not found.");
            }
            meleeCooldownTimer = typedStats.startTimeBtwAttack;
        }
    }

    private void SetState(EnemyState newState)
    {
        if (currentState == newState) return;
        EnemyState previous = currentState;
        currentState = newState;
        OnStateEntered(newState, previous);
    }

    private void OnStateEntered(EnemyState state, EnemyState previous)
    {
        if (state == EnemyState.PreparingToShoot)
        {
            StopIdleAudio();
            PlaySfx(shootClip);
        }

        if (state == EnemyState.Idle)
        {
            idleTimer = Mathf.Max(0.01f, idleIntervalSeconds);
        }
    }

    private void HandleIdleAudio(float distanceToPlayer)
    {
        if (audioSource == null) return;

        bool canHear = distanceToPlayer <= hearDistance;
        UpdateAudioSourceDistance(audioSource);

        if (!canHear || currentState != EnemyState.Idle)
        {
            StopIdleAudio();
            return;
        }

        if (idleClip == null) return;

        idleTimer -= Time.deltaTime;
        if (idleTimer > 0f) return;

        // Play idle as the main clip so we can stop it cleanly when state changes.
        audioSource.clip = idleClip;
        audioSource.volume = idleVolume;
        audioSource.loop = false;
        audioSource.Play();
        idleTimer = Mathf.Max(0.01f, idleIntervalSeconds);
    }

    private void StopIdleAudio()
    {
        // Only stop if we're currently playing the idle clip, so we don't cut off other SFX.
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == idleClip)
        {
            audioSource.Stop();
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || audioSource == null || player == null) return;
        float distance = Vector2.Distance(player.position, transform.position);
        if (distance > hearDistance) return;

        // Prioritize SFX over idle (single AudioSource).
        StopIdleAudio();
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void EnsureAudioSources()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource(audioSource);
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = Mathf.Max(1f, hearDistance);
        // Route enemy audio through the project's SFX AudioMixerGroup when available
        if (AudioManager.Instance != null)
        {
            var g = AudioManager.Instance.GetSFXGroup();
            if (g != null) source.outputAudioMixerGroup = g;
        }
    }

    private void UpdateAudioSourceDistance(AudioSource source)
    {
        if (source == null) return;
        float max = Mathf.Max(1f, hearDistance);
        if (!Mathf.Approximately(source.maxDistance, max))
        {
            source.maxDistance = max;
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        float dir = (player.position.x >= transform.position.x) ? 1f : -1f;
        Vector3 s = stats.baseScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    void OnDrawGizmos()
    {
        if (stats == null) return;
        if (typedStats == null)
        {
            typedStats = stats as Serpent_Stats;
            if (typedStats == null) return;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, typedStats.distanceToPlayer);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, typedStats.meleeRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, hearDistance);
    }
}