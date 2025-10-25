using System.Collections;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    public BigCrab_Stats stats;
    [SerializeField] private int maxHealth;
    [SerializeField] private bool damageable = true;
    [SerializeField] private float invincibilityDuration = .2f;

    private bool hit;
    private HealthSystem healthSystem;
    private XP_System xP_System;
    private Drop_Materials drop_Materials;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        drop_Materials = GetComponent<Drop_Materials>();
    }

    private void Start()
    {
        if (healthSystem == null)
        {
            Debug.LogError($"{gameObject.name}: Missing HealthSystem component!");
            return;
        }

        healthSystem.OnHealthChanged += OnEnemyHealthChanged;
        healthSystem.Initialize(maxHealth);
        xP_System = GameObject.FindGameObjectWithTag("Player")?.GetComponent<XP_System>();
    }

    public void TakeDamage(int damage)
    {
        if (damageable && !hit && healthSystem.CurrentHealth > 0)
        {
            hit = true;
            healthSystem.TakeDamage(damage, gameObject);
        }
    }

    private void OnEnemyHealthChanged(int currentHealth, int maxHealth)
    {
        if (healthSystem.CurrentHealth != currentHealth)
            return;

        Debug.Log($"{gameObject.name} current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} has died.");
            if (xP_System != null && stats != null)
                xP_System.DropXP(transform.position, stats.xpOnDeath);

            if (drop_Materials != null && stats != null)
                drop_Materials.DropMaterial(stats.dropA, stats.dropB, stats.dropC);

            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(TurnOffHit());
        }
    }

    private IEnumerator TurnOffHit()
    {
        yield return new WaitForSeconds(invincibilityDuration);
        hit = false;
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= OnEnemyHealthChanged;
    }
}
