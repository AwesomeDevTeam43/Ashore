using System.Collections;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    // SIMPLE CARDINAL MELEE: Each attack samples raw movement and snaps to nearest axis.
    // Diagonals collapse to whichever axis has larger magnitude (tie -> vertical).

    [Header("Damage & Range")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.3f;

    [Header("Effect Prefab")]
    [SerializeField] private GameObject meleeEffectPrefab; // Must have SpriteRenderer
    [SerializeField] private float effectLifetime = 0.25f;
    [SerializeField] private Transform attackOrigin;

    [Header("Offsets (Facing Right)")]
    [SerializeField] private Vector2 offsetRight = new Vector2(0.6f, 0.1f);
    [SerializeField] private Vector2 offsetLeft = new Vector2(-0.6f, 0.1f);
    [SerializeField] private Vector2 offsetUp = new Vector2(0f, 0.9f);
    [SerializeField] private Vector2 offsetDown = new Vector2(0f, -0.2f);

    [Header("Input Thresholds")]
    [Tooltip("Absolute axis value required to consider that axis active.")]
    [Range(0f,1f)] [SerializeField] private float axisThreshold = 0.5f;

    private Player_Movement playerMovement;
    private Player_Camera playerCamera;
    private Player_InputHandler inputHandler;
    private Animator animator;

    public enum MeleeDir { Right, Left, Up, Down }
    [Header("Current Cardinal (debug)")] [SerializeField]
    private MeleeDir currentDir = MeleeDir.Right;

    private void Start()
    {
        playerMovement = GetComponentInParent<Player_Movement>();
        playerCamera = GetComponentInParent<Player_Camera>();
        inputHandler = GetComponentInParent<Player_InputHandler>();
        animator = GetComponentInParent<Animator>();
    }

    public void PerformAttack()
    {
        // Sample input once per attack and resolve to cardinal.
        ResolveFromInput();
        Attack_Animation();
    }

    public void Attack()
    {
        SpawnEffect();
        DamageEnemies();
    }

    private void ResolveFromInput()
    {
        Vector2 raw = inputHandler != null ? inputHandler.MovementInput : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool facingLeft = playerMovement != null && playerMovement.IsFacingLeft;
        Vector2 dir = CardinalDirectionResolver.Resolve(raw, axisThreshold, facingLeft);
        // Convert to enum
        if (dir == Vector2.up) currentDir = MeleeDir.Up;
        else if (dir == Vector2.down)
        {
            if (playerMovement != null && playerMovement.IsGrounded())
            {
                currentDir = facingLeft ? MeleeDir.Left : MeleeDir.Right; // block downward when grounded
            }
            else currentDir = MeleeDir.Down;
        }
        else if (dir == Vector2.left) currentDir = MeleeDir.Left;
        else currentDir = MeleeDir.Right;
    }

    private void SpawnEffect()
    {
        if (meleeEffectPrefab == null) return;
        // Base position: player root (not damage origin) for independence
        Vector3 worldPos = transform.position + ResolveWorldOffset();
        GameObject effect = Instantiate(meleeEffectPrefab, worldPos, Quaternion.identity);
        var sr = effect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = currentDir == MeleeDir.Left;
        }
        float rotZ = ResolveRotationZ();
        effect.transform.rotation = Quaternion.Euler(0,0,rotZ);
        Destroy(effect, effectLifetime);
    }

    private Vector3 ResolveWorldOffset()
    {
        return currentDir switch
        {
            MeleeDir.Right => (Vector3)offsetRight,
            MeleeDir.Left  => (Vector3)offsetLeft,
            MeleeDir.Up    => (Vector3)offsetUp,
            MeleeDir.Down  => (Vector3)offsetDown,
            _ => Vector3.zero
        };
    }

    private float ResolveRotationZ()
    {
        return currentDir switch
        {
            MeleeDir.Up => 90f,
            MeleeDir.Down => -90f,
            _ => 0f
        };
    }

    private void DamageEnemies()
    {
        if (attackOrigin == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyLayer);
        foreach (var c in hits)
        {
            var hp = c.GetComponent<Enemy_Health>();
            if (hp != null) hp.TakeDamage(damage);
            var rb = c.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                StartCoroutine(ApplyKnockback(rb, c.transform));
            }
        }
        if (playerCamera != null) playerCamera.StartCameraShake();
    }

    private IEnumerator ApplyKnockback(Rigidbody2D enemyRb, Transform enemyTf)
    {
        if (enemyRb == null) yield break;
        Vector2 dir = (enemyTf.position - attackOrigin.position).normalized;
        enemyRb.linearVelocity = Vector2.zero;
        enemyRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        float t = 0f; Vector2 startVel = enemyRb.linearVelocity;
        while (t < knockbackDuration && enemyRb != null)
        {
            enemyRb.linearVelocity = Vector2.Lerp(startVel, Vector2.zero, t / knockbackDuration);
            t += Time.deltaTime; yield return null;
        }
        if (enemyRb != null) enemyRb.linearVelocity = Vector2.zero;
    }

    private void Attack_Animation()
    {
        if (animator != null) animator.SetTrigger("Meele Attack");
    }

    private void OnDrawGizmos()
    {
        if (attackOrigin == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }
}
