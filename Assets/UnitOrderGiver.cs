public Tilemap Tilemap; // assigned in Inspector

public void GiveMoveOrder(Vector2 destination)
{
    // Convert world destination to grid cell
    Vector3Int cell = Tilemap.WorldToCell(destination);
    Vector2Int gridCell = new Vector2Int(cell.x, cell.y);

    // Calculate map size based on Tilemap bounds (can adjust as needed)
    int width = Tilemap.size.x;
    int height = Tilemap.size.y;

    // Generate flow field
    FlowField field = new FlowField(width, height);
    field.Generate(gridCell, Tilemap);

    // Assign flow field to all selected units
    foreach (var unit in FindObjectsOfType<UnitMover>()) // or your selection logic
    {
        unit.SetFlowField(field, gridCell);
    }
}
