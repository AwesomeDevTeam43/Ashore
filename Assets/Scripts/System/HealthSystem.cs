using UnityEngine;
using System;
public class HealthSystem : MonoBehaviour
{
    private int _maxHealth;
    private int _currentHealth;

    public event Action<int, int> OnHealthChanged;
    public event Action<GameObject> OnDamageTaken;

    public void Initialize(int maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage, GameObject damageSource = null)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnDamageTaken?.Invoke(damageSource);

        Debug.Log($"Damage taken: {damage} by {gameObject.name}. Current health: {_currentHealth}/{_maxHealth}");
    }

    public void Heal(int heal)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + heal);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public int CurrentHealth => _currentHealth;
    public int MaxHealth
    {
      get => _maxHealth;
      set => _maxHealth = value;
    }
    public bool IsAlive => _currentHealth > 0;
}
