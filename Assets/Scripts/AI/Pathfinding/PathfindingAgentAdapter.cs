using System.Collections.Generic;
using UnityEngine;
using Ashore.AI.Pathfinding;

namespace Ashore.AI.Pathfinding
{
    // MonoBehaviour adapter to provide a unified pathfinding interface for agents.
    // It will use NavGrid2D when available; otherwise it will fall back to GridPathfinder2D.
    public class PathfindingAgentAdapter : MonoBehaviour, IAgentPathfinder
    {
        [Header("Local Grid Fallback Config")]
        [SerializeField] private Vector2 gridWorldSize = new Vector2(12, 8);
        [SerializeField] private float nodeRadius = 0.2f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private bool enableCache = true;
        [SerializeField] private float cacheTTL = 0.75f;

        private GridPathfinder2D _localPathfinder;

        private void Awake()
        {
            _localPathfinder = new GridPathfinder2D(gridWorldSize, nodeRadius, obstacleMask);
            _localPathfinder.SetCache(enableCache, cacheTTL);
        }

        public List<Vector2> FindPath(Vector2 start, Vector2 target)
        {
            if (NavGrid2D.Instance != null)
            {
                return NavGrid2D.Instance.FindPath(start, target);
            }
            _localPathfinder.Configure(gridWorldSize, nodeRadius, obstacleMask);
            return _localPathfinder.FindPath(start, target);
        }
    }
}
