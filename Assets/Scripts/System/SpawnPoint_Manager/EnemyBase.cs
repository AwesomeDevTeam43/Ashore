using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Assigned by SpawnManager (optional)")]
    public Enemy_Stats stats;

    public virtual void SetStats(Enemy_Stats s)
    {
        if (s == null) return;
        var enemyHealth = GetComponent<Enemy_Health>();
        if (enemyHealth != null)
        {
            enemyHealth.Initialize(s.maxHealth, s.xpOnDeath, s.woodDrop, s.stoneDrop, s.ropeDrop);
        }
        transform.localScale = s.baseScale;
    }
}