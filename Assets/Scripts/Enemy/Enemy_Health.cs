using System.Collections;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    [SerializeField]
    private int maxHealth;
    [SerializeField]
    private bool damageable = true;
    [SerializeField]
    private float invincibilityDuration = .2f;
    private bool hit;
    private HealthSystem healthSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.OnHealthChanged += OnPlayerHealthChanged;
        healthSystem.Initialize(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damageable && !hit && healthSystem.CurrentHealth > 0)
        {
            hit = true;
            healthSystem.TakeDamage(damage, gameObject);
            OnPlayerHealthChanged(healthSystem.CurrentHealth, healthSystem.MaxHealth);
        }
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} has died.");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"{gameObject.name} took damage. Current health: {currentHealth}/{maxHealth}");
            StartCoroutine(TurnOffHit());
        }
    }

    private IEnumerator TurnOffHit()
    {
        yield return new WaitForSeconds(invincibilityDuration);
        hit = false;
    }
}
