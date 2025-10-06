using UnityEngine;
using System.Collections;

public class Melee : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;
    [SerializeField] private Player_Camera player_Camera;
    [SerializeField] private Transform attackOrigin;

    [Header("Attack Settings")]
    public float attackRadius = 1f;
    public int damage;
    public LayerMask enemyLayer;
    public float cooldownTime = 0.5f;
    private float cooldownTimer = 0f;

    [Header("Knockback Settings")]
    public float knockbackForce = 6f;
    public float knockbackDuration = 0.3f; // tempo do "stun"

    [Header("Visual Effect")]
    public GameObject meleeEffectPrefab;
    public float effectDuration = 0.3f;

    void Update()
    {
        if (cooldownTimer <= 0)
        {
            if (player_InputHandler.AttackTriggered)
            {
                PerformAttack();
                cooldownTimer = cooldownTime;
            }
        }
        else
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    void PerformAttack()
    {
        // Efeito visual
        if (meleeEffectPrefab != null)
        {
            GameObject effect = Instantiate(meleeEffectPrefab, attackOrigin.position, Quaternion.identity);
            effect.transform.SetParent(attackOrigin);

            if (transform.localScale.x < 0)
            {
                Vector3 scale = effect.transform.localScale;
                scale.x *= -1;
                effect.transform.localScale = scale;
            }

            Destroy(effect, effectDuration);
        }

        // Detetar inimigos atingidos
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            HealthSystem healthSystem = enemy.GetComponent<HealthSystem>();
            player_Camera.StartCameraShake();

            if (healthSystem != null)
            {
                healthSystem.TakeDamage(damage);
                Debug.Log("damage take");
            }

            ApplyKnockback(enemy.transform, enemy.GetComponent<Rigidbody2D>());
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }

    public void ApplyKnockback(Transform enemyTransform, Rigidbody2D enemyRigidbody)
    {
        if (enemyRigidbody == null) return;

        // Direção do knockback
        Vector2 direction = (enemyTransform.position - attackOrigin.position).normalized;

        // Resetar velocidade e aplicar força inicial
        enemyRigidbody.linearVelocity = Vector2.zero;
        enemyRigidbody.AddForce(direction * knockbackForce, ForceMode2D.Impulse);

        // Iniciar desaceleração suave
        StartCoroutine(SlowDownEnemy(enemyRigidbody));
    }

    private IEnumerator SlowDownEnemy(Rigidbody2D enemyRigidbody)
    {
        float timer = 0f;
        Vector2 startVelocity = enemyRigidbody.linearVelocity;

        while (timer < knockbackDuration)
        {
            // Interpola a velocidade até parar
            enemyRigidbody.linearVelocity = Vector2.Lerp(startVelocity, Vector2.zero, timer / knockbackDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Garante que parou completamente
        enemyRigidbody.linearVelocity = Vector2.zero;
    }
}
