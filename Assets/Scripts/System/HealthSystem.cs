using UnityEngine;
using System;
public class HealthSystem : MonoBehaviour
{
    private float _maxHealth;
    private float _currentHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action<GameObject> OnDamageTaken;

    private void Update()
    {
      Debug.Log($"{_maxHealth}, {_currentHealth}");
    }

    public void Initialize(float maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage, GameObject damageSource = null)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnDamageTaken?.Invoke(damageSource);

        Debug.Log($"Damage taken: {damage} by {gameObject.name}. Current health: {_currentHealth}/{_maxHealth}");
    }

    public void Heal(float heal)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + heal);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public float CurrentHealth => _currentHealth;
    public float MaxHealth
    {
      get => _maxHealth;
      set => _maxHealth = value;

    }
    public bool IsAlive => _currentHealth <= 0;
}
