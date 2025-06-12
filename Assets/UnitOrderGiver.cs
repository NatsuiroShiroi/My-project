// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;

    [Tooltip("LayerMask for static terrain obstacles")]
    public LayerMask obstacleMask;

    // 8‐way neighbor offsets, reused for BFS goal assignment
    private static readonly Vector2Int[] neighborDirs = FlowField.Dirs;

    void Awake()
    {
        // Auto‐find Grid → Tilemap
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();

        // Auto‐find Ground sprite
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0f;
            IssueMoveOrder(worldPoint);
        }
    }

    void IssueMoveOrder(Vector3 worldDest)
    {
        if (tilemap == null || groundSprite == null)
            return;

        // Clear previous unit reservations
        UnitMover.ClearReservations();

        // Gather selected units (or all, if none)
        var selectors = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in selectors)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0)
            return;

        // Compute ground cell bounds (inset half a cell)
        Bounds bs = groundSprite.bounds;
        Vector3 inset = tilemap.cellSize * 0.5f;
        Vector3 worldMin = bs.min + inset;
        Vector3 worldMax = bs.max - inset;
        Vector3Int minC = tilemap.WorldToCell(worldMin);
        Vector3Int maxC = tilemap.WorldToCell(worldMax);

        // Clamp clicked destination inside ground
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        Vector2Int baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minC.x, maxC.x),
            Mathf.Clamp(clickC.y, minC.y, maxC.y)
        );

        // BFS outward to pick N free goal cells
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (goals.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();

            // Skip if this cell is blocked by static terrain
            Vector3 worldCtr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(worldCtr, tilemap.cellSize * 0.9f, 0f, obstacleMask) == null)
            {
                goals.Add(cur);
            }

            // Enqueue neighbors
            foreach (var d in neighborDirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt))
                    queue.Enqueue(nxt);
            }
        }

        // Build and assign a FlowField & move order per unit
        int width = maxC.x - minC.x + 1;
        int height = maxC.y - minC.y + 1;
        for (int i = 0; i < goals.Count && i < movers.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];

            var field = new FlowField(
                minC.x, minC.y,
                width, height,
                tilemap,
                obstacleMask    // ← pass in your terrain LayerMask here
            );
            field.Generate(goal);
            unit.SetFlowField(field, goal);
        }
    }
}
