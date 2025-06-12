// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your Grid→Tilemap here")]
    public Tilemap Tilemap;

    [Tooltip("Select the layer your obstacle GameObjects sit on")]
    public LayerMask ObstacleMask;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            GiveMoveOrder(wp);
        }
    }

    void GiveMoveOrder(Vector3 worldDestination)
    {
        // grab selected units
        var selectors = UnitSelector.GetSelectedUnits();
        Debug.Log($"[OrderGiver] Selector found {selectors.Count} units.");

        var movers = new List<UnitMover>(selectors.Count);
        foreach (var s in selectors)
            if (s.TryGetComponent<UnitMover>(out var m))
                movers.Add(m);

        // fallback: grab all if none selected
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

        // world → cell
        Vector3Int c3 = Tilemap.WorldToCell(worldDestination);
        Vector2Int goal = new Vector2Int(c3.x, c3.y);

        // build & generate flow field
        var field = new FlowField(Tilemap, ObstacleMask);
        field.Generate(goal);

        // hand it off
        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
