// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Tilemap your units walk on (auto-found if blank)")]
    public Tilemap tilemap;

    [Tooltip("BoxCollider2D on Ground defining play area (auto-found if blank)")]
    public BoxCollider2D groundCollider;

    [Tooltip("Which layers count as static obstacles")]
    public LayerMask obstacleMask;

    static readonly Vector2Int[] Neighbors = FlowField.Dirs;

    void Awake()
    {
        // auto-find tilemap
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null) tilemap = g.GetComponentInChildren<Tilemap>();
        }

        // auto-find ground collider
        if (groundCollider == null)
        {
            var ground = GameObject.Find("Ground");
            if (ground != null) groundCollider = ground.GetComponent<BoxCollider2D>();
        }

        if (tilemap == null) Debug.LogError("[OrderGiver] Missing Tilemap!");
        if (groundCollider == null) Debug.LogError("[OrderGiver] Missing Ground Collider!");
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
        if (tilemap == null || groundCollider == null) return;

        // 1) Gather selected units
        var sels = UnitSelector.GetSelectedUnits();
        if (sels.Count == 0) return;

        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0) return;

        // 2) Compute ground bounds inset half-cell
        Bounds gb = groundCollider.bounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        Vector3 worldMin = gb.min + half;
        Vector3 worldMax = gb.max - half;
        Vector3Int minCell = tilemap.WorldToCell(worldMin);
        Vector3Int maxCell = tilemap.WorldToCell(worldMax);

        int originX = minCell.x;
        int originY = minCell.y;
        int width = maxCell.x - minCell.x + 1;
        int height = maxCell.y - minCell.y + 1;

        // 3) Clamp click inside ground
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minCell.x, maxCell.x),
            Mathf.Clamp(clickC.y, minCell.y, maxCell.y)
        );

        // 4) BFS outward to pick distinct free goal cells
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int> { baseGoal };
        var q = new Queue<Vector2Int>();
        q.Enqueue(baseGoal);

        while (goals.Count < movers.Count && q.Count > 0)
        {
            var cur = q.Dequeue();
            Vector3 ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));

            // skip if static obstacle
            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f, obstacleMask) == null)
                goals.Add(cur);

            foreach (var d in Neighbors)
            {
                var nxt = cur + d;
                if (nxt.x < minCell.x || nxt.x > maxCell.x ||
                    nxt.y < minCell.y || nxt.y > maxCell.y)
                    continue;
                if (seen.Add(nxt)) q.Enqueue(nxt);
            }
        }

        // 5) Build & hand off each flow field
        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];

            var field = new FlowField(
                originX, originY,
                width, height,
                tilemap,
                obstacleMask
            );
            field.Generate(goal);
            unit.SetFlowField(field);
        }
    }
}
