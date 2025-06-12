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
            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            IssueMoveOrder(wp);
        }
    }

    void IssueMoveOrder(Vector3 worldDest)
    {
        if (tilemap == null || groundSprite == null)
            return;

        // 1) Gather selected units (or all)
        var sels = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in sels)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0)
            return;

        // 2) Compute ground cell bounds (inset half-cell)
        Bounds bs = groundSprite.bounds;
        Vector3 inset = tilemap.cellSize * 0.5f;
        Vector3 worldMin = bs.min + inset, worldMax = bs.max - inset;
        Vector3Int minC = tilemap.WorldToCell(worldMin);
        Vector3Int maxC = tilemap.WorldToCell(worldMax);

        // 3) Clamp click inside ground
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        Vector2Int baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minC.x, maxC.x),
            Mathf.Clamp(clickC.y, minC.y, maxC.y)
        );

        // 4) BFS to pick N free goal cells
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (goals.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();
            // skip if terrain obstacle
            Vector3 ctrWorld = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(ctrWorld, tilemap.cellSize * 0.9f, 0f, obstacleMask) == null)
                goals.Add(cur);

            foreach (var d in FlowField.Dirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt))
                    queue.Enqueue(nxt);
            }
        }

        // 5) For each unit, compute its static path and hand off
        int width = maxC.x - minC.x + 1;
        int height = maxC.y - minC.y + 1;

        for (int i = 0; i < goals.Count && i < movers.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];

            // Build flow field over terrain only
            var field = new FlowField(
                minC.x, minC.y,
                width, height,
                tilemap,
                obstacleMask
            );
            field.Generate(goal);

            // Determine start cell
            Vector3Int start3 = tilemap.WorldToCell(unit.transform.position);
            var start = new Vector2Int(start3.x, start3.y);

            // Backtrace a static path
            var path = field.GetPath(start, goal);

            // Give the unit its path to follow
            unit.SetPath(path);
        }
    }
}
