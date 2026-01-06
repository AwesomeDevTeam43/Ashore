using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player_Health : MonoBehaviour
{
    private HealthSystem healthSystem;
    private XP_System xP_System;

    public bool godMode = false;

    [Header("Player Stats")]
    [SerializeField] private PlayerStats playerStats;
    public bool IsAlive = true;

    private int currentHealth;
    public int Health => currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private int previousHealth;
    private bool processingDeath;

    private Player_Camera playerCamera;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.OnHealthChanged += OnPlayerHealthChanged;

        xP_System = GetComponent<XP_System>();
        if (xP_System != null)
        {
            xP_System.OnLevelUp += UpdateHealthStat;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        playerCamera = FindFirstObjectByType<Player_Camera>();
    }

    private void OnEnable()
    {
        if (GameFlowState.IsLoading)
        {
            return;
        }
        if (playerStats != null)
        {
            UpdateHealthStat(1); // Initialize with level 1 stats
        }
    }

    private void Start()
    {
        if (GameFlowState.IsLoading)
        {
            return;
        }
        Debug.Log($"Player_Health: Initializing HealthSystem with {currentHealth} HP");
        healthSystem.Initialize(currentHealth);
        previousHealth = healthSystem.CurrentHealth;
    }

    private void Update()
    {
        HandleGodModeToggle();
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnPlayerHealthChanged;
        }

        if (xP_System != null)
        {
            xP_System.OnLevelUp -= UpdateHealthStat;
        }
    }

    public void UpdateHealthStat(int level)
    {
        int previousMaxHealth = currentHealth;
        int currentHealthPoints = healthSystem != null ? healthSystem.CurrentHealth : 0;

        currentHealth = playerStats.GetHealth(level);

        if (healthSystem != null)
        {
            int healthDifference = currentHealth - previousMaxHealth;
            int newCurrentHealth = currentHealthPoints + healthDifference;

            // Update max health and set current health
            healthSystem.MaxHealth = currentHealth;
            healthSystem.SetHealth(newCurrentHealth);
            
            Debug.Log($"Player_Health: Level up! Max HP: {currentHealth}, Current HP: {newCurrentHealth}");
        }
    }

    public void SetHealth(int health)
    {
        healthSystem.SetHealth(health);
        previousHealth = health;
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {        
        // IMPORTANTE: NÃO chamar SetHealth aqui - isso causa loop e eventos duplicados!
        // O HealthSystem já atualizou internamente antes de invocar este evento

        // God Mode: restaura vida instantaneamente
        if (godMode && currentHealth < maxHealth)
        {
            Debug.Log("Player_Health: God Mode ativo - restaurando HP");
            // Aqui pode chamar SetHealth porque é intencional restaurar
            healthSystem.SetHealth(maxHealth);
            previousHealth = maxHealth;
            return;
        }

        // Efeito visual de dano
        if (currentHealth < previousHealth)
        {
            StartCoroutine(DamageEffect());
            playerCamera?.StartCameraShake();
        }

        // Atualiza o HP anterior
        previousHealth = currentHealth;

        // Verifica morte
        if (currentHealth <= 0)
        {
            if (processingDeath)
            {
                return;
            }
            
            processingDeath = true;
            IsAlive = false;
            
            Debug.Log("Player_Health: Player morreu - recarregando o último save");
            
            // Destroy the persistent player so the reload spawns it cleanly
            PlayerPersistence.DestroyPersistentPlayer();
            ReloadLastSave();
        }
    }

    private IEnumerator DamageEffect()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
        }
    }

    private void HandleGodModeToggle()
    {
        if (!Input.GetKey("z")) return;
        if (!Input.GetKeyDown("p")) return;

        godMode = !godMode;

        if (godMode && healthSystem != null)
        {
            healthSystem.SetHealth(healthSystem.MaxHealth);
            previousHealth = healthSystem.MaxHealth;
        }

        Debug.Log(godMode ? "God Mode ENABLED" : "God Mode DISABLED");
    }

    private void ReloadLastSave()
    {
        string savedScene = SaveSystem.GetSavedSceneName(SaveSlotTracker.CurrentSlot);
        string targetScene = string.IsNullOrEmpty(savedScene) ? SceneManager.GetActiveScene().name : savedScene;

        GameFlowState.LoadGameOnStart = true;
        GameFlowState.IsLoading = true;
        SceneManager.LoadScene(targetScene);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("FallLevel"))
        {
            Debug.Log("Player_Health: Player caiu - voltando ao checkpoint e tomando dano");
            GetComponent<Player_Controller>().ReturnToLastPoint();
            healthSystem.TakeDamage(1);
        }
    }
}
