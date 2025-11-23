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
    [SerializeField] private Transform projectileSpawnSecondary;

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
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float followLerpSpeed = 5f;

    [Header("Activation")]
    [SerializeField] private bool bossActive = false;

    [Header("Contact")]
    [SerializeField] private int contactDamage = 1;
    [Header("Animation")]
    [SerializeField] private string animIdleParam = "Idle";
    [SerializeField] private string animInjuredParam = "Injured";

    private int animIdleHash;
    private int animInjuredHash;

    private int currentStage = 1;
    private bool spawnToggle = false;
    private bool shootDouble = false;

    private bool isStopped = false;
    private bool coreDestroyedDuringPhase = false;
    private Coroutine injuredCoroutine;
    private bool injuredPhaseEnded = false;

    private readonly float[] thresholds = new float[] { 0.75f, 0.5f, 0.25f };
    private bool[] thresholdTriggered;

    public bool BossActive
    {
        get => bossActive;
        set => SetBossActive(value);
    }

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

        if (animator != null)
        {
            animIdleHash = Animator.StringToHash(animIdleParam);
            animInjuredHash = Animator.StringToHash(animInjuredParam);
            animator.SetBool(animIdleHash, true);
            animator.SetBool(animInjuredHash, false);
        }

        if (transform.parent != null)
        {
            var parentRb = transform.parent.GetComponent<Rigidbody2D>();
            transform.SetParent(null);
        }

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

        if (enemyHealth != null && TypedStats != null)
        {
            enemyHealth.Initialize(TypedStats.maxHealth, TypedStats.xpOnDeath, TypedStats.woodDrop, TypedStats.stoneDrop, TypedStats.ropeDrop);
        }

        if (healthSystem != null)
            healthSystem.OnHealthChanged += OnHealthChanged;

        RefreshCores(true);

        thresholdTriggered = new bool[thresholds.Length];

        currentShootInterval = TypedStats != null ? TypedStats.baseFireInterval : 2f;
        currentStage = 1;
        spawnToggle = false;
        shootDouble = false;

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

        ApplyBossActivationState();
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
        if (cores != null)
        {
            for (int i = 0; i < cores.Length; i++)
            {
                var old = cores[i];
                if (old != null)
                    old.OnCoreDestroyed -= HandleCoreDestroyed;
            }
        }

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

        var all = UnityEngine.Resources.FindObjectsOfTypeAll<ShieldedCore>();
        var picked = new System.Collections.Generic.List<ShieldedCore>();

        foreach (var s in all)
        {
            if (s == null) continue;
            if (s.transform.IsChildOf(transform)) picked.Add(s);
        }

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
        if (!bossActive)
        {
            ApplyBossActivationState();
            return;
        }

        if (player == null) return;

        if (animator != null)
        {
            animator.SetBool(animIdleHash, !isStopped);
            animator.SetBool(animInjuredHash, isStopped);
        }
        if (!isStopped)
        {
            float y = Mathf.Sin(Time.time * (TypedStats != null ? TypedStats.floatSpeed : 1f)) * (TypedStats != null ? TypedStats.floatAmplitude : 0.5f);
            float baseY = (transform.parent != null ? transform.parent.position.y : transform.position.y) + y;

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
        if (!bossActive) return;
        if (projectilePrefab == null || player == null) return;

        Transform spawnA = projectileSpawn != null ? projectileSpawn : transform;
        Transform spawnB = projectileSpawnSecondary != null ? projectileSpawnSecondary : projectileSpawn;

        if (currentStage == 3 && shootDouble)
        {
            Debug.Log($"{name}: Stage 3 double-shot from {spawnA.position} and {spawnB.position} towards player at {player.position}");
            SpawnProjectileAt(spawnA);
            if (spawnB != null && spawnB != spawnA)
                SpawnProjectileAt(spawnB);
            return;
        }

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
        if (!bossActive) return;
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
        if (!bossActive) return;
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
        shootTimer = 0f;
        coreDestroyedDuringPhase = false;
        injuredPhaseEnded = false;
        Debug.Log($"{name}: Entering InjuredPhase stage {stageIndex}. Unprotecting cores for {injuredStopDuration}s or until one is destroyed.");

        try
        {
            if (enemyHealth != null)
                enemyHealth.SetDamageable(false);

            if (animator != null)
            {
                animator.SetBool(animInjuredHash, true);
                animator.SetBool(animIdleHash, false);
            }

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
                if (healthSystem != null)
                {
                    int healAmount = Mathf.RoundToInt(healthSystem.MaxHealth * 0.25f);
                    Debug.Log($"{name}: No core destroyed during injured phase; healing boss by {healAmount}.");
                    healthSystem.Heal(healAmount);
                }
            }

            foreach (var c in cores)
            {
                if (c != null)
                    c.SetProtected(true);
            }

            if (enemyHealth != null)
                enemyHealth.SetDamageable(true);
        }
        finally
        {
            Debug.Log($"{name}: InjuredPhase finishing; running cleanup.");
            ExitInjuredPhaseCleanup();
            injuredCoroutine = null;

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
                spawnToggle = false;
                shootDouble = false;
                break;
            case 2:
                currentShootInterval *= stage2CooldownMultiplier;
                shootDouble = false;
                break;
            case 3:
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
        if (!bossActive) return;
        coreDestroyedDuringPhase = true;
        Debug.Log($"{name}: Core destroyed -> {core.name}. Ending injured phase now.");
        ExitInjuredPhaseCleanup();

        currentShootInterval *= 0.85f;
    }

	private void ExitInjuredPhaseCleanup()
	{
		if (injuredPhaseEnded) return;
		injuredPhaseEnded = true;

		foreach (var c in cores)
		{
			if (c != null)
				c.SetProtected(true);
		}

		if (enemyHealth != null)
			enemyHealth.SetDamageable(true);

		if (spriteRenderers != null && originalColors != null)
		{
			for (int i = 0; i < spriteRenderers.Length && i < originalColors.Length; i++)
			{
				if (spriteRenderers[i] != null)
					spriteRenderers[i].color = originalColors[i];
			}
		}

		isStopped = false;

        if (animator != null)
        {
            animator.SetBool(animInjuredHash, false);
            animator.SetBool(animIdleHash, true);
        }
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
        if (!bossActive) return;
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
        if (!bossActive) return;
        if (other == null) return;
        if (!other.CompareTag("Player")) return;

        var hsTrig = other.GetComponentInParent<HealthSystem>();
        if (hsTrig != null)
        {
            Debug.Log($"{name}: Player touched boss trigger — applying {contactDamage} damage.");
            hsTrig.TakeDamage(contactDamage, gameObject);
        }
    }

    public void SetBossActive(bool active)
    {
        if (bossActive == active) return;
        bossActive = active;
        ApplyBossActivationState();
    }

    private void ApplyBossActivationState()
    {
        if (!bossActive)
        {
            shootTimer = 0f;
        }

        if (animator != null)
        {
            bool showInjured = bossActive && isStopped;
            animator.SetBool(animInjuredHash, showInjured);
            animator.SetBool(animIdleHash, !showInjured);

            if (!bossActive)
            {
                animator.SetBool(animIdleHash, true);
                animator.SetBool(animInjuredHash, false);
            }
        }
    }
}
