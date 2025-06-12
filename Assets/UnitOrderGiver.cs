// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Drag your Grid→Tilemap here")]
    public Tilemap Tilemap;

    void Update()
    {
        // Right-click issues a move order
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            GiveMoveOrder(mouseWorld);
        }
    }

    void GiveMoveOrder(Vector3 worldDestination)
    {
        // 1) Grab all currently selected UnitSelectors
        var selectors = UnitSelector.GetSelectedUnits(); // List<UnitSelector>

        // 2) Convert to UnitMover list
        List<UnitMover> movers = new List<UnitMover>(selectors.Count);
        foreach (var sel in selectors)
        {
            var mover = sel.GetComponent<UnitMover>();
            if (mover != null)
                movers.Add(mover);
        }

        if (movers.Count == 0)
            return; // nothing selected

        // 3) Compute goal cell on your Tilemap
        Vector3Int c3 = Tilemap.WorldToCell(worldDestination);
        Vector2Int goalCell = new Vector2Int(c3.x, c3.y);

        // 4) Build a single FlowField for that goal
        int w = Tilemap.size.x;
        int h = Tilemap.size.y;
        FlowField field = new FlowField(w, h);
        field.Generate(goalCell, Tilemap);

        // 5) Hand it to each mover
        foreach (var u in movers)
        {
            u.SetFlowField(field, goalCell);
        }
    }
}
