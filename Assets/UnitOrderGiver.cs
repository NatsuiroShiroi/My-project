// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;

    void Awake()
    {
        // Auto-find Grid → Tilemap
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();

        // Auto-find Ground sprite
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            IssueMoveOrder(wp);
        }
    }

    void IssueMoveOrder(Vector3 worldDest)
    {
        if (tilemap == null || groundSprite == null)
            return;

        // Clear previous reservations
        UnitMover.ClearReservations();

        // 1) Gather selected units (or all)
        var selectors = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var sel in selectors)
            if (sel.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0)
            return;

        // 2) Compute ground cell bounds (inset half-cell)
        Bounds bs = groundSprite.bounds;
        Vector3 inset = tilemap.cellSize * 0.5f;
        Vector3 worldMin = bs.min + inset;
        Vector3 worldMax = bs.max - inset;
        Vector3Int minC = tilemap.WorldToCell(worldMin);
        Vector3Int maxC = tilemap.WorldToCell(worldMax);

        // 3) Clamp click inside ground
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        Vector2Int baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minC.x, maxC.x),
            Mathf.Clamp(clickC.y, minC.y, maxC.y)
        );

        // 4) BFS to pick distinct goal cells
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (goals.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();

            // Skip if terrain blocks this cell
            Vector3 ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f) == null)
                goals.Add(cur);

            // Enqueue neighbors
            foreach (var d in FlowField.Dirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt))
                    queue.Enqueue(nxt);
            }
        }

        // 5) Build & assign a flow field per unit
        int width = maxC.x - minC.x + 1;
        int height = maxC.y - minC.y + 1;

        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];

            var field = new FlowField(
                minC.x, minC.y,
                width, height,
                tilemap
            );
            field.Generate(goal);
            unit.SetFlowField(field, goal);
        }
    }
}
