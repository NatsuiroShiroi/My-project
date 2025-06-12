using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your Grid→Tilemap here")]
    public Tilemap Tilemap;

    void Update()
    {
        // Right-click = order move
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            GiveMoveOrder(wp);
        }
    }

    void GiveMoveOrder(Vector3 worldDestination)
    {
        // 1) Get selectors & their movers
        var selectors = UnitSelector.GetSelectedUnits(); // your existing API
        if (selectors.Count == 0) return;

        List<UnitMover> movers = new List<UnitMover>(selectors.Count);
        foreach (var s in selectors)
            if (s.TryGetComponent<UnitMover>(out var m))
                movers.Add(m);

        if (movers.Count == 0) return;

        // 2) Build goal cell & flow field
        Vector3Int c3 = Tilemap.WorldToCell(worldDestination);
        Vector2Int goal = new Vector2Int(c3.x, c3.y);

        var field = new FlowField(Tilemap);
        field.Generate(goal, Tilemap);

        // 3) Hand it off
        foreach (var u in movers)
            u.SetFlowField(field, goal);
    }
}
