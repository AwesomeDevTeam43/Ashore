using UnityEngine;

public class Melee : MonoBehaviour
{
    public Transform attackOrigin;
    public float attackRadius = 1f; 
    public LayerMask enemyLayer;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyLayer);
            foreach (var enemyLayer in hitEnemies)
            {
                Debug.Log("We hit " + enemyLayer.name);
            }
        }
    }
}
