using UnityEngine;

namespace Ashore.AI.Utilities
{
    // Shared enemy AI states for simple state-machine AIs
    public enum SimpleEnemyState { Roaming, AttackWindup, Lunging, Retreating }

    public static class AIHelpers
    {
        // Compute approximate feet position from a collider
        public static Vector3 GetFeetPosition(Collider2D col, Transform fallback)
        {
            if (col != null)
            {
                var b = col.bounds;
                return new Vector3(b.center.x, b.min.y, b.center.z);
            }
            return fallback != null ? fallback.position + Vector3.down * 0.5f : Vector3.zero;
        }

        // Basic LOS check between two points against an obstacle mask
        public static bool HasLineOfSight(Vector2 origin, Vector2 target, LayerMask obstacleMask)
        {
            Vector2 dir = (target - origin);
            float dist = dir.magnitude;
            if (dist <= 0.01f) return true;
            dir /= dist;
            var hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
            return hit.collider == null;
        }
    }
}
