// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;
    private static readonly Vector2Int[] neighborDirs = {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(1,-1),
        new Vector2Int(-1,1),new Vector2Int(-1,-1)
    };

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
            wp.z = 0;
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

        var b = groundSprite.bounds;
        var minC = tilemap.WorldToCell(b.min);
        var maxC = tilemap.WorldToCell(b.max);
        int minX = minC.x, minY = minC.y, maxX = maxC.x, maxY = maxC.y;

        var click3 = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(click3.x, minX, maxX),
            Mathf.Clamp(click3.y, minY, maxY)
        );

        // BFS to gather nearest free cells
        var assigned = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var goals = new List<Vector2Int>();
        queue.Enqueue(baseGoal); seen.Add(baseGoal);
        while (goals.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();
            // skip terrain
            var ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f) == null)
            {
                goals.Add(cur);
                assigned.Add(cur);
            }
            // enqueue neighbors
            foreach (var d in neighborDirs)
            {
                var nxt = cur + d;
                if (nxt.x < minX || nxt.x > maxX || nxt.y < minY || nxt.y > maxY) continue;
                if (seen.Add(nxt)) queue.Enqueue(nxt);
            }
        }

        int width = maxX - minX + 1, height = maxY - minY + 1;
        for (int i = 0; i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];
            var field = new FlowField(minX, minY, width, height, tilemap);
            field.Generate(goal);
            unit.SetFlowField(field, goal);
        }
    }
}
