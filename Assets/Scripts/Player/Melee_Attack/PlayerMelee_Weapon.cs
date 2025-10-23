using System.Collections;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.3f;

    [Header("Visual Effect")]
    [SerializeField] private GameObject meleeEffectPrefab;
    [SerializeField] private float effectDuration = 0.3f;
    [SerializeField] private Transform attackOrigin;

    private Rigidbody2D rb;
    private Player_Movement playerMovement;
    private Player_MeleeManager playerMelee;
    private Player_Camera playerCamera;

    private Vector2 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponentInParent<Player_Movement>();
        playerMelee = GetComponentInParent<Player_MeleeManager>();
        playerCamera = GetComponentInParent<Player_Camera>();
    }

    public void PerformAttack()
    {
        DetermineAttackDirection();
        SpawnMeleeEffect(direction);
        DetectAndDamageEnemies();
    }

    // --- Determinar direção do ataque ---
    private void DetermineAttackDirection()
    {
        Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (inputDir == Vector2.zero)
        {
            direction = playerMovement.IsFacingLeft ? Vector2.left : Vector2.right;
        }
        else
        {
            if (Mathf.Abs(inputDir.y) >= Mathf.Abs(inputDir.x))
                direction = inputDir.y > 0 ? Vector2.up : Vector2.down;
            else
                direction = inputDir.x > 0 ? Vector2.right : Vector2.left;
        }

        if (direction == Vector2.down && playerMovement.IsGrounded())
            direction = playerMovement.IsFacingLeft ? Vector2.left : Vector2.right;
    }

    private void DetectAndDamageEnemies()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            Enemy_Health enemyHealth = enemy.GetComponent<Enemy_Health>();
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();

            if (enemyHealth != null)
                enemyHealth.TakeDamage(damage);

            if (enemyRb != null)
                StartCoroutine(ApplyKnockback(enemyRb, enemy.transform));

            if (playerCamera != null)
                playerCamera.StartCameraShake();
        }
    }

    private IEnumerator ApplyKnockback(Rigidbody2D enemyRb, Transform enemyTransform)
    {
        if (enemyRb == null) yield break;

        Vector2 dir = (enemyTransform.position - attackOrigin.position).normalized;
        enemyRb.linearVelocity = Vector2.zero;
        enemyRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);

        float t = 0f;
        Vector2 startVel = enemyRb.linearVelocity;

        while (t < knockbackDuration && enemyRb != null)
        {
            enemyRb.linearVelocity = Vector2.Lerp(startVel, Vector2.zero, t / knockbackDuration);
            t += Time.deltaTime;
            yield return null;
        }

        if (enemyRb != null)
            enemyRb.linearVelocity = Vector2.zero;
    }

    private void SpawnMeleeEffect(Vector2 attackDirection)
    {
        if (meleeEffectPrefab == null || attackOrigin == null)
            return;

        GameObject effect = Instantiate(meleeEffectPrefab, attackOrigin.position, Quaternion.identity);
        effect.transform.SetParent(attackOrigin);

        SpriteRenderer sprite = effect.GetComponent<SpriteRenderer>();

        if (attackDirection == Vector2.right)
        {
            effect.transform.localRotation = Quaternion.identity;
            if (sprite != null) sprite.flipX = false;
        }
        else if (attackDirection == Vector2.left)
        {
            effect.transform.localRotation = Quaternion.identity;
            if (sprite != null) sprite.flipX = true;
        }
        else if (attackDirection == Vector2.up)
        {
            float angle = playerMovement.IsFacingLeft ? -90 : 90;
            effect.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
        else if (attackDirection == Vector2.down)
        {
            float angle = playerMovement.IsFacingLeft ? 90 : -90;
            effect.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        Destroy(effect, effectDuration);
    }

    private void OnDrawGizmos()
    {
        if (attackOrigin == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }
}
