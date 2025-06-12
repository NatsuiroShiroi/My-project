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

        // 1) Gather units
        var sels = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in sels)
            if (s.TryGetComponent(out UnitMover mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));

        // 2) Compute ground bounds inset half tile
        var bs = groundSprite.bounds;
        var inset = tilemap.cellSize * 0.5f;
        var minC = tilemap.WorldToCell(bs.min + inset);
        var maxC = tilemap.WorldToCell(bs.max - inset);

        // 3) Clamp click
        var c3 = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(c3.x, minC.x, maxC.x),
            Mathf.Clamp(c3.y, minC.y, maxC.y)
        );

        // 4) BFS to assign N nearest free cells
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var q = new Queue<Vector2Int>();
        q.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (goals.Count < movers.Count && q.Count > 0)
        {
            var cur = q.Dequeue();
            // skip terrain
            var ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f) == null)
                goals.Add(cur);

            foreach (var d in FlowField.Dirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt)) q.Enqueue(nxt);
            }
        }

        // 5) Issue each unit its static path
        int width = maxC.x - minC.x + 1;
        int height = maxC.y - minC.y + 1;
        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];
            var field = new FlowField(minC.x, minC.y, width, height, tilemap);
            field.Generate(goal);

            // backtrace a static path
            var path = field.GetPath(
                new Vector2Int(
                    tilemap.WorldToCell(unit.transform.position).x,
                    tilemap.WorldToCell(unit.transform.position).y
                ),
                goal
            );
            unit.SetPath(path);
        }
    }
}
