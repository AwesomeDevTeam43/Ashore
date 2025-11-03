using System.Collections.Generic;
using UnityEngine;

// Lightweight 2D grid A* for platformer-style scenes.
// Builds a temporary grid around the midpoint of start/target and finds a path avoiding obstacleMask.
public class GridPathfinder2D
{
    public class Node
    {
        public bool walkable;
        public Vector2 worldPos;
        public int x, y;
        public int gCost;
        public int hCost;
        public Node parent;
        public int fCost => gCost + hCost;
        public Node(bool walkable, Vector2 worldPos, int x, int y)
        {
            this.walkable = walkable;
            this.worldPos = worldPos;
            this.x = x;
            this.y = y;
        }
    }

    private float nodeRadius;
    private float clearance;
    private Vector2 gridWorldSize;
    private LayerMask obstacleMask;

    private Node[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;
    private Vector2 origin;

    // Optional caching to avoid rebuilding grids too often (chunk-based)
    private static readonly System.Collections.Generic.Dictionary<string, CacheEntry> s_cache = new System.Collections.Generic.Dictionary<string, CacheEntry>();
    private bool useCache = true;
    private float cacheTTL = 1.0f; // seconds

    private class CacheEntry
    {
        public Node[,] grid;
        public Vector2 origin;
        public int sizeX, sizeY;
        public float nodeRadius;
        public Vector2 gridWorldSize;
        public int obstacleMaskValue;
        public float createdTime;
    }

    public GridPathfinder2D(Vector2 gridWorldSize, float nodeRadius, LayerMask obstacleMask)
    {
        this.gridWorldSize = gridWorldSize;
        this.nodeRadius = Mathf.Max(0.05f, nodeRadius);
        this.obstacleMask = obstacleMask;
        nodeDiameter = this.nodeRadius * 2f;
    }

    public void Configure(Vector2 gridWorldSize, float nodeRadius, LayerMask obstacleMask)
    {
        this.gridWorldSize = gridWorldSize;
        this.nodeRadius = Mathf.Max(0.05f, nodeRadius);
        this.obstacleMask = obstacleMask;
        nodeDiameter = this.nodeRadius * 2f;
    }

    public void SetClearance(float clearanceRadius)
    {
        clearance = Mathf.Max(0f, clearanceRadius);
    }

    public void SetCache(bool use, float ttlSeconds)
    {
        useCache = use;
        cacheTTL = Mathf.Max(0f, ttlSeconds);
    }

    private void BuildGrid(Vector2 center)
    {
        gridSizeX = Mathf.Max(2, Mathf.RoundToInt(gridWorldSize.x / nodeDiameter));
        gridSizeY = Mathf.Max(2, Mathf.RoundToInt(gridWorldSize.y / nodeDiameter));

        // Chunk key based on gridWorldSize and integer chunk indices from center
        int chunkX = Mathf.FloorToInt(center.x / gridWorldSize.x);
        int chunkY = Mathf.FloorToInt(center.y / gridWorldSize.y);
        string key = $"{gridWorldSize.x:F2}x{gridWorldSize.y:F2}|r{nodeRadius:F3}|m{obstacleMask.value}|cx{chunkX}|cy{chunkY}";

        // Compute aligned chunk center/origin
        Vector2 chunkCenter = new Vector2((chunkX + 0.5f) * gridWorldSize.x, (chunkY + 0.5f) * gridWorldSize.y);
        Vector2 computedOrigin = chunkCenter - gridWorldSize * 0.5f;

        bool reused = false;
        if (useCache && s_cache.TryGetValue(key, out var entry))
        {
            if (Time.time - entry.createdTime <= cacheTTL &&
                Mathf.Approximately(entry.nodeRadius, nodeRadius) &&
                Mathf.Approximately(entry.gridWorldSize.x, gridWorldSize.x) &&
                Mathf.Approximately(entry.gridWorldSize.y, gridWorldSize.y) &&
                entry.obstacleMaskValue == obstacleMask.value)
            {
                grid = entry.grid;
                origin = entry.origin; // should equal computedOrigin
                gridSizeX = entry.sizeX;
                gridSizeY = entry.sizeY;
                reused = true;
            }
        }

        if (!reused)
        {
            grid = new Node[gridSizeX, gridSizeY];
            origin = computedOrigin;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector2 worldPoint = origin + new Vector2(x * nodeDiameter + nodeRadius, y * nodeDiameter + nodeRadius);
                    float r = nodeRadius + clearance;
                    bool walkable = !Physics2D.OverlapCircle(worldPoint, r, obstacleMask);
                    grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }

            if (useCache)
            {
                s_cache[key] = new CacheEntry
                {
                    grid = grid,
                    origin = origin,
                    sizeX = gridSizeX,
                    sizeY = gridSizeY,
                    nodeRadius = nodeRadius,
                    gridWorldSize = gridWorldSize,
                    obstacleMaskValue = obstacleMask.value,
                    createdTime = Time.time
                };
            }
        }
    }

