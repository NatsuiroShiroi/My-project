// Assets/FlowField.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Priority_Queue;
//
public class FlowField
{
    private readonly int originX, originY, width, height;
    private readonly float[,] cost;
    private readonly Vector2[,] flowDir;
    private readonly Tilemap tilemap;

    // 8-way movement offsets
    public static readonly Vector2Int[] Dirs = {
        new Vector2Int(1, 0),  new Vector2Int(-1, 0),
        new Vector2Int(0, 1),  new Vector2Int(0, -1),
        new Vector2Int(1, 1),  new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };
    private static readonly float[] dirCost = {
        1f, 1f, 1f, 1f,
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

    public void Generate(Vector2Int goal)
    {
        int gx = goal.x - originX, gy = goal.y - originY;
        if (gx < 0 || gy < 0 || gx >= width || gy >= height) return;

        // Initialize all costs to infinity
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cost[x, y] = float.MaxValue;

        var pq = new SimplePriorityQueue<Vector2Int>();
        cost[gx, gy] = 0f;
        pq.Enqueue(new Vector2Int(gx, gy), 0f);

        // Dijkstra flood-fill
        while (pq.Count > 0)
        {
            var cur = pq.Dequeue();
            float cc = cost[cur.x, cur.y];

            for (int i = 0; i < Dirs.Length; i++)
            {
                var d = Dirs[i];
                int nx = cur.x + d.x, ny = cur.y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                // world‐center of candidate
                Vector3 center = tilemap.GetCellCenterWorld(
                    new Vector3Int(nx + originX, ny + originY, 0));

                // skip if terrain blocks target cell
                if (Physics2D.OverlapBox(center, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;

                // ◼︎ AoE II “no corner-cut” check (only these 8 lines):
                if (d.x != 0 && d.y != 0)
                {
                    var o1 = new Vector3Int(cur.x + d.x + originX, cur.y + originY, 0);
                    var o2 = new Vector3Int(cur.x + originX, cur.y + d.y + originY, 0);
                    Vector3 w1 = tilemap.GetCellCenterWorld(o1);
                    Vector3 w2 = tilemap.GetCellCenterWorld(o2);
                    if (Physics2D.OverlapBox(w1, tilemap.cellSize * 0.9f, 0f) != null ||
                        Physics2D.OverlapBox(w2, tilemap.cellSize * 0.9f, 0f) != null)
                        continue;
                }

                float nc = cc + dirCost[i];
                if (nc < cost[nx, ny])
                {
                    cost[nx, ny] = nc;
                    pq.Enqueue(new Vector2Int(nx, ny), nc);
                }
            }
        }

        // Compute flowDir from cost gradients
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float best = cost[x, y];
                Vector2 bestDir = Vector2.zero;
                foreach (var d in Dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (cost[nx, ny] < best)
                    {
                        best = cost[nx, ny];
                        bestDir = d;
                    }
                }
                flowDir[x, y] = bestDir.normalized;
            }
    }

    public Vector2 GetDirection(Vector2Int cell)
    {
        int rx = cell.x - originX, ry = cell.y - originY;
        if (rx < 0 || ry < 0 || rx >= width || ry >= height)
            return Vector2.zero;
        return flowDir[rx, ry];
    }

    public bool IsCellInBounds(Vector2Int cell)
    {
        int rx = cell.x - originX, ry = cell.y - originY;
        return rx >= 0 && ry >= 0 && rx < width && ry < height;
    }

    /// <summary>
    /// Back-trace a static cell-to-cell path from start to goal.
    /// </summary>
    public List<Vector2Int> GetPath(Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        if (!IsCellInBounds(start) || !IsCellInBounds(goal))
            return path;

        Vector2Int cur = start;
        path.Add(cur);
        while (cur != goal)
        {
            float best = cost[cur.x - originX, cur.y - originY];
            Vector2Int next = cur;
            foreach (var d in Dirs)
            {
                var cand = cur + d;
                if (!IsCellInBounds(cand)) continue;
                float cVal = cost[cand.x - originX, cand.y - originY];
                if (cVal < best)
                {
                    best = cVal;
                    next = cand;
                }
            }
            if (next == cur) break;
            cur = next;
            path.Add(cur);
        }
        return path;
    }
}
