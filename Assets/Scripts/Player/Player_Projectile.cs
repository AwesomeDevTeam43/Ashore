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

    private Vector2 moveDir = Vector2.right;
    private int currentDamage;

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
                Debug.Log("Enemy must take damage");
                var enemyHealth = hitInfo.collider.GetComponent<Enemy_Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(currentDamage, Enemy_Health.DamageSourceType.PlayerRanged);
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

    public void SetDamage(int damageAmount)
    {
        currentDamage = Mathf.Max(1, damageAmount);
    }

    void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