    private Node NodeFromWorldPoint(Vector2 worldPos)
    {
        float percentX = Mathf.InverseLerp(origin.x, origin.x + gridWorldSize.x, worldPos.x);
        float percentY = Mathf.InverseLerp(origin.y, origin.y + gridWorldSize.y, worldPos.y);
        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);
        return grid[x, y];
    }

    private IEnumerable<Node> GetNeighbours(Node node)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = node.x + dx;
                int ny = node.y + dy;
                if (nx < 0 || ny < 0 || nx >= gridSizeX || ny >= gridSizeY) continue;
                var neighbour = grid[nx, ny];
                if (!neighbour.walkable) continue;
                // Prevent corner cutting on diagonals
                if (dx != 0 && dy != 0)
                {
                    var n1 = grid[node.x + dx, node.y];
                    var n2 = grid[node.x, node.y + dy];
                    if (!n1.walkable || !n2.walkable) continue;
                }
                yield return neighbour;
            }
        }
    }

    private static int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.x - b.x);
        int dstY = Mathf.Abs(a.y - b.y);
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    public List<Vector2> FindPath(Vector2 start, Vector2 target)
    {
        return FindPath(start, target, null);
    }

    public List<Vector2> FindPath(Vector2 start, Vector2 target, Vector2? centerOverride)
    {
        Vector2 center = centerOverride.HasValue ? centerOverride.Value : (start + target) * 0.5f;
        BuildGrid(center);

    Node startNode = NodeFromWorldPoint(start);
    Node targetNode = NodeFromWorldPoint(target);

        // Reset node path state for a fresh search
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                grid[x, y].gCost = 0;
                grid[x, y].hCost = 0;
                grid[x, y].parent = null;
            }
        }
        if (!startNode.walkable || !targetNode.walkable)
        {
            // Try to nudge nodes to nearest walkable around them (simple ring search)
            if (!startNode.walkable)
            {
                Node fallbackS = null;
                int radiusS = 1;
                int maxRS = Mathf.Max(gridSizeX, gridSizeY);
                while (radiusS < maxRS && fallbackS == null)
                {
                    for (int x = -radiusS; x <= radiusS; x++)
                    {
                        for (int y = -radiusS; y <= radiusS; y++)
                        {
                            int nx = Mathf.Clamp(startNode.x + x, 0, gridSizeX - 1);
                            int ny = Mathf.Clamp(startNode.y + y, 0, gridSizeY - 1);
                            var n = grid[nx, ny];
                            if (n.walkable) { fallbackS = n; break; }
                        }
                        if (fallbackS != null) break;
                    }
                    radiusS++;
                }
                if (fallbackS != null) startNode = fallbackS;
            }
            if (!targetNode.walkable)
            {
                Node fallback = null;
                int radius = 1;
                int maxR = Mathf.Max(gridSizeX, gridSizeY);
                while (radius < maxR && fallback == null)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            int nx = Mathf.Clamp(targetNode.x + x, 0, gridSizeX - 1);
                            int ny = Mathf.Clamp(targetNode.y + y, 0, gridSizeY - 1);
                            var n = grid[nx, ny];
                            if (n.walkable) { fallback = n; break; }
                        }
                        if (fallback != null) break;
                    }
                    radius++;
                }
                if (fallback != null) targetNode = fallback;
            }
        }

    var openSet = new List<Node>(128);
        var closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost || (openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost))
                {
                    current = openSet[i];
                }
            }
            openSet.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (var neighbour in GetNeighbours(current))
            {
                if (closedSet.Contains(neighbour)) continue;
                int newCost = current.gCost + GetDistance(current, neighbour);
                if (!openSet.Contains(neighbour) || newCost < neighbour.gCost)
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = current;
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
        }

        return null; // no path
    }

    private List<Vector2> RetracePath(Node startNode, Node endNode)
    {
        var path = new List<Vector2>();
        Node current = endNode;
        while (current != null && current != startNode)
        {
            path.Add(current.worldPos);
            current = current.parent;
        }
        path.Add(startNode.worldPos);
        path.Reverse();
        // Optional simple thinning
        return path;
    }
}
