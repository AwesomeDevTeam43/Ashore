using UnityEngine;

public class Enemy : MonoBehaviour
{

    public int health;
    public float speed;
    //public GameObject bloodEffect;

    HealthSystem healthSystem;

    private Rigidbody2D rb;

    void Awake()
    {
      healthSystem = GetComponent<HealthSystem>();
      healthSystem.OnHealthChanged += OnHealthChanged;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      healthSystem.Initialize(health);
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }
    
  void OnHealthChanged(float current, float max)
  {
    Debug.Log($"Health changed {current} {max}");
    if (current <= 0)
    {
      Destroy(gameObject);
    }
  }
}
