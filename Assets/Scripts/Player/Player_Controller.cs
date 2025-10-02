using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  private HealthSystem healthSystem;
  private XP_System xP_System;
  private Player_Atributes player_Atributes;

  private void Awake()
  {
    player_Atributes = GetComponent<Player_Atributes>();

    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnPlayerHealthChanged;

    healthSystem.Initialize(player_Atributes.Health);

    xP_System = GetComponent<XP_System>();
    xP_System.Initialize(player_Atributes.LVL1XpAmount, player_Atributes.LvlGap);
  }

  private void OnDisable()
  {
    healthSystem.OnHealthChanged -= OnPlayerHealthChanged;
  }
  
  private void Update()
  {

    if (Input.GetKeyDown(KeyCode.M))
    {
      healthSystem.Heal(1);
      Debug.Log($"{healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
    }
  }

  private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
  {
    if (currentHealth <= 0)
    {
      Destroy(gameObject);
      Debug.Log("Player Died!");
    }
  }
}

