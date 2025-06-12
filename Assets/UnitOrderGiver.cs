// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your *ground* Tilemap here (must cover the whole playable grid)")]
    public Tilemap Tilemap;

    [Tooltip("Layer of your obstacle GameObjects")]
    public LayerMask ObstacleMask;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            GiveMoveOrder(wp);
        }
    }

    void GiveMoveOrder(Vector3 worldDestination)
    {
        // -- 1) Gather movers --
        var selectors = UnitSelector.GetSelectedUnits();
        Debug.Log($"[OrderGiver] Selector found {selectors.Count} units.");

        var movers = new List<UnitMover>();
        foreach (var s in selectors)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);

        if (movers.Count == 0)
        {
            var all = FindObjectsByType<UnitMover>(FindObjectsSortMode.None);
            Debug.Log($"[OrderGiver] Fallback to {all.Length} movers.");
            movers.AddRange(all);
        }
        else Debug.Log($"[OrderGiver] Issuing orders to {movers.Count} movers.");

        if (movers.Count == 0) return;

        // -- 2) Compute grid parameters from the Tilemap component --
        int originX = Tilemap.origin.x;
        int originY = Tilemap.origin.y;
        int width = Tilemap.size.x;
        int height = Tilemap.size.y;
        Debug.Log($"[OrderGiver] Grid origin=({originX},{originY}), size=({width},{height})");

        // -- 3) Tell the FlowField its grid and obstacles --
        var field = new FlowField(originX, originY, width, height, Tilemap, ObstacleMask);

        // -- 4) Convert world click → cell coords --
        Vector3Int c3 = Tilemap.WorldToCell(worldDestination);
        Vector2Int goal = new Vector2Int(c3.x, c3.y);
        Debug.Log($"[OrderGiver] Goal cell = {goal}");

        // -- 5) Generate & hand out the field --
        field.Generate(goal);
        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
