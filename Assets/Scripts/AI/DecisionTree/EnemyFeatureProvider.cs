using UnityEngine;

namespace Ashore.AI.DecisionTree
{
    // Default feature provider for enemies: distances, LOS, health ratios, cooldowns
    public class EnemyFeatureProvider : MonoBehaviour, IFeatureProvider
    {
        [SerializeField] private Transform player;
        [SerializeField] private LayerMask obstacleMask; // for LOS
        [SerializeField] private EnemyBase enemyBase;

        // Customize the feature count/order to match your training metadata
        public int FeatureCount => 6;

        private void Awake()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) player = p.transform;
            }
            if (enemyBase == null)
            {
                enemyBase = GetComponent<EnemyBase>();
            }
        }

        public void CollectFeatures(float[] buffer)
        {
            if (buffer == null || buffer.Length < FeatureCount) return;
            if (!player || !enemyBase) { System.Array.Clear(buffer, 0, buffer.Length); return; }

            Vector3 epos = enemyBase.transform.position;
            Vector3 ppos = player.position;
            float dist = Vector2.Distance(epos, ppos);
            float vdelta = ppos.y - epos.y;
            bool los = HasLineOfSight(epos, ppos);

            // Health ratios
            float enemyHealthRatio = 1f;
            float playerHealthRatio = 1f;
            var eh = enemyBase.GetComponent<Enemy_Health>();
            if (eh != null)
            {
                var hs = eh.GetComponent<HealthSystem>();
                if (hs != null && hs.MaxHealth > 0)
                {
                    enemyHealthRatio = Mathf.Clamp01((float)hs.CurrentHealth / hs.MaxHealth);
                }
            }
            var ph = player.GetComponent<HealthSystem>();
            if (ph != null && ph.MaxHealth > 0)
            {
                playerHealthRatio = Mathf.Clamp01((float)ph.CurrentHealth / ph.MaxHealth);
            }

            // Cooldown example: use bee lunge cooldown if available; otherwise 0
            float cooldown = 0f;
            var bee = enemyBase as BeeEnemy;
            if (bee != null)
            {
                var f = bee.GetType().GetField("currentCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) cooldown = (float)f.GetValue(bee);
            }

            buffer[0] = dist;
            buffer[1] = vdelta;
            buffer[2] = los ? 1f : 0f;
            buffer[3] = playerHealthRatio;
            buffer[4] = enemyHealthRatio;
            buffer[5] = cooldown;
        }

        private bool HasLineOfSight(Vector3 a, Vector3 b)
        {
            Vector2 origin = a;
            Vector2 dir = (b - a);
            float dist = dir.magnitude;
            if (dist <= 0.01f) return true;
            dir /= dist;
            var hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
            return hit.collider == null;
        }
    }
}
