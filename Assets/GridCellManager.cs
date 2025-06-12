using System.Collections.Generic;
using UnityEngine;

public class GridCellManager : MonoBehaviour
{
    public static GridCellManager Instance;

    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    void Awake() { Instance = this; }

    public bool TryReserveCell(Vector2Int cell)
    {
        if (occupiedCells.Contains(cell)) return false;
        occupiedCells.Add(cell);
        return true;
    }

    public void ReleaseCell(Vector2Int cell)
    {
        occupiedCells.Remove(cell);
    }
}
