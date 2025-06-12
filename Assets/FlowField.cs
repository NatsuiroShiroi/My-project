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

    public FlowField(int originX, int originY, int width, int height, Tilemap tilemap, LayerMask obstacleMask)
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

        // initialize costs
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

                // world position of candidate cell
                var cellPos = new Vector3Int(nx + originX, ny + originY, 0);
                Vector3 worldCenter = tilemap.GetCellCenterWorld(cellPos);

                // 1) If target cell is blocked by static terrain, skip
                if (Physics2D.OverlapBox(worldCenter, tilemap.cellSize, 0f, obstacleMask) != null)
                    continue;

                // ◼︎ AoE II “no corner-cut” check:
                // For diagonal moves, also ensure neither orthogonal neighbor is blocked
                if (d.x != 0 && d.y != 0)
                {
                    var ortho1 = new Vector3Int(cur.x + d.x + originX, cur.y + originY, 0);
                    var ortho2 = new Vector3Int(cur.x + originX, cur.y + d.y + originY, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(ortho1), tilemap.cellSize, 0f, obstacleMask) != null
                     || Physics2D.OverlapBox(tilemap.GetCellCenterWorld(ortho2), tilemap.cellSize, 0f, obstacleMask) != null)
                    {
                        continue;
                    }
                }

                // update cost & queue
                float nc = cc + DirCost[i];
                if (nc < cost[nx, ny])
                {
                    cost[nx, ny] = nc;
                    pq.Enqueue(new Vector2Int(nx, ny), nc);
                }
            }
        }

        // compute flow directions
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

    public float GetCost(Vector2Int cell)
    {
        int rx = cell.x - originX, ry = cell.y - originY;
        if (rx < 0 || ry < 0 || rx >= width || ry >= height) return float.MaxValue;
        return cost[rx, ry];
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
