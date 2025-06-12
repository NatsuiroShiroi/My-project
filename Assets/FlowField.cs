using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlowField
{
    private readonly int width, height;
    private readonly int originX, originY;      // cellBounds.xMin / yMin
    private float[,] cost;
    private Vector2[,] flowDir;

    public FlowField(Tilemap tilemap)
    {
        var bounds = tilemap.cellBounds;
        originX = bounds.xMin;
        originY = bounds.yMin;
        width = bounds.size.x;
        height = bounds.size.y;

        cost = new float[width, height];
        flowDir = new Vector2[width, height];
    }

    /// <summary>
    /// goalCell is in world‐cell coords (tilemap.Grid cell coordinates).
    /// </summary>
    public void Generate(Vector2Int goalCell, Tilemap tilemap)
    {
        // Convert goalCell → local array indices
        int gx = goalCell.x - originX;
        int gy = goalCell.y - originY;
        if (gx < 0 || gy < 0 || gx >= width || gy >= height)
            return;  // clicked outside of bounds

        // Build walkable mask & init cost
        var walkable = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int wx = originX + x, wy = originY + y;
                walkable[x, y] = !tilemap.HasTile(new Vector3Int(wx, wy, 0));
                cost[x, y] = float.MaxValue;
            }
        }

        // Dijkstra flood-fill from goal
        var queue = new Queue<Vector2Int>();
        cost[gx, gy] = 0f;
        queue.Enqueue(new Vector2Int(gx, gy));

        var dirs = new Vector2Int[]{
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            float cCost = cost[cur.x, cur.y];

            foreach (var d in dirs)
            {
                int nx = cur.x + d.x, ny = cur.y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (!walkable[nx, ny]) continue;

                float nc = cCost + 1f;
                if (nc < cost[nx, ny])
                {
                    cost[nx, ny] = nc;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        // Compute flowDir: point each cell toward its lowest-cost neighbor
        for (int x = 0; x < width; x++)
        {
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
        }
    }

    /// <summary>
    /// cell is in world‐cell coords; we convert to array‐indices internally.
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
