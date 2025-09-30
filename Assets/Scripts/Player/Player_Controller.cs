using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  [SerializeField] private HealthSystem healthSystem;
  [SerializeField] private XP_System xP_System;

  private int playerHealth = 3;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnPlayerHealthChanged;
  }

  private void OnDisable()
  {
    healthSystem.OnHealthChanged -= OnPlayerHealthChanged;
  }


  private void Start()
  {
    healthSystem.Initialize(playerHealth);
    xP_System.Initialize(10);
  }

  private void Update()
  {
      Debug.Log($"{healthSystem.MaxHealth}");
    if (Input.GetKeyDown(KeyCode.M))
    {
      healthSystem.MaxHealth++;
      Debug.Log($"{healthSystem.MaxHealth}");
    }
  }

  private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
  {

  }
}

