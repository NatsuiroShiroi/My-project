// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;
    private readonly Vector2Int[] neighborDirs = {
        Vector2Int.right, Vector2Int.left,
        Vector2Int.up,    Vector2Int.down,
        new Vector2Int(1, 1),   new Vector2Int( 1, -1),
        new Vector2Int(-1, 1),  new Vector2Int(-1, -1)
    };

    void Awake()
    {
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
            Debug.LogError("[OrderGiver] Missing Grid→Tilemap!");

        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
        if (groundSprite == null)
            Debug.LogError("[OrderGiver] Missing Ground→SpriteRenderer!");
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

        // 0) Clear old occupancy & any lingering flow fields
        UnitMover.ClearReservations();

        // 1) Gather selected movers (fallback to all)
        var sels = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in sels)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0) return;

        // 2) Reset every mover’s flow field (so unassigned ones stop)
        foreach (var m in movers)
            m.SetFlowField(null, default);

        // 3) Compute ground bounds in cell-space
        Bounds b = groundSprite.bounds;
        Vector3Int minC = tilemap.WorldToCell(b.min);
        Vector3Int maxC = tilemap.WorldToCell(b.max);
        int minX = minC.x, minY = minC.y;
        int maxX = maxC.x, maxY = maxC.y;

        // 4) Clamp clicked destination
        var clickCell3 = tilemap.WorldToCell(worldDest);
        int gx = Mathf.Clamp(clickCell3.x, minX, maxX);
        int gy = Mathf.Clamp(clickCell3.y, minY, maxY);
        Vector2Int baseGoal = new Vector2Int(gx, gy);

        // 5) Prepare per-unit assignments
        var assigned = new HashSet<Vector2Int>();
        var assignments = new List<(UnitMover unit, Vector2Int goal)>();

        // First unit → exact click
        assigned.Add(baseGoal);
        assignments.Add((movers[0], baseGoal));

        // Others → nearest free neighbor
        for (int i = 1; i < movers.Count; i++)
        {
            float bestDist = float.MaxValue;
            Vector2Int bestCell = default;
            foreach (var d in neighborDirs)
            {
                var cand = baseGoal + d;
                // must lie within ground
                if (cand.x < minX || cand.x > maxX || cand.y < minY || cand.y > maxY)
                    continue;
                // skip if already assigned
                if (assigned.Contains(cand))
                    continue;
                // skip if obstacle
                Vector3Int c3 = new Vector3Int(cand.x, cand.y, 0);
                Vector3 worldC = tilemap.GetCellCenterWorld(c3);
                if (Physics2D.OverlapBox(worldC, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;
                // pick nearest to baseGoal
                float dist = (cand - baseGoal).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCell = cand;
                }
            }
            // only assign if we found a neighbor
            if (bestDist < float.MaxValue)
            {
                assigned.Add(bestCell);
                assignments.Add((movers[i], bestCell));
            }
        }

        // 6) Issue each assignment with its own flow field
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        for (int i = 0; i < assignments.Count; i++)
        {
            var (unit, goal) = assignments[i];
            var field = new FlowField(minX, minY, width, height, tilemap);
            field.Generate(goal);
            unit.SetFlowField(field, goal);
        }
    }
}
