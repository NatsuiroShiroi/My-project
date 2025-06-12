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
    private readonly LayerMask obstacleMask;

    public FlowField(Tilemap tilemap, LayerMask obstacleMask)
    {
        this.tilemap = tilemap;
        this.obstacleMask = obstacleMask;

        var bounds = tilemap.cellBounds;
        originX = bounds.xMin;
        originY = bounds.yMin;
        width = bounds.size.x;
        height = bounds.size.y;

        cost = new float[width, height];
        flowDir = new Vector2[width, height];
    }

    /// <summary>
    /// goal is in tilemap cell coordinates.
    /// </summary>
    public void Generate(Vector2Int goal)
    {
        // 1) Convert goal → local index
        int gx = goal.x - originX;
        int gy = goal.y - originY;
        if (gx < 0 || gy < 0 || gx >= width || gy >= height)
            return;

        // 2) Build walkable mask & init costs
        bool[,] walkable = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                // world-space center of this cell:
                Vector3 worldCenter = tilemap.CellToWorld(
                    new Vector3Int(x + originX, y + originY, 0)
                ) + tilemap.cellSize * 0.5f;

                // check for any obstacle collider here
                bool occupied = Physics2D.OverlapBox(
                    worldCenter,
                    tilemap.cellSize,
                    0f,
                    obstacleMask
                ) != null;

                walkable[x, y] = !occupied;
                cost[x, y] = float.MaxValue;
            }

        // 3) Dijkstra flood-fill from the goal cell
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
            float cc = cost[cur.x, cur.y];

            foreach (var d in dirs)
            {
                int nx = cur.x + d.x, ny = cur.y + d.y;
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

        // 4) Compute flow direction per cell
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
    }

    /// <summary>
    /// cell in tilemap coords → returns local flow direction
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
