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

    // 8‐way directions with proper costs
    private readonly Vector2Int[] dirs = {
        new Vector2Int(1,0),   new Vector2Int(-1,0),
        new Vector2Int(0,1),   new Vector2Int(0,-1),
        new Vector2Int(1,1),   new Vector2Int(1,-1),
        new Vector2Int(-1,1),  new Vector2Int(-1,-1)
    };
    private readonly float[] dirCost = {
        1f,1f,1f,1f, Mathf.Sqrt(2f), Mathf.Sqrt(2f),
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

        // 1) init
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cost[x, y] = float.MaxValue;

        cost[gx, gy] = 0f;
        var pq = new SimplePriorityQueue<Vector2Int>();
        pq.Enqueue(new Vector2Int(gx, gy), 0f);

        // 2) Dijkstra flood‐fill
        while (pq.Count > 0)
        {
            var cur = pq.Dequeue();
            float cCost = cost[cur.x, cur.y];
            for (int i = 0; i < dirs.Length; i++)
            {
                var d = dirs[i];
                int nx = cur.x + d.x, ny = cur.y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                // obstacle check
                var center = tilemap.GetCellCenterWorld(new Vector3Int(nx + originX, ny + originY, 0));
                if (Physics2D.OverlapBox(center, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;

                float nc = cCost + dirCost[i];
                if (nc < cost[nx, ny])
                {
                    cost[nx, ny] = nc;
                    pq.Enqueue(new Vector2Int(nx, ny), nc);
                }
            }
        }

        // 3) compute best‐neighbor dir
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float best = cost[x, y];
                Vector2 bestDir = Vector2.zero;
                for (int i = 0; i < dirs.Length; i++)
                {
                    var d = dirs[i];
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
        if (rx < 0 || ry < 0 || rx >= width || ry >= height) return Vector2.zero;
        return flowDir[rx, ry];
    }
}
