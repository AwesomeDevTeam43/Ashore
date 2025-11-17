using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
[RequireComponent(typeof(Rigidbody2D))]
public class OldFriend_Boss : EnemyBase
{
    private FinalBoss_Stats TypedStats => stats as FinalBoss_Stats;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawn;
    [SerializeField] private Transform projectileSpawnSecondary; // second firing point

    [Header("Timing")]
    [SerializeField] private float injuredStopDuration = 10f;

    private Enemy_Health enemyHealth;
    private HealthSystem healthSystem;
    private Transform player;
    private Animator animator;
    private Rigidbody2D rb;

    private ShieldedCore[] cores;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private float shootTimer = 0f;
    private float currentShootInterval = 2f;
    private float projectileStopFollow = 5f;

    [Header("Stage Settings")]
    [SerializeField] private float stage2CooldownMultiplier = 0.85f;
    [SerializeField] private float stage4CooldownMultiplier = 0.7f;

    [Header("Movement")]
    [Tooltip("Horizontal follow responsiveness (larger = snappier follow on X axis)")]
    [SerializeField] private float followSpeed = 3f;
    [Tooltip("Position smoothing when applying target X/Y (larger = faster interpolation)")]
    [SerializeField] private float followLerpSpeed = 5f;
    [Header("Contact")]
    [Tooltip("Damage dealt to the player when they collide with the boss body (collision or non-activation trigger)")]
    [SerializeField] private int contactDamage = 2;

    private int currentStage = 1; // 1..4
    private bool spawnToggle = false;
    private bool shootDouble = false;

    private bool isStopped = false;
    private bool coreDestroyedDuringPhase = false;
    private Coroutine injuredCoroutine;
    private bool injuredPhaseEnded = false;

    private readonly float[] thresholds = new float[] { 0.75f, 0.5f, 0.25f };
    private bool[] thresholdTriggered;

    // Returns a minimum allowed health (clamp) for incoming damage so that damage
    // doesn't reduce health below the next configured threshold. Returns null if
    // no clamping should be applied.
    public int? GetHealthClampForIncomingDamage(int incomingDamage)
    {
        if (healthSystem == null) return null;
        int current = healthSystem.CurrentHealth;
        int max = healthSystem.MaxHealth;
        int candidate = current - incomingDamage;

        for (int i = 0; i < thresholds.Length; i++)
        {
            int thresholdHp = Mathf.RoundToInt(max * thresholds[i]);
            if (current > thresholdHp && candidate < thresholdHp)
            {
                // Clamp to this threshold so the hit cannot skip below it.
                return thresholdHp;
            }
        }

        return null;
    }

