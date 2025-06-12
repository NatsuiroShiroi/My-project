// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;

    void Awake()
    {
        // Auto-find the Tilemap under the "Grid" GameObject
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
        {
            tilemap = gridGO.GetComponentInChildren<Tilemap>();
        }
        if (tilemap == null)
            Debug.LogError("[OrderGiver] Could not find Grid → Tilemap!");
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

        // 1) Gather movers
        var sels = UnitSelector.GetSelectedUnits();
        var movers = new List<UnitMover>();
        foreach (var s in sels)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);

        // fallback: all units
        if (movers.Count == 0)
            movers.AddRange(FindObjectsByType<UnitMover>(FindObjectsSortMode.None));

        if (movers.Count == 0) return;

        // 2) Build flow-field
        var field = new FlowField(tilemap);
        // destination cell in grid coords
        Vector3Int c3 = tilemap.WorldToCell(worldDest);
        var goal = new Vector2Int(c3.x, c3.y);

        // 3) Generate & assign
        field.Generate(goal);
        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
