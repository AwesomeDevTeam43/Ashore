using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    private int _maxHealth;
    private int _currentHealth;

    private Player_Camera player_Camera;

    public event Action<int, int> OnHealthChanged;
    public event Action<GameObject> OnDamageTaken;

    public void Initialize(int maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = _maxHealth;

        player_Camera = GetComponent<Player_Camera>();

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void SetHealth(int health)
    {
        _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    // Optional minAllowedHealth: if provided, the resulting health after applying damage
    // will not be reduced below that value. This allows callers to clamp damage so
    // the target stops at defined thresholds. Default behavior (no clamp) remains unchanged.
    public void TakeDamage(int damage, GameObject damageSource = null, int minAllowedHealth = int.MinValue)
    {
        int old = _currentHealth;
        int candidate = _currentHealth - damage;
        if (minAllowedHealth != int.MinValue)
        {
            candidate = Mathf.Max(minAllowedHealth, candidate);
        }
        _currentHealth = Mathf.Clamp(candidate, 0, _maxHealth);

        int actualDamage = old - _currentHealth;
        Debug.Log($"Damage taken: {actualDamage} by {gameObject.name}. Current health: {_currentHealth}/{_maxHealth}");

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnDamageTaken?.Invoke(damageSource);
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