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
        // Auto‐find your Grid → Tilemap
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
            Debug.LogError("[OrderGiver] Could not find Grid → Tilemap!");

        // Auto‐find your Ground sprite
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
        if (groundSprite == null)
            Debug.LogError("[OrderGiver] Could not find Ground → SpriteRenderer!");
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

        // --- CLEAR ALL CELL RESERVATIONS FROM PREVIOUS ORDER ---
        UnitMover.ClearReservations();

        // 1) Gather selected units (fallback to all)
        var selectors = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var sel in selectors)
            if (sel.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);

        if (movers.Count == 0)
        {
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        }

        if (movers.Count == 0)
            return;

        // 2) Derive grid bounds from Ground.sprite
        Bounds b = groundSprite.bounds;
        Vector3Int min = tilemap.WorldToCell(b.min);
        Vector3Int max = tilemap.WorldToCell(b.max);
        int originX = min.x;
        int originY = min.y;
        int width = max.x - min.x + 1;
        int height = max.y - min.y + 1;

        // 3) Create and fill the flow field
        var field = new FlowField(originX, originY, width, height, tilemap);

        // 4) Compute the goal cell
        Vector3Int c3 = tilemap.WorldToCell(worldDest);
        Vector2Int goalCell = new Vector2Int(c3.x, c3.y);

        // 5) Generate and hand off
        field.Generate(goalCell);
        foreach (var u in movers)
            u.SetFlowField(field, goalCell);
    }
}
