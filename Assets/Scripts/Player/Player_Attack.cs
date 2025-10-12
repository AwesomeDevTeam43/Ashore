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
            // This is needed for the ground check.
            Player_Movement player_Movement = GetComponent<Player_Movement>();


            // 1. Determine attack direction
            Vector2 attackDirection = Vector2.right;
            Vector2 inputDir = player_InputHandler.MovementInput;

            if (inputDir != Vector2.zero)
            {
                if (Mathf.Abs(inputDir.y) >= Mathf.Abs(inputDir.x))
                {
                    attackDirection = inputDir.y > 0 ? Vector2.up : Vector2.down;
                }
                else
                {
                    attackDirection = inputDir.x > 0 ? Vector2.right : Vector2.left;
                }
            }
            else
            {
                attackDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            }

            // 2. Prevent attacking down while grounded
            if (attackDirection == Vector2.down && player_Movement != null && player_Movement.IsGrounded())
            {
                attackDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            }


            // 3. Instantiate and parent as before
            GameObject effect = Instantiate(meleeEffectPrefab, attackOrigin.position, Quaternion.identity);
            effect.transform.SetParent(attackOrigin);
            SpriteRenderer effectSprite = effect.GetComponent<SpriteRenderer>();


            // 4. Set rotation and flip based on direction
            if (attackDirection == Vector2.right)
            {
                // Default state, no changes needed.
            }
            else if (attackDirection == Vector2.left)
            {
                // To mirror symmetrically, we flip the sprite's rendering on the X-axis.
                if (effectSprite != null)
                {
                    effectSprite.flipX = true;
                }
            }
            else if (attackDirection == Vector2.up)
            {
                // THE FIX: Compensate for parent's flip when facing left.
                float angle = (transform.localScale.x > 0) ? 90 : -90;
                effect.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            else if (attackDirection == Vector2.down)
            {
                // THE FIX: Compensate for parent's flip when facing left.
                float angle = (transform.localScale.x > 0) ? -90 : 90;
                effect.transform.localRotation = Quaternion.Euler(0, 0, angle);
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
        if (enemyRigidbody == null) yield break;
        float timer = 0f;
        if (enemyRigidbody == null) yield break;
        Vector2 startVelocity = enemyRigidbody.linearVelocity;

        while (timer < knockbackDuration && enemyRigidbody != null)
        {
            // Interpola a velocidade até parar
            enemyRigidbody.linearVelocity = Vector2.Lerp(startVelocity, Vector2.zero, timer / knockbackDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Garante que parou completamente
        if (enemyRigidbody != null)
        enemyRigidbody.linearVelocity = Vector2.zero;
    }
}
