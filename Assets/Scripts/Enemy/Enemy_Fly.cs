using UnityEngine;

public class Enemy_Fly : MonoBehaviour
{
    [Header("Patrol Points")]
    public GameObject pointFinal;
    public GameObject pointInitial;
    private Rigidbody2D rb;
    private Transform currentPoint;
    public float speed;

    [Header("Enemy Settings")]
    public int health;
    private HealthSystem healthSystem;
    private XP_System xP_System;
    public GameObject player;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.Initialize(health);
        healthSystem.OnHealthChanged += OnHealthChanged;
    }

    void Start()
    {
        
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // garantir que não desce
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // não rodar
        player = GameObject.FindGameObjectWithTag("Player");
        xP_System = player.GetComponent<XP_System>();

        currentPoint = pointInitial.transform;
    }

    void Update()
    {
        // Direção entre a posição atual e o ponto de destino
        Vector2 direction = (currentPoint.position - transform.position).normalized;

        // Movimento na direção certa
        rb.linearVelocity = direction * speed;

        // Verifica se chegou perto do ponto
        if (Vector2.Distance(transform.position, currentPoint.position) < 0.5f)
        {
            FlipSprite();

            // Troca de ponto
            currentPoint = (currentPoint == pointInitial.transform) ? pointFinal.transform : pointInitial.transform;
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
        if (pointFinal != null && pointInitial != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pointFinal.transform.position, 1f);
            Gizmos.DrawWireSphere(pointInitial.transform.position, 1f);
            Gizmos.DrawLine(pointFinal.transform.position, pointInitial.transform.position);
        }
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
