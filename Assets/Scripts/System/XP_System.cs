using System;
using UnityEngine;

public class XP_System : MonoBehaviour
{
    private int _current_Xp;
    private int _Max_Xp_PerLevel;
    private int _Current_Level = 1;
    private int _levelGap;

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

    public int CurrentXp => _current_Xp;
    public int CurrentLevel => _Current_Level;
    public int MaxXpPerLevel => _Max_Xp_PerLevel;
    public float XpProgress => (float)_current_Xp / _Max_Xp_PerLevel;
}