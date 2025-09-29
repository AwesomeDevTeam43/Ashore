using UnityEngine;

public class Melee : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;

    [SerializeField] private Player_Camera player_Camera;

    public Transform attackOrigin;
    public float attackRadius = 1f;
    public int damage;
    public LayerMask enemyLayer;
    public float cooldownTime = 0.5f;
    private float cooldownTimer = 0f;

    [Header("Knockback Settings")]
    public float knockbackForce = 6f;

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
                        player_Camera.StartCameraShake();
                        
                        enemy.TakeDamage(damage);
                        ApplyKnockback(hitEnemies[i].transform, hitEnemies[i].GetComponent<Rigidbody2D>());
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

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }

    public void ApplyKnockback(Transform enemyTransform, Rigidbody2D enemyRigidbody)
    {
        Vector2 direction = (enemyTransform.position - attackOrigin.position).normalized;
        enemyRigidbody.linearVelocity = Vector2.zero; // Reset current velocity
        enemyRigidbody.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }
}
