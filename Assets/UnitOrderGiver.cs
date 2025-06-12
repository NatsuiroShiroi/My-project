// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;
    private static readonly Vector2Int[] neighborDirs = FlowField.Dirs;

    void Awake()
    {
        var g = GameObject.Find("Grid");
        if (g != null) tilemap = g.GetComponentInChildren<Tilemap>();
        var gr = GameObject.Find("Ground");
        if (gr != null) groundSprite = gr.GetComponent<SpriteRenderer>();
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
        if (tilemap == null || groundSprite == null) return;

        UnitMover.ClearReservations();

        var sels = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in sels)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0) return;

        // ground cell bounds inset half-cell
        var bs = groundSprite.bounds;
        var inset = tilemap.cellSize * 0.5f;
        var minC = tilemap.WorldToCell(bs.min + inset);
        var maxC = tilemap.WorldToCell(bs.max - inset);

        // clamp click inside
        var click3 = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(click3.x, minC.x, maxC.x),
            Mathf.Clamp(click3.y, minC.y, maxC.y)
        );

        // BFS nearest free cells
        var assigned = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (assigned.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();
            // terrain check
            var ctr = tilemap.GetCellCenterWorld(
                new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f) == null)
                assigned.Add(cur);

            foreach (var d in neighborDirs)
            {
                var nxt = cur + d;
                if (!cur.Within(minC, maxC)) continue;
                if (seen.Add(nxt)) queue.Enqueue(nxt);
            }
        }

        int width = maxC.x - minC.x + 1;
        int height = maxC.y - minC.y + 1;
        for (int i = 0; i < assigned.Count; i++)
        {
            var unit = movers[i];
            var goal = assigned[i];
            var field = new FlowField(minC.x, minC.y, width, height, tilemap);
            field.Generate(goal);
            unit.SetFlowField(field, goal);
        }
    }
}

// Extension for cell-in-bounds check shorthand
static class Vector2IntExtensions
{
    public static bool Within(this Vector2Int v, Vector3Int min, Vector3Int max)
    {
        return v.x >= min.x && v.x <= max.x
            && v.y >= min.y && v.y <= max.y;
    }
}
