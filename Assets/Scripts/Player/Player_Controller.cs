using UnityEngine;

public class Player_Controller : MonoBehaviour
{
  HealthSystem healthSystem;

  private int playerHealth = 3;

  private void Awake()
  {
    healthSystem = GetComponent<HealthSystem>();
  }

  private void Start()
  {
    healthSystem.Initialize(playerHealth);


  }

  private void Update()
  {
    Debug.Log($"{healthSystem.MaxHealth}");
    if (Input.GetKeyDown(KeyCode.M))
    {
      healthSystem.MaxHealth++;
    }
  }
}
