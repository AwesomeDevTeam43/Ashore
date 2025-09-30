using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  [SerializeField] private HealthSystem healthSystem;
  [SerializeField] private XP_System xP_System;

  private int playerHealth = 3;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
  }

  private void Start()
  {
    healthSystem.Initialize(playerHealth);
    xP_System.Initialize(10);
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.M))
    {
      healthSystem.MaxHealth++;
      Debug.Log($"{healthSystem.MaxHealth}");
    }
  }
}
