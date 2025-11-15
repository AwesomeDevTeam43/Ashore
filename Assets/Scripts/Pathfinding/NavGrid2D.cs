using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class NavGrid2D : MonoBehaviour
{
    public static NavGrid2D Instance { get; private set; }
    // Registry of all grids in the scene to support multi-grid setups
    public static readonly List<NavGrid2D> All = new List<NavGrid2D>();

    [Header("Grid Bounds (world)")]
    public Vector2 origin = new Vector2(-20, -12);
    public Vector2 size = new Vector2(40, 24);

    [Header("Resolution")]
    [Tooltip("Half of a cell size (world units)")]
    public float nodeRadius = 0.12f;
    [Tooltip("Extra clearance added to nodeRadius when checking obstacles")] 
    public float clearance = 0.0f;
    [Tooltip("When true, draw a denser overlay of grid gizmos for debugging.")]
    public bool drawDenseGizmos = false;

    [Header("Layers")]
    public LayerMask obstacleMask; // Ground | MovingPlatform

    [Header("Bake")]
    public bool bakeOnStart = true;
    public float gizmoAlpha = 0.1f;

    public class Node
    {
        public bool walkable;
        public Vector2 worldPos;
        public int x, y;
        public int g, h; public Node parent; public int f => g + h;
        public Node(bool w, Vector2 p, int x, int y){walkable=w;worldPos=p;this.x=x;this.y=y;}
    }

    private Node[,] grid;
    private int gridX, gridY; private float nodeDiameter;

    void Awake()
    {
        if (Instance == null) Instance = this; // keep first grid as legacy Instance for backward compatibility
        if (!All.Contains(this)) All.Add(this);
        nodeDiameter = Mathf.Max(0.05f, nodeRadius) * 2f;
    }

    void OnDestroy()
    {
        if (All.Contains(this)) All.Remove(this);
        if (Instance == this) Instance = All.Count > 0 ? All[0] : null;
    }

    void Start()
    {
        if (bakeOnStart) Bake();
    }

    public void Bake()
    {
        gridX = Mathf.Max(2, Mathf.RoundToInt(size.x / nodeDiameter));
        gridY = Mathf.Max(2, Mathf.RoundToInt(size.y / nodeDiameter));
        grid = new Node[gridX, gridY];
        float r = Mathf.Max(0.01f, nodeRadius + clearance);
        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                Vector2 wp = origin + new Vector2(x * nodeDiameter + nodeRadius, y * nodeDiameter + nodeRadius);
                bool walk = !Physics2D.OverlapCircle(wp, r, obstacleMask);
                grid[x, y] = new Node(walk, wp, x, y);
            }
        }
    }

    // Multi-grid helpers
    public Rect GetWorldRect()
    {
        // Support negative sizes gracefully
        Vector2 min = new Vector2(Mathf.Min(origin.x, origin.x + size.x), Mathf.Min(origin.y, origin.y + size.y));
        Vector2 max = new Vector2(Mathf.Max(origin.x, origin.x + size.x), Mathf.Max(origin.y, origin.y + size.y));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public bool Contains(Vector2 worldPos)
    {
        var r = GetWorldRect();
        return r.Contains(worldPos);
    }

    public static NavGrid2D GetGridAt(Vector2 worldPos)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var g = All[i];
            if (g != null && g.Contains(worldPos)) return g;
        }
        return null;
    }

    public static NavGrid2D GetNearestGrid(Vector2 worldPos)
    {
        float best = float.PositiveInfinity; NavGrid2D bestG = null;
        for (int i = 0; i < All.Count; i++)
        {
            var g = All[i]; if (g == null) continue;
            var rect = g.GetWorldRect();
            // Distance 0 if inside; otherwise shortest distance to rect
            float dx = Mathf.Max(0, rect.xMin - worldPos.x, worldPos.x - rect.xMax);
            float dy = Mathf.Max(0, rect.yMin - worldPos.y, worldPos.y - rect.yMax);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < best) { best = d; bestG = g; }
        }
        return bestG;
    }

    private Node NodeFromWorld(Vector2 world)
    {
        float px = Mathf.InverseLerp(origin.x, origin.x + size.x, world.x);
        float py = Mathf.InverseLerp(origin.y, origin.y + size.y, world.y);
        int x = Mathf.Clamp(Mathf.RoundToInt((gridX - 1) * px), 0, gridX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridY - 1) * py), 0, gridY - 1);
        return grid[x, y];
    }

    private IEnumerable<Node> Neigh(Node n)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = n.x + dx, ny = n.y + dy;
            if (nx < 0 || ny < 0 || nx >= gridX || ny >= gridY) continue;
            var m = grid[nx, ny]; if (!m.walkable) continue;
            if (dx != 0 && dy != 0)
            {
                if (!grid[n.x + dx, n.y].walkable || !grid[n.x, n.y + dy].walkable) continue;
            }
            yield return m;
        }
    }

    private static int Dist(Node a, Node b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return dx > dy ? 14 * dy + 10 * (dx - dy) : 14 * dx + 10 * (dy - dx);
    }

    public List<Vector2> FindPath(Vector2 start, Vector2 target)
    {
        if (grid == null) Bake();
        var s = NodeFromWorld(start); var t = NodeFromWorld(target);
        if (!s.walkable) s = NearestWalkable(s) ?? s;
        if (!t.walkable) t = NearestWalkable(t) ?? t;

        // Reset costs
        for (int x = 0; x < gridX; x++)
          for (int y = 0; y < gridY; y++) { grid[x, y].g = 0; grid[x, y].h = 0; grid[x, y].parent = null; }

        var open = new List<Node>(128); var closed = new HashSet<Node>();
        open.Add(s);
        while (open.Count > 0)
        {
            Node cur = open[0];
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].f < cur.f || (open[i].f == cur.f && open[i].h < cur.h)) cur = open[i];
            }
            open.Remove(cur); closed.Add(cur);
            if (cur == t) return Retrace(s, t);
            foreach (var nb in Neigh(cur))
            {
                if (closed.Contains(nb)) continue;
                int newG = cur.g + Dist(cur, nb);
                if (!open.Contains(nb) || newG < nb.g) { nb.g = newG; nb.h = Dist(nb, t); nb.parent = cur; if (!open.Contains(nb)) open.Add(nb); }
            }
        }
        return null;
    }

    private Node NearestWalkable(Node from)
    {
        int maxR = Mathf.Max(gridX, gridY);
        for (int r = 1; r < maxR; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                int nx = Mathf.Clamp(from.x + dx, 0, gridX - 1);
                int ny = Mathf.Clamp(from.y + dy, 0, gridY - 1);
                var n = grid[nx, ny]; if (n.walkable) return n;
            }
        }
        return null;
    }

    private List<Vector2> Retrace(Node s, Node t)
    {
        var list = new List<Vector2>(); var cur = t; while (cur != null && cur != s){ list.Add(cur.worldPos); cur = cur.parent; } list.Add(s.worldPos); list.Reverse(); return list;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0,1,0,gizmoAlpha);
        Gizmos.DrawWireCube(origin + size*0.5f, size);
        if (grid != null)
        {
            float a = gizmoAlpha * 1.5f;
            int step = drawDenseGizmos ? 1 : Mathf.Max(1, Mathf.RoundToInt(0.5f / Mathf.Max(0.05f, nodeRadius)));
            for (int x = 0; x < gridX; x+=step)
            for (int y = 0; y < gridY; y+=step)
            {
                var n = grid[x,y];
                Gizmos.color = n.walkable ? new Color(0,0.8f,0,a) : new Color(0.8f,0,0,a);
                float sz = nodeRadius * 1.6f;
                Gizmos.DrawCube(n.worldPos, new Vector3(sz, sz, sz));
            }
            // Draw clearance ring reference
            Gizmos.color = new Color(0.2f,0.5f,1f,0.4f);
            Gizmos.DrawWireSphere(origin + size*0.5f, nodeRadius + clearance);
        }
    }
}
