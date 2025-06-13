// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Tilemap your units walk on")]
    public Tilemap tilemap;

    [Tooltip("Which layers count as static obstacles")]
    public LayerMask obstacleMask;

    static readonly Vector2Int[] Neighbors = FlowField.Dirs;

    void Awake()
    {
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null)
                tilemap = g.GetComponentInChildren<Tilemap>();
        }
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
        if (tilemap == null) return;

        var sels = UnitSelector.GetSelectedUnits();
        if (sels.Count == 0) return;

        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0) return;

        // use the Tilemap's cellBounds for origin & size
        var cb = tilemap.cellBounds;
        int ox = cb.xMin, oy = cb.yMin;
        int w = cb.size.x, h = cb.size.y;

        // clamp click
        var click = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(click.x, cb.xMin, cb.xMax - 1),
            Mathf.Clamp(click.y, cb.yMin, cb.yMax - 1)
        );

        // BFS outward to assign goals
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int> { baseGoal };
        var q = new Queue<Vector2Int>();
        q.Enqueue(baseGoal);

        while (goals.Count < movers.Count && q.Count > 0)
        {
            var cur = q.Dequeue();
            var ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));

            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f, obstacleMask) == null)
                goals.Add(cur);

            foreach (var d in Neighbors)
            {
                var nxt = cur + d;
                if (!cb.Contains(new Vector3Int(nxt.x, nxt.y, 0)) || !seen.Add(nxt))
                    continue;
                q.Enqueue(nxt);
            }
        }

        // build & hand off fields
        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var mv = movers[i];
            var goal = goals[i];

            var field = new FlowField(ox, oy, w, h, tilemap, obstacleMask);
            field.Generate(goal);
            mv.SetFlowField(field);
        }
    }
}
