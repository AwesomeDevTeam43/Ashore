using UnityEngine;

public class Boss : MonoBehaviour
{
    private HealthSystem bossHealth;

    private void Awake()
    {
        bossHealth = GetComponent<HealthSystem>();
        bossHealth.Initialize(35);
        bossHealth.OnHealthChanged += OnHealthChanged;

        anim = GetComponent<Animator>();
    }

    private bool isFlipped = false;
    [SerializeField] private Vector2 attackOffset;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask attackMask;
    [SerializeField] private int attackDamage = 2;
    Animator anim;



    public void LookAtPlayer(Transform player)
    {
        Vector3 flipped = transform.localScale;
        flipped.z *= -1f;

        if (transform.position.x > player.position.x && isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = false;
        }
        else if (transform.position.x < player.position.x && !isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = true;
        }
    }

    public void AttackPlayer()
    {
        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, LayerMask.GetMask("Player"));
        if (colInfo != null)
        {
            colInfo.GetComponent<HealthSystem>().TakeDamage(attackDamage);
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, attackRange);
    }



    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= maxHealth / 2)
        {
            Debug.Log("Boss phase 2");
            anim.SetBool("Phase2", true);
            attackDamage = 4;
        }
        if (currentHealth <= 0)
        {
            Debug.Log("Boss defeated");
            Destroy(gameObject);
        }
    }
}
