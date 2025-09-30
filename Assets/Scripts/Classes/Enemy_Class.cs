using UnityEngine;


public class Enemy_Class : ScriptableObject
{
    public readonly int Id;
    public int Health { get; set; }
    public int Attack_Power { get; set; }
    public float Move_Speed { get; set; }
    //public float Attack_Range { get; set; }

    public int Xp_Reward { get; set; }
    //public int Material_Rewards { get; set; }

    //public Transform SpawnPoint { get; set; } 

    public Enemy_Class(int _Id, int _Health, int _Attack_Power, float _Move_Speed, int _Xp_Reward)
    {
        Id = _Id;
        Health = _Health;
        Attack_Power = _Attack_Power;
        Move_Speed = _Move_Speed;
        Xp_Reward = _Xp_Reward;
    }
}