    private void Start()
    {
        enemyHealth = GetComponent<Enemy_Health>();
        healthSystem = GetComponent<HealthSystem>();
        animator = GetComponentInChildren<Animator>();

        // Immediately unparent to avoid inheriting any parent's physics motion
        if (transform.parent != null)
        {
            var parentRb = transform.parent.GetComponent<Rigidbody2D>();
            transform.SetParent(null);
        }

        // configure all Rigidbody2D on this object and children to avoid falling due to gravity
        var rbs = GetComponentsInChildren<Rigidbody2D>(true);
        int fixedCount = 0;
        foreach (var r in rbs)
        {
            if (r == null) continue;
            r.bodyType = RigidbodyType2D.Kinematic;
            r.gravityScale = 0f;
            r.constraints = RigidbodyConstraints2D.FreezeRotation;
            fixedCount++;
        }
        rb = GetComponent<Rigidbody2D>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // initialize health via existing Enemy_Health as before
        if (enemyHealth != null && TypedStats != null)
        {
            enemyHealth.Initialize(TypedStats.maxHealth, TypedStats.xpOnDeath, TypedStats.dropA, TypedStats.dropB, TypedStats.dropC);
        }

        if (healthSystem != null)
            healthSystem.OnHealthChanged += OnHealthChanged;

        // populate cores (children first, then optional scene fallback)
        RefreshCores(true);

        thresholdTriggered = new bool[thresholds.Length];

        currentShootInterval = TypedStats != null ? TypedStats.baseFireInterval : 2f;
        // ensure stage defaults
        currentStage = 1;
        spawnToggle = false;
        shootDouble = false;

        // cache sprite renderers and their original colors
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    originalColors[i] = spriteRenderers[i].color;
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            if (rb.bodyType != RigidbodyType2D.Kinematic)
                rb.bodyType = RigidbodyType2D.Kinematic;
            if (rb.gravityScale != 0f)
                rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void RefreshCores(bool allowSceneFallback)
    {
        // unsubscribe previous handlers
        if (cores != null)
        {
            for (int i = 0; i < cores.Length; i++)
            {
                var old = cores[i];
                if (old != null)
                    old.OnCoreDestroyed -= HandleCoreDestroyed;
            }
        }

        // try children first
        var found = GetComponentsInChildren<ShieldedCore>(true);
        if (found != null && found.Length > 0)
        {
            cores = found;
            for (int i = 0; i < cores.Length; i++)
            {
                var c = cores[i];
                if (c == null) continue;
                c.SetProtected(true);
                c.OnCoreDestroyed += HandleCoreDestroyed;
            }
            return;
        }

        if (!allowSceneFallback) 
        {
            cores = new ShieldedCore[0];
            return;
        }

        // Scene fallback: search all ShieldedCore instances (including inactive)
        var all = UnityEngine.Resources.FindObjectsOfTypeAll<ShieldedCore>();
        var picked = new System.Collections.Generic.List<ShieldedCore>();

        // Prefer cores that are parented under this boss
        foreach (var s in all)
        {
            if (s == null) continue;
            if (s.transform.IsChildOf(transform)) picked.Add(s);
        }

        // If none are children, fall back to nearest cores by distance (within 20 units)
        if (picked.Count == 0)
        {
            float maxDist = 20f;
            foreach (var s in all)
            {
                if (s == null) continue;
                float d = Vector2.Distance(transform.position, s.transform.position);
                if (d <= maxDist) picked.Add(s);
            }
        }

        if (picked.Count > 0)
        {
            cores = picked.ToArray();
            for (int i = 0; i < cores.Length; i++)
            {
                var c = cores[i];
                if (c == null) continue;
                c.SetProtected(true);
                c.OnCoreDestroyed += HandleCoreDestroyed;
            }
        }
        else
        {
            cores = new ShieldedCore[0];
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (!isStopped)
        {
            // Vertical floating
            float y = Mathf.Sin(Time.time * (TypedStats != null ? TypedStats.floatSpeed : 1f)) * (TypedStats != null ? TypedStats.floatAmplitude : 0.5f);
            float baseY = (transform.parent != null ? transform.parent.position.y : transform.position.y) + y;

            // Follow player only on X axis (smooth)
            float desiredX = Mathf.Lerp(transform.position.x, player.position.x, followSpeed * Time.deltaTime);

            Vector3 targetPos = new Vector3(desiredX, baseY, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followLerpSpeed);
        }

        if (isStopped) return;

        shootTimer += Time.deltaTime;
        if (shootTimer >= currentShootInterval)
        {
            shootTimer = 0f;
            ShootAtPlayer();
        }
    }

    private void ShootAtPlayer()
    {
        if (projectilePrefab == null || player == null) return;

        // Choose spawn behavior based on current stage
        Transform spawnA = projectileSpawn != null ? projectileSpawn : transform;
        Transform spawnB = projectileSpawnSecondary != null ? projectileSpawnSecondary : projectileSpawn;

        if (currentStage == 3 && shootDouble)
        {
            // spawn two projectiles at once (from both points)
            Debug.Log($"{name}: Stage 3 double-shot from {spawnA.position} and {spawnB.position} towards player at {player.position}");
            SpawnProjectileAt(spawnA);
            if (spawnB != null && spawnB != spawnA)
                SpawnProjectileAt(spawnB);
            return;
        }

        // stage 1 (and others unless double) alternate between spawn points
        Transform chosen = spawnA;
        if (spawnB != null && spawnB != spawnA)
        {
            chosen = spawnToggle ? spawnB : spawnA;
            spawnToggle = !spawnToggle;
        }

        Debug.Log($"{name}: Shooting projectile from {chosen.position} towards player at {player.position} (stage={currentStage})");
        SpawnProjectileAt(chosen);
    }

	private void SpawnProjectileAt(Transform spawn)
	{
		GameObject go = Instantiate(projectilePrefab, spawn.position, Quaternion.identity);
		var proj = go.GetComponent<BossProjectile>();
		if (proj != null)
		{
			Vector2 dir = (player.position - spawn.position);
			float speedMult = 1f;
			proj.Initialize(dir, projectileStopFollow, speedMult, TypedStats != null ? TypedStats.projectileDamage : 1);
			proj.speed = TypedStats != null ? TypedStats.projectileSpeed : proj.speed;
		}
	}

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        float pct = (float)currentHealth / maxHealth;

        for (int i = 0; i < thresholds.Length; i++)
        {
            if (!thresholdTriggered[i] && pct <= thresholds[i])
            {
                thresholdTriggered[i] = true;
                injuredCoroutine = StartCoroutine(InjuredPhase(i));
                break;
            }
        }
    }

    private IEnumerator InjuredPhase(int stageIndex)
    {
        isStopped = true;
        // reset shoot timer so the boss doesn't immediately fire when recovering
        shootTimer = 0f;
        coreDestroyedDuringPhase = false;
        injuredPhaseEnded = false;
        Debug.Log($"{name}: Entering InjuredPhase stage {stageIndex}. Unprotecting cores for {injuredStopDuration}s or until one is destroyed.");

        try
        {
            // change sprite color(s) to green to indicate injuredF
            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                        spriteRenderers[i].color = Color.green;
                }
            }

            // make boss temporarily invulnerable
            if (enemyHealth != null)
                enemyHealth.SetDamageable(false);

            // unprotect all cores so player can damage them
            // refresh cores at injured phase in case they were created or parented after Start
            RefreshCores(true);
            Debug.Log($"{name}: InjuredPhase - cores count={(cores!=null?cores.Length:0)}");
            if (cores != null)
            {
                for (int i = 0; i < cores.Length; i++)
                {
                    var c = cores[i];
                    if (c == null)
                    {
                        Debug.Log($"{name}: InjuredPhase - core[{i}] is null");
                        continue;
                    }
                    Debug.Log($"{name}: InjuredPhase - unprotecting core[{i}] = {c.name}");
                    c.SetProtected(false);
                }
            }

            float timer = 0f;
            while (timer < injuredStopDuration && !coreDestroyedDuringPhase)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (!coreDestroyedDuringPhase)
            {
                // player failed to destroy a core: heal by one stage (25% of max)
                if (healthSystem != null)
                {
                    int healAmount = Mathf.RoundToInt(healthSystem.MaxHealth * 0.25f);
                    Debug.Log($"{name}: No core destroyed during injured phase; healing boss by {healAmount}.");
                    healthSystem.Heal(healAmount);
                }
            }

            // protect remaining cores again
            foreach (var c in cores)
            {
                if (c != null)
                    c.SetProtected(true);
            }

            // boss becomes damageable again and resumes
            if (enemyHealth != null)
                enemyHealth.SetDamageable(true);
        }
        finally
        {
            // central cleanup (idempotent)
            Debug.Log($"{name}: InjuredPhase finishing; running cleanup.");
            ExitInjuredPhaseCleanup();
            injuredCoroutine = null;

            // advance stage after injured phase finishes
            int newStage = Mathf.Clamp(stageIndex + 2, 1, 4);
            ApplyStageSettings(newStage);
        }
    }

    private void ApplyStageSettings(int stage)
    {
        if (stage == currentStage) return;
        currentStage = stage;
        Debug.Log($"{name}: Applying stage settings -> stage {stage}");
        switch (stage)
        {
            case 1:
                // default: alternate between spawn points
                spawnToggle = false;
                shootDouble = false;
                break;
            case 2:
                // reduce cooldown moderately
                currentShootInterval *= stage2CooldownMultiplier;
                shootDouble = false;
                break;
            case 3:
                // shoot two projectiles at once
                shootDouble = true;
                break;
            case 4:
                // decrease cooldown more
                currentShootInterval *= stage4CooldownMultiplier;
                // stage 4: faster and double-shot as well
                shootDouble = true;
                break;
        }
    }

    private void HandleCoreDestroyed(ShieldedCore core)
    {
        // signal the injured phase that a core was destroyed
        coreDestroyedDuringPhase = true;
        Debug.Log($"{name}: Core destroyed -> {core.name}. Ending injured phase now.");
        // perform immediate, idempotent cleanup so boss returns to normal now
        ExitInjuredPhaseCleanup();

        // increase fire rate (shoot faster) when a core is destroyed
        currentShootInterval *= 0.85f;
    }

	private void ExitInjuredPhaseCleanup()
	{
		if (injuredPhaseEnded) return; // already cleaned up
		injuredPhaseEnded = true;

		// protect other remaining cores again
		foreach (var c in cores)
		{
			if (c != null)
				c.SetProtected(true);
		}

		// boss becomes damageable again so player can damage boss
		if (enemyHealth != null)
			enemyHealth.SetDamageable(true);

		// restore sprite colors immediately
		if (spriteRenderers != null && originalColors != null)
		{
			for (int i = 0; i < spriteRenderers.Length && i < originalColors.Length; i++)
			{
				if (spriteRenderers[i] != null)
					spriteRenderers[i].color = originalColors[i];
			}
		}

		// ensure boss resumes
		isStopped = false;
	}

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= OnHealthChanged;

        foreach (var c in cores)
        {
            if (c != null)
                c.OnCoreDestroyed -= HandleCoreDestroyed;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!other.CompareTag("Player")) return;

        var hs = other.GetComponentInParent<HealthSystem>();
        if (hs != null)
        {
            Debug.Log($"{name}: Player collided with boss body — applying {contactDamage} damage.");
            hs.TakeDamage(contactDamage, gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player")) return;

        // Apply contact damage for boss triggers (there is no activationArea configured in this build)

        var hsTrig = other.GetComponentInParent<HealthSystem>();
        if (hsTrig != null)
        {
            Debug.Log($"{name}: Player touched boss trigger — applying {contactDamage} damage.");
            hsTrig.TakeDamage(contactDamage, gameObject);
        }
    }
}
