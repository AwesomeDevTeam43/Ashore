using UnityEngine;

public class Enemy_Fly : MonoBehaviour
{
    [Header("Patrol Points")]
    public GameObject pointA;
    public GameObject pointB;
    private Rigidbody2D rb;
    private Transform currentPoint;
    public float speed;

    [Header("Enemy Settings")]
    public float health;
    private HealthSystem healthSystem;
    private XP_System xP_System;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.OnHealthChanged += OnHealthChanged;
    }
    void Start()
    {
        healthSystem.Initialize((int)health);
        rb = GetComponent<Rigidbody2D>();
        
        currentPoint = pointB.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 point = currentPoint.position - transform.position;
        if (currentPoint == pointB.transform)
        {
            rb.linearVelocity = new Vector2(speed, 0);
        }
        else
        {
            rb.linearVelocity = new Vector2(-speed, 0);
        }

        if (Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointB.transform)
        {
            FlipSprite();
            currentPoint = pointA.transform;
        }
        if (Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointA.transform)
        {
            FlipSprite();
            currentPoint = pointB.transform;
        }
    }

    void FlipSprite()
    {
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(pointA.transform.position, 1f);
        Gizmos.DrawWireSphere(pointB.transform.position, 1f);
        Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
    }
    void OnHealthChanged(int current, int max)
  {
    Debug.Log($"Enemy Health changed {current} {max}");
    if (current <= 0)
    {
      Destroy(gameObject);
      if (xP_System != null)
      {
        xP_System.DropXP(transform.position, 1);
      }
    }
  }
}
