// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your *ground* GameObject here (must have BoxCollider2D covering walkable area)")]
    public BoxCollider2D GroundCollider;

    [Tooltip("Drag the Tilemap used for cell→world conversions here")]
    public Tilemap Tilemap;

    [Tooltip("Layer(s) your obstacle GameObjects use")]
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

    void GiveMoveOrder(Vector3 worldDest)
    {
        // 1) Gather selected UnitSelector → UnitMover
        var selectors = UnitSelector.GetSelectedUnits();
        Debug.Log($"[OrderGiver] Selector found {selectors.Count} units.");

        var movers = new List<UnitMover>();
        foreach (var s in selectors)
            if (s.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);

        // fallback: if none selected, move all
        if (movers.Count == 0)
        {
            var all = FindObjectsByType<UnitMover>(FindObjectsSortMode.None);
            Debug.Log($"[OrderGiver] Fallback → {all.Length} movers.");
            movers.AddRange(all);
        }
        else Debug.Log($"[OrderGiver] Issuing to {movers.Count} movers.");

        if (movers.Count == 0) return;

        // 2) Compute grid from GroundCollider
        Bounds b = GroundCollider.bounds;
        Vector3 min = b.min, max = b.max;
        Vector3Int minCell = Tilemap.WorldToCell(min);
        Vector3Int maxCell = Tilemap.WorldToCell(max);
        int originX = minCell.x;
        int originY = minCell.y;
        int width = maxCell.x - minCell.x + 1;
        int height = maxCell.y - minCell.y + 1;
        Debug.Log($"[OrderGiver] Grid origin=({originX},{originY}), size=({width},{height})");

        // 3) Build flow field
        var field = new FlowField(originX, originY, width, height, Tilemap, ObstacleMask);

        // 4) Destination cell
        Vector3Int c3 = Tilemap.WorldToCell(worldDest);
        Vector2Int goal = new Vector2Int(c3.x, c3.y);
        Debug.Log($"[OrderGiver] Goal cell = {goal}");

        // 5) Generate & assign
        field.Generate(goal);
        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
