// Assets/FlowField.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlowField
{
    private readonly int width, height;
    private float[,] cost;
    private Vector2[,] flowDir;

    public FlowField(int width, int height)
    {
        this.width = width;
        this.height = height;
        cost = new float[width, height];
        flowDir = new Vector2[width, height];
    }

    /// <summary>
    /// Builds a Dijkstra flood-fill from 'goal' over the Tilemap,
    /// then computes, for each cell, the unit vector pointing to its lowest-cost neighbor.
    /// </summary>
    public void Generate(Vector2Int goal, Tilemap tilemap)
    {
        var queue = new Queue<Vector2Int>();
        bool[,] walkable = new bool[width, height];

        // 1) Build walkable mask from your obstacle Tilemap
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                walkable[x, y] = !tilemap.HasTile(new Vector3Int(x, y, 0));

        // 2) Init all costs to ∞
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cost[x, y] = float.MaxValue;

        // 3) Flood‐fill from the goal
        cost[goal.x, goal.y] = 0f;
        queue.Enqueue(goal);

        var dirs = new Vector2Int[] {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            float curCost = cost[cur.x, cur.y];

            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if (nxt.x < 0 || nxt.y < 0 || nxt.x >= width || nxt.y >= height) continue;
                if (!walkable[nxt.x, nxt.y]) continue;

                float nc = curCost + 1f;
                if (nc < cost[nxt.x, nxt.y])
                {
                    cost[nxt.x, nxt.y] = nc;
                    queue.Enqueue(nxt);
                }
            }
        }

        // 4) Compute best‐move direction per cell
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float bestCost = cost[x, y];
                Vector2 bestDir = Vector2.zero;

                foreach (var d in dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (cost[nx, ny] < bestCost)
                    {
                        bestCost = cost[nx, ny];
                        bestDir = d;
                    }
                }

                flowDir[x, y] = bestDir.normalized;
            }
    }

    /// <summary>
    /// Returns a unit‐length vector pointing from the given cell toward
    /// the goal along the computed cost gradient.
    /// </summary>
    public Vector2 GetDirection(Vector2Int cell)
    {
        if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
            return Vector2.zero;
        return flowDir[cell.x, cell.y];
    }
}
