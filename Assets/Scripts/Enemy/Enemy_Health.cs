using System.Collections;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    [SerializeField]
    private bool damageable = true;
    [SerializeField] 
    private int maxHealth = 100;
    [SerializeField]
    private float invincibilityDuration = .2f;
    private bool hit;
    private int currentHealth;
    private HealthSystem healthSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.Initialize(maxHealth);
    }
    
    public void TakeDamage(int damage)
    {
        if(damageable && !hit && currentHealth > 0)
        {
            hit = true;
            currentHealth -= damage;
            healthSystem.TakeDamage(damage, gameObject);
            if (currentHealth <= 0)
            {
                Debug.Log($"{gameObject.name} died.");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"{gameObject.name} took {damage} damage. Current health: {currentHealth}/{maxHealth}");
                StartCoroutine(TurnOffHit());
            }
        }
    }

    private IEnumerator TurnOffHit()
    {
        yield return new WaitForSeconds(invincibilityDuration);
        hit = false;
    }
}
