// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your Grid→Tilemap here")]
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
        // 1) Gather selected units
        var selectors = UnitSelector.GetSelectedUnits();
        Debug.Log($"[OrderGiver] Selector found {selectors.Count} units.");

        // 2) Map to movers
        var movers = new List<UnitMover>();
        foreach (var sel in selectors)
            if (sel.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);

        // 3) Fallback: if none selected, grab all movers
        if (movers.Count == 0)
        {
            var all = FindObjectsByType<UnitMover>(FindObjectsSortMode.None);
            Debug.Log($"[OrderGiver] No selection—falling back to {all.Length} movers.");
            movers.AddRange(all);
        }
        else
        {
            Debug.Log($"[OrderGiver] Issuing orders to {movers.Count} movers.");
        }

        if (movers.Count == 0) return;

        // 4) Compute goal cell
        Vector3Int c3 = Tilemap.WorldToCell(worldDestination);
        Vector2Int goal = new Vector2Int(c3.x, c3.y);
        Debug.Log($"[OrderGiver] Goal cell = {goal}");

        // 5) Build & generate flow field
        var field = new FlowField(Tilemap, ObstacleMask);
        field.Generate(goal);
        Debug.Log("[OrderGiver] FlowField generated.");

        // 6) Hand it off
        foreach (var u in movers)
        {
            u.SetFlowField(field, goal);
        }
    }
}
