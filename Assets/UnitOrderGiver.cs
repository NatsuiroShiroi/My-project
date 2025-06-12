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

        var c3 = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(c3.x, minX, maxX),
            Mathf.Clamp(c3.y, minY, maxY)
        );

        var assigned = new HashSet<Vector2Int>();
        var assigns = new List<(UnitMover, Vector2Int)>();

        // first unit => click
        assigned.Add(baseGoal);
        assigns.Add((movers[0], baseGoal));

        // others => nearest neighbor
        for (int i = 1; i < movers.Count; i++)
        {
            float bestDist = float.MaxValue;
            Vector2Int best = baseGoal;
            foreach (var d in neighborDirs)
            {
                var cand = baseGoal + d;
                if (cand.x < minX || cand.x > maxX || cand.y < minY || cand.y > maxY) continue;
                if (assigned.Contains(cand)) continue;
                var ctr = tilemap.GetCellCenterWorld(new Vector3Int(cand.x, cand.y, 0));
                if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f) != null) continue;
                float dist = (cand - baseGoal).sqrMagnitude;
                if (dist < bestDist) { bestDist = dist; best = cand; }
            }
            if (bestDist < float.MaxValue)
            {
                assigned.Add(best);
                assigns.Add((movers[i], best));
            }
        }

        int width = maxX - minX + 1, height = maxY - minY + 1;
        foreach (var (unit, goal) in assigns)
        {
            var field = new FlowField(minX, minY, width, height, tilemap);
            field.Generate(goal);
            unit.SetFlowField(field, goal);
        }
    }
}
