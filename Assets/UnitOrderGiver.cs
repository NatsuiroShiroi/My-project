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
        // Auto-find your Grid → Tilemap
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
            Debug.LogError("[OrderGiver] Could not find Grid → Tilemap!");

        // Auto-find your Ground sprite
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
        if (groundSprite == null)
            Debug.LogError("[OrderGiver] Could not find Ground → SpriteRenderer!");
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
        if (tilemap == null || groundSprite == null) return;

        // 1) Gather selected units (fallback to all movers)
        var selectors = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in selectors)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0) return;

        // 2) Compute ground cell bounds, inset half a cell so we stay inside
        Bounds bs = groundSprite.bounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        Vector3 worldMin = bs.min + half;
        Vector3 worldMax = bs.max - half;
        Vector3Int minC = tilemap.WorldToCell(worldMin);
        Vector3Int maxC = tilemap.WorldToCell(worldMax);

        // 3) Clamp clicked destination to within those bounds
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        Vector2Int baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minC.x, maxC.x),
            Mathf.Clamp(clickC.y, minC.y, maxC.y)
        );

        // 4) BFS outward to pick one free goal cell per mover
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (goals.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();

            // skip if terrain occupies this cell
            Vector3 ctrWorld = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(ctrWorld, tilemap.cellSize * 0.9f, 0f) == null)
            {
                goals.Add(cur);
            }

            // enqueue neighbors
            foreach (var d in FlowField.Dirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt))
                    queue.Enqueue(nxt);
            }
        }

        // 5) For each unit, build its static path and assign it
        int width = maxC.x - minC.x + 1;
        int height = maxC.y - minC.y + 1;

        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];

            // build a flow field over just the terrain grid
            var field = new FlowField(minC.x, minC.y, width, height, tilemap);
            field.Generate(goal);

            // backtrace one static path from the unit's start cell to its goal
            Vector3Int start3 = tilemap.WorldToCell(unit.transform.position);
            var start = new Vector2Int(start3.x, start3.y);
            List<Vector2Int> path = field.GetPath(start, goal);

            // hand off that path for the unit to follow
            unit.SetPath(path);
        }
    }
}
