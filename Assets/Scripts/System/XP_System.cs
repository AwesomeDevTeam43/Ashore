using System;
using UnityEngine;

public class XP_System : MonoBehaviour
{
    private int _current_Xp;
    private int _Max_Xp_PerLevel;
    private int _Current_Level = 1;
    private int _baseXpRequirement;
    private float _xpGrowthMultiplier = 1f;

    [SerializeField] private GameObject _xp_Particle;

    public event Action<int> OnCollectXP;
    public event Action<int> OnLevelUp;

    public void Initialize(int baseXpRequirement, float xpGrowthMultiplier)
    {
        _baseXpRequirement = Mathf.Max(1, baseXpRequirement);
        _xpGrowthMultiplier = Mathf.Max(1f, xpGrowthMultiplier);
        _current_Xp = 0;
        _Current_Level = 1;
        _Max_Xp_PerLevel = CalculateXpForLevel(_Current_Level);
    }

    public void Initialize(int level, int currentXp, int maxXp, int baseXpRequirement, float xpGrowthMultiplier)
    {
        _baseXpRequirement = Mathf.Max(1, baseXpRequirement);
        _xpGrowthMultiplier = Mathf.Max(1f, xpGrowthMultiplier);
        _Current_Level = Mathf.Max(1, level);
        _current_Xp = Mathf.Max(0, currentXp);
        _Max_Xp_PerLevel = maxXp > 0 ? maxXp : CalculateXpForLevel(_Current_Level);
    }

    public void IncreaseXP(int xpAmount)
    {
        _current_Xp += xpAmount;
        OnCollectXP?.Invoke(xpAmount);
        LevelUp();

        Debug.Log($"XP collected: {xpAmount}, Current XP : {_current_Xp}");
    }

    public void SetLevel(int level)
    {
        _Current_Level = Mathf.Max(1, level);
        _Max_Xp_PerLevel = CalculateXpForLevel(_Current_Level);
        OnLevelUp?.Invoke(_Current_Level);
    }

    public void SetXP(int xp)
    {
        _current_Xp = xp;
    }

    private void LevelUp()
    {
        while (_Max_Xp_PerLevel > 0 && _current_Xp >= _Max_Xp_PerLevel)
        {
            _current_Xp -= _Max_Xp_PerLevel;
            _Current_Level += 1;
            _Max_Xp_PerLevel = CalculateXpForLevel(_Current_Level);

            OnLevelUp?.Invoke(_Current_Level);
            Debug.Log($"Level Up to {_Current_Level}, Current XP : {_current_Xp}, Next Level Requires {_Max_Xp_PerLevel}");
        }
    }

    private int CalculateXpForLevel(int level)
    {
        int normalizedLevel = Mathf.Max(1, level);
        float exponent = Mathf.Max(0, normalizedLevel - 1);
        float scaled = _baseXpRequirement * Mathf.Pow(_xpGrowthMultiplier, exponent);
        return Mathf.Max(1, Mathf.CeilToInt(scaled));
    }

    public void DropXP(Vector3 position, int particlesAmount)
    {
        if (_xp_Particle == null)
        {
            Debug.LogWarning("XP Particle prefab is not assigned!");
            return;
        }

        for (int i = 0; i < particlesAmount; i++)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            );

            Vector3 spawnPosition = position + randomOffset;

            GameObject xpParticle = Instantiate(_xp_Particle, spawnPosition, Quaternion.identity);

            Rigidbody2D rb = xpParticle.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomForce = new Vector2(
                    UnityEngine.Random.Range(-3f, 3f),
                    UnityEngine.Random.Range(2f, 5f)
                );
                rb.AddForce(randomForce, ForceMode2D.Impulse);
            }
        }

        Debug.Log($"Dropped {particlesAmount} XP particles at {position}");
    }


    public int CurrentXp => _current_Xp;
    public int CurrentLevel => _Current_Level;
    public int MaxXpPerLevel => _Max_Xp_PerLevel;
    public float XpProgress => _Max_Xp_PerLevel > 0 ? (float)_current_Xp / _Max_Xp_PerLevel : 0f;
}