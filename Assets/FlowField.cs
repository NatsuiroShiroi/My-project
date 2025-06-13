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
    private readonly LayerMask obstacleMask;

    public static readonly Vector2Int[] Dirs = {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(1,-1),
        new Vector2Int(-1,1), new Vector2Int(-1,-1)
    };
    private static readonly float[] DirCost = {
        1f,1f,1f,1f,
        1.4142f,1.4142f,1.4142f,1.4142f
    };

    public FlowField(int originX, int originY, int width, int height,
                     Tilemap tilemap, LayerMask obstacleMask)
    {
        this.originX = originX;
        this.originY = originY;
        this.width = width;
        this.height = height;
        this.tilemap = tilemap;
        this.obstacleMask = obstacleMask;
        cost = new float[width, height];
        flowDir = new Vector2[width, height];
    }

    public void Generate(Vector2Int goal)
    {
        int gx = goal.x - originX, gy = goal.y - originY;
        if (gx < 0 || gy < 0 || gx >= width || gy >= height) return;

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

                var world = tilemap.GetCellCenterWorld(
                    new Vector3Int(nx + originX, ny + originY, 0));
                // static obstacle?
                if (Physics2D.OverlapBox(world, tilemap.cellSize * 0.9f, 0f, obstacleMask) != null)
                    continue;
                // no corner-cut
                if (d.x != 0 && d.y != 0)
                {
                    var o1 = new Vector3Int(cur.x + d.x + originX, cur.y + originY, 0);
                    var o2 = new Vector3Int(cur.x + originX, cur.y + d.y + originY, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(o1), tilemap.cellSize * 0.9f, 0f, obstacleMask) != null
                     || Physics2D.OverlapBox(tilemap.GetCellCenterWorld(o2), tilemap.cellSize * 0.9f, 0f, obstacleMask) != null)
                        continue;
                }
                float nc = cc + DirCost[i];
                if (nc < cost[nx, ny])
                {
                    cost[nx, ny] = nc;
                    pq.Enqueue(new Vector2Int(nx, ny), nc);
                }
            }
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float best = cost[x, y];
                Vector2 bd = Vector2.zero;
                foreach (var d in Dirs)
                {
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

    /// <summary>Backtrace a cell‐to‐cell path from start→goal.</summary>
    public List<Vector2Int> GetPath(Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        if (start.x < originX || start.y < originY || start.x >= originX + width || start.y >= originY + height)
            return path;
        if (goal.x < originX || goal.y < originY || goal.x >= originX + width || goal.y >= originY + height)
            return path;

        var cur = start;
        path.Add(cur);
        while (cur != goal)
        {
            Vector2Int next = cur;
            float best = cost[cur.x - originX, cur.y - originY];
            foreach (var d in Dirs)
            {
                var cand = cur + d;
                int rx = cand.x - originX, ry = cand.y - originY;
                if (rx < 0 || ry < 0 || rx >= width || ry >= height) continue;
                float cVal = cost[rx, ry];
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
