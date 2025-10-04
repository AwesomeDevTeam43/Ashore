using System;
using UnityEngine;

public class XP_System : MonoBehaviour
{
    private int _current_Xp;
    private int _Max_Xp_PerLevel;
    private int _Current_Level = 1;
    private int _levelGap;

    [SerializeField] private GameObject _xp_Particle;

    public event Action<int> OnCollectXP;
    public event Action<int> OnLevelUp;

    public void Initialize(int _max_Xp_1stLVL, int levelGap)
    {
        _Max_Xp_PerLevel = _max_Xp_1stLVL;
        _current_Xp = 0;
        _Current_Level = 1;
        _levelGap = levelGap;
    }

    public void IncreaseXP(int xpAmount)
    {
        _current_Xp += xpAmount;
        OnCollectXP?.Invoke(xpAmount);
        LevelUp();

        Debug.Log($"XP collected: {xpAmount}, Current XP : {_current_Xp}");
    }

    private void LevelUp()
    {
        if (_current_Xp >= _Max_Xp_PerLevel)
        {
            int xpExceed = _current_Xp - _Max_Xp_PerLevel;
            _current_Xp = xpExceed;
            _Current_Level += 1;
            _Max_Xp_PerLevel += _levelGap;

            OnLevelUp?.Invoke(_Current_Level);

            Debug.Log($"Level Up to {_Current_Level}, Current XP : {_current_Xp}");

            LevelUp();
        }
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
            // Cria um offset aleatório para espalhar as partículas (2D)
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f // Z sempre 0 para 2D
            );

            Vector3 spawnPosition = position + randomOffset;

            // Instancia a partícula de XP
            GameObject xpParticle = Instantiate(_xp_Particle, spawnPosition, Quaternion.identity);

            // Opcional: Adiciona uma força inicial para criar um efeito de "explosão" (2D)
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
    public float XpProgress => (float)_current_Xp / _Max_Xp_PerLevel;
}