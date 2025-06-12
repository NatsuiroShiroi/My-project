using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlowField
{
    private int width, height;
    private Vector2Int goal;
    private float[,] cost;
    private Vector2[,] flowDir;

    public FlowField(int w, int h)
    {
        width = w;
        height = h;
        cost = new float[w, h];
        flowDir = new Vector2[w, h];
    }

    public void Generate(Vector2Int goal, Tilemap tilemap)
    {
        this.goal = goal;
        var queue = new Queue<Vector2Int>();

        // Mark walkable grid by checking Tilemap
        bool[,] walkable = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                walkable[x, y] = !tilemap.HasTile(cell); // Empty = walkable
            }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cost[x, y] = float.MaxValue;

        cost[goal.x, goal.y] = 0;
        queue.Enqueue(goal);

        Vector2Int[] directions = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            float currentCost = cost[current.x, current.y];

            foreach (var dir in directions)
            {
                var next = current + dir;
                if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height) continue;
                if (!walkable[next.x, next.y]) continue;
                float nextCost = currentCost + 1f;
                if (nextCost < cost[next.x, next.y])
                {
                    cost[next.x, next.y] = nextCost;
                    queue.Enqueue(next);
                }
            }
        }

        // Compute flow field directions (lowest cost neighbor)
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float minCost = cost[x, y];
                Vector2 best = Vector2.zero;
                foreach (var dir in directions)
                {
                    int nx = x + dir.x, ny = y + dir.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (cost[nx, ny] < minCost)
                    {
                        minCost = cost[nx, ny];
                        best = dir;
                    }
                }
                flowDir[x, y] = best.normalized;
            }
    }

    public Vector2 GetDirection(Vector2Int cell)
    {
        if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
            return Vector2.zero;
        return flowDir[cell.x, cell.y];
    }
}
