using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NavAgent2D : MonoBehaviour
{
    [Header("Grid Source")]
    public NavGrid2D grid;
    [Tooltip("If true, the agent will automatically pick the NavGrid2D that contains its start/target position.")]
    public bool useDynamicGridSelection = true;

    [Header("Agent")]
    [Tooltip("Inflates the grid clearance checks by this radius to keep the agent from clipping into obstacles.")]
    public float agentRadius = 0.25f;
    [Tooltip("Optional: override obstacle mask used for clearance.")]
    public LayerMask obstacleMaskOverride;

    [Header("Debug / Gizmos")]
    public bool drawGizmos = true;
    public Color pathColor = new Color(1f, 0.7f, 0.2f, 0.9f);
    public Color agentColor = new Color(0.2f, 0.8f, 1f, 0.7f);

    private List<Vector2> lastPath = new List<Vector2>();

    void Reset()
    {
        grid = NavGrid2D.Instance;
    }

    public List<Vector2> RequestPath(Vector2 start, Vector2 target)
    {
        NavGrid2D targetGrid = grid;
        if (useDynamicGridSelection || targetGrid == null)
        {
            // Prefer grid containing start; then target; else nearest
            targetGrid = NavGrid2D.GetGridAt(start) ?? NavGrid2D.GetGridAt(target) ?? NavGrid2D.GetNearestGrid(start);
        }

        if (targetGrid == null)
        {
            Debug.LogWarning("NavAgent2D: No NavGrid2D available.");
            return null;
        }

        // Temporarily apply agent radius by adjusting grid clearance
        float originalClearance = targetGrid.clearance;
        LayerMask originalMask = targetGrid.obstacleMask;
        try
        {
            targetGrid.clearance = Mathf.Max(targetGrid.clearance, agentRadius);
            if (obstacleMaskOverride.value != 0) targetGrid.obstacleMask = obstacleMaskOverride;
            lastPath = targetGrid.FindPath(start, target);
        }
        finally
        {
            // restore
            targetGrid.clearance = originalClearance;
            targetGrid.obstacleMask = originalMask;
        }
        return lastPath;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = agentColor;
        Gizmos.DrawWireSphere(transform.position, agentRadius);
        if (lastPath != null && lastPath.Count > 0)
        {
            Gizmos.color = pathColor;
            for (int i = 0; i < lastPath.Count - 1; i++)
            {
                Gizmos.DrawLine(lastPath[i], lastPath[i + 1]);
                Gizmos.DrawWireSphere(lastPath[i], agentRadius * 0.3f);
            }
            Gizmos.DrawWireSphere(lastPath[lastPath.Count - 1], agentRadius * 0.3f);
        }
    }
}
