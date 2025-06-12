// Assets/FlowField.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Priority_Queue;

public class FlowField
{
    private readonly int originX, originY, width, height;
    private readonly float[,] cost;
    private readonly Vector2[,] flowDir;
    private readonly Tilemap tilemap;

    // 8‐way directions + costs
    public static readonly Vector2Int[] Dirs = {
        new Vector2Int(1,0),  new Vector2Int(-1,0),
        new Vector2Int(0,1),  new Vector2Int(0,-1),
        new Vector2Int(1,1),  new Vector2Int(1,-1),
        new Vector2Int(-1,1), new Vector2Int(-1,-1)
    };
    private static readonly float[] DirCost = {
        1f,1f,1f,1f,
        Mathf.Sqrt(2f), Mathf.Sqrt(2f),
        Mathf.Sqrt(2f), Mathf.Sqrt(2f)
    };

    public FlowField(int originX, int originY, int width, int height, Tilemap tilemap)
    {
        this.originX = originX;
        this.originY = originY;
        this.width = width;
        this.height = height;
        this.tilemap = tilemap;
        cost = new float[width, height];
        flowDir = new Vector2[width, height];
    }

    /// <summary>
    /// Builds the cost field from goal and computes flowDir.
    /// </summary>
    public void Generate(Vector2Int goal)
    {
        int gx = goal.x - originX, gy = goal.y - originY;
        if (gx < 0 || gy < 0 || gx >= width || gy >= height) return;

        // init
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cost[x, y] = float.MaxValue;

        var pq = new SimplePriorityQueue<Vector2Int>();
        cost[gx, gy] = 0f;
        pq.Enqueue(new Vector2Int(gx, gy), 0f);

        while (pq.Count > 0)
        {
            var cur = pq.Dequeue();
            float cc = cost[cur.x, cur.y];
            for (int i = 0; i < Dirs.Length; i++)
            {
                var d = Dirs[i];
                int nx = cur.x + d.x, ny = cur.y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                // terrain collision
                var world = tilemap.GetCellCenterWorld(
                    new Vector3Int(nx + originX, ny + originY, 0));
                if (Physics2D.OverlapBox(world, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;

                float nc = cc + DirCost[i];
                if (nc < cost[nx, ny])
                {
                    cost[nx, ny] = nc;
                    pq.Enqueue(new Vector2Int(nx, ny), nc);
                }
            }
        }

        // compute flowDir
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float best = cost[x, y];
                Vector2 bd = Vector2.zero;
                for (int i = 0; i < Dirs.Length; i++)
                {
                    var d = Dirs[i];
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (cost[nx, ny] < best)
                    {
                        best = cost[nx, ny];
                        bd = d;
                    }
                }
                flowDir[x, y] = bd.normalized;
            }
    }

    /// <summary>
    /// Returns raw Dijkstra cost (distance-to-goal) for a cell.
    /// </summary>
    public float GetCost(Vector2Int cell)
    {
        int rx = cell.x - originX, ry = cell.y - originY;
        if (rx < 0 || ry < 0 || rx >= width || ry >= height) return float.MaxValue;
        return cost[rx, ry];
    }

    /// <summary>
    /// Backtrace the static path from start→goal using the cost field.
    /// </summary>
    public List<Vector2Int> GetPath(Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        if (!IsCellInBounds(start) || !IsCellInBounds(goal)) return path;

        Vector2Int cur = start;
        path.Add(cur);
        // loop until we reach the goal or get stuck
        while (cur != goal)
        {
            float best = GetCost(cur);
            Vector2Int next = cur;
            foreach (var d in Dirs)
            {
                var cand = cur + d;
                if (!IsCellInBounds(cand)) continue;
                float cVal = GetCost(cand);
                if (cVal < best)
                {
                    best = cVal;
                    next = cand;
                }
            }
            if (next == cur) break;  // no progress
            cur = next;
            path.Add(cur);
        }
        return path;
    }

    public Vector2 GetDirection(Vector2Int cell)
    {
        int rx = cell.x - originX, ry = cell.y - originY;
        if (rx < 0 || ry < 0 || rx >= width || ry >= height) return Vector2.zero;
        return flowDir[rx, ry];
    }

    public bool IsCellInBounds(Vector2Int cell)
    {
        int rx = cell.x - originX, ry = cell.y - originY;
        return rx >= 0 && ry >= 0 && rx < width && ry < height;
    }
}
