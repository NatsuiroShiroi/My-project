using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlowField
{
    private readonly int originX, originY, width, height;
    private readonly float[,] cost;
    private readonly Vector2[,] flowDir;
    private readonly Tilemap tilemap;
    private readonly LayerMask obstacleMask;

    public FlowField(Tilemap tilemap, LayerMask obstacleMask)
    {
        this.tilemap = tilemap;
        this.obstacleMask = obstacleMask;

        var b = tilemap.cellBounds;
        originX = b.xMin;
        originY = b.yMin;
        width = b.size.x;
        height = b.size.y;

        Debug.Log($"[FlowField] cellBounds = {b} → origin({originX},{originY}), size({width},{height})");

        cost = new float[width, height];
        flowDir = new Vector2[width, height];
    }

    public void Generate(Vector2Int goal)
    {
        // convert goal into local array coords
        int gx = goal.x - originX;
        int gy = goal.y - originY;
        if (gx < 0 || gy < 0 || gx >= width || gy >= height)
        {
            Debug.LogWarning($"[FlowField] Goal {goal} is outside tilemap bounds!");
            return;
        }

        // build walkable mask & init cost[]
        bool[,] walkable = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 worldCenter = tilemap.CellToWorld(new Vector3Int(x + originX, y + originY, 0))
                                      + tilemap.cellSize * 0.5f;
                bool occupied = Physics2D.OverlapBox(worldCenter, tilemap.cellSize, 0f, obstacleMask) != null;
                walkable[x, y] = !occupied;
                cost[x, y] = float.MaxValue;
            }

        // flood‐fill from the goal
        var queue = new Queue<Vector2Int>();
        cost[gx, gy] = 0f;
        queue.Enqueue(new Vector2Int(gx, gy));

        var dirs = new Vector2Int[] {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            float cc = cost[c.x, c.y];
            foreach (var d in dirs)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (!walkable[nx, ny]) continue;
                float nc = cc + 1f;
                if (nc < cost[nx, ny])
                {
                    cost[nx, ny] = nc;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        // compute flowDir
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float best = cost[x, y];
                Vector2 v = Vector2.zero;
                foreach (var d in dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (cost[nx, ny] < best)
                    {
                        best = cost[nx, ny];
                        v = d;
                    }
                }
                flowDir[x, y] = v.normalized;
            }

        Debug.Log("[FlowField] Generation complete.");
    }

    public Vector2 GetDirection(Vector2Int cell)
    {
        int rx = cell.x - originX;
        int ry = cell.y - originY;
        if (rx < 0 || ry < 0 || rx >= width || ry >= height)
            return Vector2.zero;
        return flowDir[rx, ry];
    }
}
