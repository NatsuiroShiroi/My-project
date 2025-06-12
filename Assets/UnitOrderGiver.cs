// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your Grid → Tilemap here")]
    public Tilemap Tilemap;

    void Update()
    {
        // Right‐click issues move orders
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            GiveMoveOrder(mouseWorld);
        }
    }

    void GiveMoveOrder(Vector3 worldDestination)
    {
        // A) Try your real selector first:
        var selectors = UnitSelector.GetSelectedUnits();
        Debug.Log($"[OrderGiver] Selector found {selectors.Count} units.");

        // B) Map them to movers
        var movers = new List<UnitMover>(selectors.Count);
        foreach (var sel in selectors)
            if (sel.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);

        // C) Fallback: if none selected, move *all* units so you can test
        if (movers.Count == 0)
        {
            // Use the new FindObjectsByType API instead of the obsolete FindObjectsOfType
            var all = FindObjectsByType<UnitMover>(FindObjectsSortMode.None);
            Debug.Log($"[OrderGiver] No selection—falling back to {all.Length} total movers.");
            movers.AddRange(all);
        }
        else
        {
            Debug.Log($"[OrderGiver] Issuing orders to {movers.Count} selected movers.");
        }

        if (movers.Count == 0)
            return;  // truly nothing in scene!?

        // D) Compute goal cell
        Vector3Int c3 = Tilemap.WorldToCell(worldDestination);
        Vector2Int goal = new Vector2Int(c3.x, c3.y);

        // E) Build and fill the flow field
        var field = new FlowField(Tilemap);
        field.Generate(goal, Tilemap);

        // F) Hand it to each mover
        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
