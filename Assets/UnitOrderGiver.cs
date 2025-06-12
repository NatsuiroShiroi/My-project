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
        // Auto-find Grid→Tilemap
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
            Debug.LogError("[OrderGiver] Could not find Grid→Tilemap!");

        // Auto-find Ground sprite
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
        if (groundSprite == null)
            Debug.LogError("[OrderGiver] Could not find Ground→SpriteRenderer!");
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

        // --- 0) Clear previous unit reservations ---
        UnitMover.ClearReservations();

        // --- 1) Gather movers ---
        var sels = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in sels)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0) return;

        // --- 2) Compute ground bounds in cell coords (min,max) ---
        Bounds b = groundSprite.bounds;
        Vector3Int minC = tilemap.WorldToCell(b.min);
        Vector3Int maxC = tilemap.WorldToCell(b.max);
        int minX = minC.x, minY = minC.y;
        int maxX = maxC.x, maxY = maxC.y;

        // --- 3) Clamp clicked destination to ground ---
        Vector3Int clickedCell3 = tilemap.WorldToCell(worldDest);
        int goalX = Mathf.Clamp(clickedCell3.x, minX, maxX);
        int goalY = Mathf.Clamp(clickedCell3.y, minY, maxY);
        Vector2Int baseGoal = new Vector2Int(goalX, goalY);

        // --- 4) Assign each unit its own target cell ---
        var assigned = new HashSet<Vector2Int>();
        // Always reserve the base goal first if free
        if (!assigned.Contains(baseGoal))
            assigned.Add(baseGoal);

        for (int i = 0; i < movers.Count; i++)
        {
            var unit = movers[i];
            Vector2Int target = baseGoal;

            if (i > 0)
            {
                // find nearest free neighbor around baseGoal
                float bestDist = float.MaxValue;
                Vector2Int best = baseGoal;
                foreach (var d in new[]{
                    Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down,
                    new Vector2Int(1,1), new Vector2Int(1,-1),
                    new Vector2Int(-1,1), new Vector2Int(-1,-1)
                })
                {
                    var cand = baseGoal + d;
                    // check within bounds
                    if (cand.x < minX || cand.x > maxX || cand.y < minY || cand.y > maxY)
                        continue;
                    // skip if already assigned
                    if (assigned.Contains(cand))
                        continue;
                    // skip if obstacle
                    Vector3Int cand3 = new Vector3Int(cand.x, cand.y, 0);
                    Vector3 worldCenter = tilemap.GetCellCenterWorld(cand3);
                    if (Physics2D.OverlapBox(worldCenter, tilemap.cellSize * 0.9f, 0f) != null)
                        continue;
                    // pick closest
                    float dist = d.sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = cand;
                    }
                }
                target = best;
                assigned.Add(target);
            }

            // --- 5) Build & assign a flow field for this unit’s target ---
            int originX = minX, originY = minY;
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            var field = new FlowField(originX, originY, width, height, tilemap);
            field.Generate(target);
            unit.SetFlowField(field, target);
        }
    }
}
