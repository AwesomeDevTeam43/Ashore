using System.Collections;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Damage & Range")]
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float criticalMultiplier = 1.5f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private float criticalKnockbackMultiplier = 1.5f;

    [Header("Effect Prefab")]
    [SerializeField] private GameObject meleeEffectPrefab;
    [SerializeField] private GameObject criticalEffectPrefab;
    [SerializeField] private float effectLifetime = 0.25f;
    [SerializeField] private Transform attackOrigin;

    [Header("Offsets (Facing Right)")]
    [SerializeField] private Vector2 offsetRight = new Vector2(0.6f, 0.1f);
    [SerializeField] private Vector2 offsetLeft = new Vector2(-0.6f, 0.1f);
    [SerializeField] private Vector2 offsetUp = new Vector2(0f, 0.9f);
    [SerializeField] private Vector2 offsetDown = new Vector2(0f, -0.2f);

    [Header("Attack Range Per Direction")]
    [SerializeField] private float rightRadius = 1f;
    [SerializeField] private float leftRadius = 1f;
    [SerializeField] private float upRadius = 0.8f;
    [SerializeField] private float downRadius = 1.2f;

    [Header("Input Thresholds")]
    [Tooltip("Absolute axis value required to consider that axis active.")]
    [Range(0f,1f)] [SerializeField] private float axisThreshold = 0.5f;

    private Player_Movement playerMovement;
    private Player_Camera playerCamera;
    private Player_InputHandler inputHandler;
    private Animator animator;
    private Player_Controller playerController;

    public enum MeleeDir { Right, Left, Up, Down }
    [Header("Current Cardinal (debug)")] 
    [SerializeField] private MeleeDir currentDir = MeleeDir.Right;
    
    private bool lastAttackWasCritical = false;
    private int enemiesHitThisAttack = 0;

    private void Start()
    {
        playerMovement = GetComponentInParent<Player_Movement>();
        playerCamera = GetComponentInParent<Player_Camera>();
        inputHandler = GetComponentInParent<Player_InputHandler>();
        animator = GetComponentInParent<Animator>();
        playerController = GetComponentInParent<Player_Controller>();

        if (playerController == null)
        {
            Debug.LogError("Player_Controller not found! MeleeWeapon needs it to calculate damage.");
        }
    }

    public void PerformAttack()
    {
        ResolveFromInput();
        
        float critChance = playerController != null ? playerController.CriticalChance : 10f;
        float roll = Random.Range(0f, 100f);
        lastAttackWasCritical = roll < critChance;
                
        enemiesHitThisAttack = 0;
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
        
        if (dir == Vector2.up) 
            currentDir = MeleeDir.Up;
        else if (dir == Vector2.down)
        {
            if (playerMovement != null && playerMovement.IsGrounded())
            {
                currentDir = facingLeft ? MeleeDir.Left : MeleeDir.Right;
            }
            else 
                currentDir = MeleeDir.Down;
        }
        else if (dir == Vector2.left) 
            currentDir = MeleeDir.Left;
        else 
            currentDir = MeleeDir.Right;
    }

    private void SpawnEffect()
    {
        GameObject prefabToUse = lastAttackWasCritical && criticalEffectPrefab != null 
            ? criticalEffectPrefab 
            : meleeEffectPrefab;
            
        if (prefabToUse == null) return;
        
        Vector3 worldPos = transform.position + ResolveWorldOffset();
        GameObject effect = Instantiate(prefabToUse, worldPos, Quaternion.identity);
        
        var sr = effect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = currentDir == MeleeDir.Left;
            
            if (lastAttackWasCritical)
            {
                sr.color = Color.yellow;
                effect.transform.localScale *= 1.2f;
            }
        }
        
        float rotZ = ResolveRotationZ();
        effect.transform.rotation = Quaternion.Euler(0, 0, rotZ);
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

    private float GetCurrentAttackRadius()
    {
        return currentDir switch
        {
            MeleeDir.Right => rightRadius,
            MeleeDir.Left => leftRadius,
            MeleeDir.Up => upRadius,
            MeleeDir.Down => downRadius,
            _ => attackRadius
        };
    }

    private void DamageEnemies()
    {
        if (attackOrigin == null) return;
        
        float currentRadius = GetCurrentAttackRadius();
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin.position, currentRadius, enemyLayer);
        
        System.Array.Sort(hits, (a, b) => 
        {
            float distA = Vector2.Distance(attackOrigin.position, a.transform.position);
            float distB = Vector2.Distance(attackOrigin.position, b.transform.position);
            return distA.CompareTo(distB);
        });

        foreach (var c in hits)
        {
            var hp = c.GetComponent<Enemy_Health>();
            if (hp != null)
            {
                int damage = CalculateDamage();
                hp.TakeDamage(damage, Enemy_Health.DamageSourceType.PlayerMelee);
                enemiesHitThisAttack++;
            }
            
            var rb = c.GetComponent<Rigidbody2D>();
            
            // CORREÇÃO: Só aplicar knockback se for Dynamic
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                float knockback = lastAttackWasCritical ? knockbackForce * criticalKnockbackMultiplier : knockbackForce;
                StartCoroutine(ApplyKnockback(rb, c.transform, knockback));
            }
        }

        if (enemiesHitThisAttack > 0 && playerCamera != null) 
        {
            playerCamera.StartCameraShake();
        }
    }

    private int CalculateDamage()
    {
        int baseDamage = playerController != null ? playerController.AttackPower : 1;
        
        if (lastAttackWasCritical)
        {
            return Mathf.RoundToInt(baseDamage * criticalMultiplier);
        }
        
        return baseDamage;
    }

    private IEnumerator ApplyKnockback(Rigidbody2D enemyRb, Transform enemyTf, float force)
    {
        if (enemyRb == null) yield break;
        
        // SEGURANÇA EXTRA: Verificar novamente se é Dynamic
        if (enemyRb.bodyType != RigidbodyType2D.Dynamic)
        {
            yield break;
        }
        
        Vector2 dir = (enemyTf.position - attackOrigin.position).normalized;
        enemyRb.linearVelocity = Vector2.zero;
        enemyRb.AddForce(dir * force, ForceMode2D.Impulse);
        
        float t = 0f; 
        Vector2 startVel = enemyRb.linearVelocity;
        
        while (t < knockbackDuration && enemyRb != null)
        {
            // Verificar se ainda é Dynamic (pode ter mudado durante coroutine)
            if (enemyRb.bodyType != RigidbodyType2D.Dynamic)
                yield break;
            
            enemyRb.linearVelocity = Vector2.Lerp(startVel, Vector2.zero, t / knockbackDuration);
            t += Time.deltaTime; 
            yield return null;
        }
        
        if (enemyRb != null && enemyRb.bodyType == RigidbodyType2D.Dynamic) 
            enemyRb.linearVelocity = Vector2.zero;
    }

    private void Attack_Animation()
    {
        if (animator != null) 
        {
            animator.SetTrigger("Meele Attack");
        }
    }

    private void OnDrawGizmos()
    {
        if (attackOrigin == null) return;
        
        Gizmos.color = lastAttackWasCritical ? Color.yellow : Color.red;
        
        float radius = GetCurrentAttackRadius();
        Gizmos.DrawWireSphere(attackOrigin.position, radius);
        
        Vector3 dirVector = currentDir switch
        {
            MeleeDir.Right => Vector3.right,
            MeleeDir.Left => Vector3.left,
            MeleeDir.Up => Vector3.up,
            MeleeDir.Down => Vector3.down,
            _ => Vector3.right
        };
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(attackOrigin.position, dirVector * radius);
    }
}
