using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class Boss : MonoBehaviour, ISaveable
{
    [SerializeField] public int enemyHealth = 5;
    private HealthSystem bossHealth;
    bool isDead = false;
    
    // Controls whether the boss is allowed to trigger its Laser attack.
    // When the Laser is used it will be disabled until a Combo run resets it.
    [HideInInspector]
    public bool canUseLaser = true;

    // Time when the boss last successfully performed an attack against the player
    [HideInInspector]
    public float lastAttackTime = -Mathf.Infinity;

    [Header("Laser Timing")]
    [Tooltip("If the boss has not attacked the player for this many seconds, allow Laser regardless of canUseLaser")]
    public float laserIdleThreshold = 5f;

    // Track when phase 2 was entered (so the idle countdown only begins after phase 2 starts)
    private float phase2EnteredTime = -Mathf.Infinity;

    // Track the last time the boss launched a Laser (so the idle countdown is renewable each time he shoots)
    private float lastLaserTime = -Mathf.Infinity;

    // Returns true when the boss has been in phase 2 and the time since the later of
    // (phase2 entry, last laser shot) is at least laserIdleThreshold.
    public bool HasBeenIdleLongEnough()
    {
        if (float.IsNegativeInfinity(phase2EnteredTime)) return false; // phase 2 not started yet
        float reference = Mathf.Max(phase2EnteredTime, lastLaserTime);
        return Time.time - reference >= laserIdleThreshold;
    }

    // Called when the boss uses/launches a laser; records the last laser time so the idle timer renews
    public void NotifyLaserUsed()
    {
        lastLaserTime = Time.time;
    }

    private void Awake()
    {
        bossHealth = GetComponent<HealthSystem>();
        bossHealth.Initialize(enemyHealth);
        bossHealth.OnHealthChanged += OnHealthChanged;

        anim = GetComponent<Animator>();

        if (laserSfxSource == null)
        {
            laserSfxSource = GetComponent<AudioSource>();
        }
    }
    

    [Range(0f, 1f)]
    [SerializeField] private float punchSfxVolume = 1f;

    private void PlaySharedSfx(AudioClip clip, float volume)
    {
        if (clip == null) return;

        // Use the same AudioSource for all boss SFX.
        if (laserSfxSource == null)
        {
            laserSfxSource = GetComponent<AudioSource>();
            if (laserSfxSource == null)
            {
                laserSfxSource = gameObject.AddComponent<AudioSource>();
                laserSfxSource.playOnAwake = false;
            }
        }

        if (AudioManager.Instance != null && laserSfxSource.outputAudioMixerGroup == null)
        {
            var group = AudioManager.Instance.GetSFXGroup();
            if (group != null) laserSfxSource.outputAudioMixerGroup = group;
        }

        laserSfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void PlayPunchSfx()
    {
        PlaySharedSfx(punchSfx, punchSfxVolume);
    }

    public object CaptureState()
    {
        return new BossData
        {
            hasDied = this.isDead
        };

    }
    
    public void RestoreState(object state)
    {
        var saveData = (BossData)state;
        this.isDead = saveData.hasDied;

        if (isDead)
        {
            Destroy(this);
        }
    }

    [Header("Laser Spawn")]
    [Tooltip("Prefab of the laser to spawn from the boss's hand")]
    public GameObject laserPrefab;

    [Tooltip("Transform representing the boss's hand where the laser should spawn")]
    public Transform laserHand;

    [Header("Laser SFX")]
    [Tooltip("AudioSource used to play the laser SFX (if null, will try to use one on this GameObject)")]
    [SerializeField] private AudioSource laserSfxSource;

    [Tooltip("SFX clip to play when the boss spawns the laser")]
    [SerializeField] private AudioClip laserSpawnSfx;
    [SerializeField] private AudioClip punchSfx;

    [Range(0f, 1f)]
    [SerializeField] private float laserSpawnSfxVolume = 1f;

    // Keep a reference to the currently spawned laser so we only resume when it finishes
    private Laser activeSpawnedLaser;

    // Called (for example) from an Animation Event on the boss's Laser animation last frame.
    // This instantiates the laser prefab at the configured hand transform, then pauses the boss
    // animator so the boss remains visually stuck on the last frame until the laser finishes
    // and calls back via Laser.FinishAndDestroy().
    public void SpawnLaserAndHold()
    {
        // Defensive: if we've already spawned a laser and haven't resumed yet, skip spawning another
        if (activeSpawnedLaser != null)
        {
            Debug.Log("Boss.SpawnLaserAndHold: spawn skipped because an active spawned laser already exists.");
            return;
        }

        if (laserPrefab == null || laserHand == null)
        {
            Debug.LogWarning("Boss.SpawnLaserAndHold: missing laserPrefab or laserHand on Boss.");
            return;
        }

        // Instantiate the laser at the hand's position and rotation
        GameObject go = Instantiate(laserPrefab, laserHand.position, laserHand.rotation, laserHand);
        Laser l = go.GetComponent<Laser>();
        if (l != null)
        {
            // Set callback so we resume the animator when the laser finishes
            activeSpawnedLaser = l;
            l.onFinished = () => ResumeAfterSpawnedLaser(l);
        }

        // Play SFX (one-shot) when the laser spawns
        PlaySharedSfx(laserSpawnSfx, laserSpawnSfxVolume);

        // Pause the boss animator so it remains on the last frame
        if (anim != null)
        {
            anim.speed = 0f;
        }
    }

    private void ResumeAfterSpawnedLaser(Laser finishedLaser)
    {
        // Only resume if this is the active spawned laser (defensive)
        if (finishedLaser != activeSpawnedLaser) return;
        activeSpawnedLaser = null;
        if (anim != null)
        {
            anim.speed = 1f;
        }
    }

    private bool isFlipped = false;
    [SerializeField] private Vector2 attackOffset;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask attackMask;
    [SerializeField] private int attackDamage = 2;
    Animator anim;



    public void LookAtPlayer(Transform player)
    {
        Vector3 flipped = transform.localScale;
        flipped.z *= -1f;

        if (transform.position.x > player.position.x && isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = false;
        }
        else if (transform.position.x < player.position.x && !isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = true;
        }
    }

    public void AttackPlayer()
    {
        PlayPunchSfx();

        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, LayerMask.GetMask("Player"));
        if (colInfo != null)
        {
            var hs = colInfo.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.TakeDamage(attackDamage);
                // record that the boss attacked the player just now
                lastAttackTime = Time.time;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, attackRange);
    }



    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= maxHealth / 2)
        {
            Debug.Log("Boss phase 2");
            anim.SetBool("Phase2", true);
            // mark phase2 entry time so the laser idle countdown starts now
            phase2EnteredTime = Time.time;
            // ensure laser doesn't fire immediately on phase transition; require idle or combo
            canUseLaser = false;
            attackDamage = 4;
        }
        if (currentHealth <= 0)
        {
            Debug.Log("Boss defeated");
            isDead = true;
            Destroy(gameObject);
        }
    }

    [System.Serializable]
    private struct BossData
    {
        public bool hasDied;
    }
}
