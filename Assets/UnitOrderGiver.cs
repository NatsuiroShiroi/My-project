using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;

    void Awake()
    {
        // Auto‐find the Tilemap under "Grid"
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();

        // Auto‐find the Ground sprite
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
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

        // 1) Gather movers
        var sels = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in sels)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));
        if (movers.Count == 0) return;

        // 2) Compute grid from Ground.bounds
        Bounds b = groundSprite.bounds;
        Vector3Int minCell = tilemap.WorldToCell(b.min);
        Vector3Int maxCell = tilemap.WorldToCell(b.max);
        int originX = minCell.x;
        int originY = minCell.y;
        int width = maxCell.x - minCell.x + 1;
        int height = maxCell.y - minCell.y + 1;

        // 3) Build & generate flow field
        var field = new FlowField(originX, originY, width, height, tilemap);
        Vector3Int c3 = tilemap.WorldToCell(worldDest);
        var goal = new Vector2Int(c3.x, c3.y);
        field.Generate(goal);

        // 4) Issue to all movers
        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
