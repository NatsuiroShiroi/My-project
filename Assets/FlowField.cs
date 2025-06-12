// Assets/FlowField.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlowField
{
    private readonly int originX, originY, width, height;
    private readonly float[,] cost;
    private readonly Vector2[,] flowDir;
    private readonly Tilemap tilemap;

    public FlowField(Tilemap tilemap)
    {
        this.tilemap = tilemap;
        var b = tilemap.cellBounds;
        originX = b.xMin;
        originY = b.yMin;
        width = b.size.x;
        height = b.size.y;

        cost = new float[width, height];
        flowDir = new Vector2[width, height];

        Debug.Log($"[FlowField] auto-sized grid: origin=({originX},{originY}), size=({width},{height})");
    }

    /// <summary>
    /// goal is in tilemap (grid) coordinates
    /// </summary>
    public void Generate(Vector2Int goal)
    {
        int gx = goal.x - originX;
        int gy = goal.y - originY;

        if (gx < 0 || gy < 0 || gx >= width || gy >= height)
        {
            Debug.LogWarning($"[FlowField] goal {goal} outside auto-bounds");
            return;
        }

        // 1) compute walkable & init costs
        bool[,] walkable = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // center of this cell in world space
                var cell = new Vector3Int(x + originX, y + originY, 0);
                Vector3 center = tilemap.CellToWorld(cell) + tilemap.cellSize * 0.5f;

                // any collider at this cell blocks movement
                bool blocked = Physics2D.OverlapBox(center, tilemap.cellSize, 0f) != null;
                walkable[x, y] = !blocked;
                cost[x, y] = float.MaxValue;
            }
        }

        // 2) flood-fill from goal (Dijkstra)
        var q = new Queue<Vector2Int>();
        cost[gx, gy] = 0f;
        q.Enqueue(new Vector2Int(gx, gy));

        var dirs = new Vector2Int[] {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        while (q.Count > 0)
        {
            var c = q.Dequeue();
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
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        // 3) compute direction per cell
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float best = cost[x, y];
                Vector2 dir = Vector2.zero;
                foreach (var d in dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (cost[nx, ny] < best)
                    {
                        best = cost[nx, ny];
                        dir = d;
                    }
                }
                flowDir[x, y] = dir.normalized;
            }
        }

        Debug.Log("[FlowField] generation complete");
    }

    /// <summary>
    /// cell in grid coords → returns flow vector (world movement)
    /// </summary>
    public Vector2 GetDirection(Vector2Int cell)
    {
        int rx = cell.x - originX;
        int ry = cell.y - originY;
        if (rx < 0 || ry < 0 || rx >= width || ry >= height)
            return Vector2.zero;
        return flowDir[rx, ry];
    }
}
