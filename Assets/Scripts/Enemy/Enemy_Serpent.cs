using UnityEngine;

public class VenomShooting : EnemyBase
{
    // Variáveis públicas
    private Serpent_Stats typedStats;
    public GameObject venomPrefab; // Nome alterado para seguir convenções (venom -> venomPrefab)
    public Transform shootPoint;
    private Enemy_Health enemyHealth;
    private Transform player;

    public enum EnemyState { Idle, Biting, PreparingToShoot, Shooting }
    private EnemyState currentState;
    private float meleeCooldownTimer;
    private float shootCooldownTimer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public Color chargeColor = Color.green;
    private float currentChargeTime;
    public const float chargeDuration = 0.5f; // Duração fixa para o carregamento do disparo
    void Start()
    {
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

        if (enemyHealth != null && typedStats != null)
        {
            enemyHealth.Initialize(typedStats.maxHealth, typedStats.xpOnDeath, typedStats.dropA, typedStats.dropB, typedStats.dropC);
        }

        transform.localScale = stats.baseScale;
        meleeCooldownTimer = 0f;
        shootCooldownTimer = 0f;
        currentState = EnemyState.Idle;
    }

    void Update()
    {
        if (player == null || stats == null) return;

        float distance = Vector2.Distance(player.position, transform.position);

        // --- 1. Gestão de Cooldowns ---
        if (meleeCooldownTimer > 0f) meleeCooldownTimer -= Time.deltaTime;
        if (shootCooldownTimer > 0f) shootCooldownTimer -= Time.deltaTime;

        // --- 2. Transições de Estado (A FSM) ---
        HandleStateTransitions(distance);

        // --- 3. Ações de Estado ---
        HandleStateActions(distance);

        // --- 4. Virar o sprite ---
        // Faz o flip apenas se estiver ativo (Chasing, Preparando, Atacando)
        if (currentState != EnemyState.Idle)
        {
            FacePlayer();
        }
    }

    // NOVO MÉTODO PARA GERIR AS TRANSIÇÕES
    void HandleStateTransitions(float distance)
    {
        // Melee tem a prioridade máxima
        if (distance < typedStats.meleeRange)
        {
            currentState = EnemyState.Biting;
            return;
        }

        // Se saiu do alcance melee, volta a transição normal
        if (currentState == EnemyState.Biting)
        {
            currentState = EnemyState.Idle;
        }

        // Se estiver no estado de disparo/preparação, mantém-se até o ciclo terminar.
        if (currentState == EnemyState.PreparingToShoot || currentState == EnemyState.Shooting)
        {
            return;
        }

        // Lógica para iniciar o ciclo de Disparo
        if (distance < typedStats.distanceToPlayer)
        {
            if (shootCooldownTimer <= 0f)
            {
                // Começa o ciclo de disparo entrando em PREPARAÇÃO
                currentState = EnemyState.PreparingToShoot;
                currentChargeTime = chargeDuration; // Reset do timer de carga
                return;
            }
            else
            {
                // Está no alcance mas em cooldown, fica Idle
                currentState = EnemyState.Idle;
            }
        }
        else
        {
            // Fora do alcance, fica Idle
            currentState = EnemyState.Idle;
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
                // --- Ação: Reduzir o Timer e Mudar a Cor ---

                // 1. Reduz o tempo de preparação
                currentChargeTime -= Time.deltaTime;

                // 2. Efeito visual: Calcula a proporção de 0 a 1
                float t = 1f - (currentChargeTime / GetChargeDuration()); // t vai de 0 a 1
                if (spriteRenderer != null)
                {
                    // Lerp: Interpola suavemente entre a cor original e a cor de carga
                    spriteRenderer.color = Color.Lerp(originalColor, chargeColor, t);
                }
                if (currentChargeTime <= 0f)
                {
                    // Transição para Disparo (AÇÃO INSTANTÂNEA)
                    currentState = EnemyState.Shooting;
                }
                break;

            case EnemyState.Shooting:
                Shoot();
                // 1. IMPORTANTE: Reinicia a cor para a original imediatamente após disparar
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
                // 2. Reinicia o Cooldown
                shootCooldownTimer = typedStats.startTimeBtwAttack;

                // 3. Volta imediatamente para o estado Idle (ou Chasing, dependendo do alcance)
                currentState = EnemyState.Idle;
                break;

            case EnemyState.Idle:
                break;
        }
    }

    private float GetChargeDuration()
    {
        return chargeDuration; // Retorna a duração fixa definida
    }

    void Shoot()
    {
        // Certifica-se de que o prefab está atribuído
        if (venomPrefab != null && shootPoint != null)
        {
            GameObject projectile = Instantiate(venomPrefab, shootPoint.position, Quaternion.identity);
        }
    }

    void Bite()
    {
        // Usa o meleeCooldownTimer
        if (meleeCooldownTimer <= 0f)
        {
            if (player.TryGetComponent<HealthSystem>(out var ph) || (ph = player.GetComponentInParent<HealthSystem>()) != null)
            {
                if (typedStats.biteDamage <= 0) Debug.LogWarning("VenomShooting: biteDamage <= 0");
                ph.TakeDamage(typedStats.biteDamage);
                Debug.Log("Serpent Bite! Player hit.");
            }
            else
            {
                Debug.LogWarning("VenomShooting: Player HealthSystem not found.");
            }

            meleeCooldownTimer = typedStats.startTimeBtwAttack;
        }
    }

    // Função para virar o sprite para onde o player está
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, typedStats.distanceToPlayer);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, typedStats.meleeRange);
    }
}