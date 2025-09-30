using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  [Header("Starting Stats")]
  [SerializeField] private int Health = 5;
  //[SerializeField] private int startingAttackPower = 1;
  //[SerializeField] private int maxLevel = 20;
  [SerializeField] private int baseXpOn1stLVL = 10;

  private HealthSystem healthSystem;
  private XP_System xP_System;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
    healthSystem.OnHealthChanged += OnPlayerHealthChanged;
    xP_System = GetComponent<XP_System>();
  }

  private void OnDisable()
  {
    healthSystem.OnHealthChanged -= OnPlayerHealthChanged;
  }

  private void Start()
  {
    healthSystem.Initialize(Health);
    xP_System.Initialize(baseXpOn1stLVL);
  }

  private void Update()
  {

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

