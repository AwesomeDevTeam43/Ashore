using UnityEngine;
using UnityEngine.Serialization;

public class Projectile : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 10f;
    public float lifeTime = 3f;
    public float distance = 0.5f;
    public LayerMask whatIsSolid;

    [Header("Damage")]
    [FormerlySerializedAs("damage")]
    [SerializeField] private int baseDamage = 5;
    [SerializeField] private float criticalMultiplier = 1.5f;

    private Vector2 moveDir = Vector2.right;
    private int currentDamage;
    private bool isCritical = false;

    private void Start()
    {
        Invoke("DestroyProjectile", lifeTime);
        if (currentDamage <= 0)
        {
            currentDamage = Mathf.Max(1, baseDamage);
        }
    }

    private void Update()
    {
        RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, moveDir, distance, whatIsSolid);
        if (hitInfo.collider != null)
        {
            if (hitInfo.collider.CompareTag("Enemy"))
            {
                var enemyHealth = hitInfo.collider.GetComponent<Enemy_Health>();
                if (enemyHealth != null)
                {
                    Vector2 hitPoint = hitInfo.point;
                    enemyHealth.TakeDamage(currentDamage, Enemy_Health.DamageSourceType.PlayerRanged, isCritical, hitPoint);
                }
            }
            DestroyProjectile();
        }
        transform.Translate(moveDir * speed * Time.deltaTime);
    }

    public void SetDirection(Vector2 dir)
    {
        moveDir = dir.normalized;
    }

    public void SetDamage(int damageAmount, bool critical = false)
    {
        isCritical = critical;
        currentDamage = Mathf.Max(1, damageAmount);
        if (isCritical)
        {
            currentDamage = Mathf.RoundToInt(currentDamage * criticalMultiplier);
        }
    }

    void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
