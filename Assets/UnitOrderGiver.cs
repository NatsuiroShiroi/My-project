// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Tilemap units walk on")]
    public Tilemap tilemap;

    [Tooltip("Which layers count as static obstacles")]
    public LayerMask obstacleMask;

    void Awake()
    {
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null) tilemap = g.GetComponentInChildren<Tilemap>();
            if (tilemap == null) Debug.LogError("[OrderGiver] No Grid→Tilemap!");
        }
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
        if (tilemap == null) return;

        var sels = UnitSelector.GetSelectedUnits();
        if (sels.Count == 0) return;

        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0) return;

        // bounds from Ground sprite
        var groundSR = GameObject.Find("Ground")?.GetComponent<SpriteRenderer>();
        if (groundSR == null) { Debug.LogError("[OrderGiver] No Ground SpriteRenderer!"); return; }
        Bounds gb = groundSR.bounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        var mn = gb.min + half;
        var mx = gb.max - half;
        var minC = tilemap.WorldToCell(mn);
        var maxC = tilemap.WorldToCell(mx);
        int ox = minC.x, oy = minC.y;
        int w = maxC.x - minC.x + 1;
        int h = maxC.y - minC.y + 1;

        Vector3Int click = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(click.x, minC.x, maxC.x),
            Mathf.Clamp(click.y, minC.y, maxC.y)
        );

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
            foreach (var d in FlowField.Dirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y) continue;
                if (seen.Add(nxt)) q.Enqueue(nxt);
            }
        }

        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];
            var field = new FlowField(ox, oy, w, h, tilemap, obstacleMask);
            field.Generate(goal);
            var path = field.GetPath(
                new Vector2Int(
                    tilemap.WorldToCell(unit.transform.position).x,
                    tilemap.WorldToCell(unit.transform.position).y
                ), goal
            );
            unit.SetPath(path);
        }
    }
}
