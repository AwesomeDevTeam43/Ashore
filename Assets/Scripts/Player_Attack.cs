using UnityEngine;

public class Melee : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;

    public Transform attackOrigin;
    public float attackRadius = 1f;
    public int damage;
    public LayerMask enemyLayer;
    public float cooldownTime = .5f;
    private float cooldownTimer = 0f;

    [Header("Knockback Settings")]
    public float knockbackForce;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }

    void Update()
    {
        if (cooldownTimer <= 0)
        {
            if (player_InputHandler.AttackTriggered)
            {
                Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyLayer);
                for (int i = 0; i < hitEnemies.Length; i++)
                {
                    Enemy enemy = hitEnemies[i].GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage);

                        // knockback
                        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            Vector2 direcao = (enemy.transform.position - attackOrigin.position).normalized;
                            rb.linearVelocity = Vector2.zero;
                            rb.AddForce(direcao * knockbackForce, ForceMode2D.Impulse);
                        }
                    }
                }
                cooldownTimer = cooldownTime;
            }
        }
        else
        {
            cooldownTimer -= Time.deltaTime;
        }
    }
}
