using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player_Health : MonoBehaviour
{
    private HealthSystem healthSystem;
    private XP_System xP_System;

    [Header("Player Stats")]
    [SerializeField] private PlayerStats playerStats;
    public bool IsAlive = true;

    private int currentHealth;
    public int Health => currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private int previousHealth;

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
        Debug.Log($"Initializing HealthSystem with {currentHealth} HP");
        healthSystem.Initialize(currentHealth);
        previousHealth = healthSystem.CurrentHealth;
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
        }
    }

    public void SetHealth(int health)
    {
        healthSystem.SetHealth(health);
        previousHealth = health;
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth < previousHealth)
        {
            StartCoroutine(DamageEffect());
        }

        previousHealth = currentHealth;

        if (currentHealth <= 0)
        {
            IsAlive = false;
            SceneManager.LoadScene("MainMenu");
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("FallLevel"))
        {
            GetComponent<Player_Controller>().ReturnToLastPoint();
            healthSystem.TakeDamage(1);
        }
    }
}
