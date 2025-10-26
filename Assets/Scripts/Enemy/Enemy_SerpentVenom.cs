using UnityEngine;

public class Enemy_SerpentVenom : MonoBehaviour
{
    // NOVO: Tornar o dano público para ser definido antes da criação
    [HideInInspector] public int damageAmount;

    public float speed = 10f;
    public float lifetime = 10f;
    private Rigidbody2D rb;
    // Opcional: Adicionar a referência ao inimigo pai para obter stats se necessário
    // public VenomShooting parentShooter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;

            // CORREÇÃO CRÍTICA: Use 'velocity' e não 'linearVelocity'
            rb.linearVelocity = direction * speed;

            float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        Destroy(gameObject, lifetime);
    }

    // ... OnTriggerEnter2D ...
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HealthSystem healthSystem = collision.GetComponent<HealthSystem>() ?? collision.GetComponentInParent<HealthSystem>();

            if (healthSystem != null)
            {
                // NOVO: Usar damageAmount
                healthSystem.TakeDamage(damageAmount);
            }
            Destroy(gameObject);
        }
        else if (collision.CompareTag("MovingPlatform") || collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}