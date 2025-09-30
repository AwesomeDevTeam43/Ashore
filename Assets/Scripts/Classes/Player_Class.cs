using UnityEngine;

public class Player_Class : MonoBehaviour
{
    public int Health { get; set; }
    public int Attack_Power { get; set; }

    public int Current_Level { get; set; }
    public int Max_1stLevel { get; set; }
    public int Current_Xp { get; set; }
    public int Max_Xp_PerLevel { get; set; } // aumenta conforme o Current_Level

    public Player_Class(int _Health, int _Attack_Power, int _Current_Level, int _Max_Level, int _Current_Xp, int _Max_Xp_PerLevel)
    {
        Health = _Health;
        Attack_Power = _Attack_Power;
        Current_Level = _Current_Level;
        Max_1stLevel = _Max_Level;
        Current_Xp = _Current_Xp;
        Max_Xp_PerLevel = _Max_Xp_PerLevel;
    }
}
