using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your ground‐covering Tilemap here")]
    public Tilemap Tilemap;

    [Tooltip("Layer mask for obstacle physics colliders")]
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
        var selectors = UnitSelector.GetSelectedUnits();
        Debug.Log($"[OrderGiver] Selector found {selectors.Count} units.");

        var movers = new List<UnitMover>();
        foreach (var sel in selectors)
            if (sel.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);

        if (movers.Count == 0)
        {
            var all = FindObjectsByType<UnitMover>(FindObjectsSortMode.None);
            Debug.Log($"[OrderGiver] Fallback to {all.Length} movers.");
            movers.AddRange(all);
        }
        else Debug.Log($"[OrderGiver] Issuing orders to {movers.Count} movers.");

        if (movers.Count == 0) return;

        Vector3Int c3 = Tilemap.WorldToCell(worldDestination);
        Vector2Int goal = new Vector2Int(c3.x, c3.y);
        Debug.Log($"[OrderGiver] Goal cell = {goal}");

        var field = new FlowField(Tilemap, ObstacleMask);
        field.Generate(goal);

        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
