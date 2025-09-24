using UnityEngine;

public class Melee : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;
    
    public Transform attackOrigin;
    public float attackRadius = 1f; 
    public LayerMask enemyLayer;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }

    void Update()
    {
        if (player_InputHandler.AttackTriggered)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyLayer);
            foreach (var enemyLayer in hitEnemies)
            {
                Debug.Log("We hit " + enemyLayer.name);
            }
        }
    }
}
