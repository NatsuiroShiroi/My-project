using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class UnitOrderGiver : MonoBehaviour
{
    public Tilemap Tilemap; // Assigned in Inspector

    public List<UnitMover> SelectedUnits = new List<UnitMover>();

    public void GiveMoveOrder(Vector2 destination)
    {
        // Convert destination to grid cell
        Vector3Int cell = Tilemap.WorldToCell(destination);
        Vector2Int gridCell = new Vector2Int(cell.x, cell.y);

        // Get map size (adjust as needed)
        int width = Tilemap.size.x;
        int height = Tilemap.size.y;

        // Generate flow field
        FlowField field = new FlowField(width, height);
        field.Generate(gridCell, Tilemap);

        // Assign to each unit
        foreach (var unit in SelectedUnits)
        {
            unit.SetFlowField(field, gridCell);
        }
    }
}
