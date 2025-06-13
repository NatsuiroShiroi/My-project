// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for world↔cell conversions")]
    public Tilemap tilemap;

    // The path (in grid cells) this unit will follow
    private List<Vector2Int> path;
    private int pathIndex;

    // Global occupancy: which grid cells are taken by moving units
    private static readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    // The cell this unit currently “owns”
    private Vector2Int currentCell;

    /// <summary>
    /// Assigns a precomputed path (from FlowField.GetPath) for this unit.
    /// Reserves the start cell in the occupancy set.
    /// </summary>
    public void SetPath(List<Vector2Int> newPath)
    {
        // free old reservation
        if (path != null && pathIndex > 0 && pathIndex <= path.Count)
            occupied.Remove(path[pathIndex - 1]);

        path = newPath;
        pathIndex = 1;

        if (path != null && path.Count >= 2)
        {
            // snap to start cell and reserve it
            currentCell = path[0];
            Vector3 world = tilemap.GetCellCenterWorld(
                new Vector3Int(currentCell.x, currentCell.y, 0));
            transform.position = world;
            occupied.Add(currentCell);
        }
        else
        {
            path = null;
        }
    }

    void Update()
    {
        if (path == null || pathIndex >= path.Count) return;

        // Next grid‐cell we want to move into
        Vector2Int nextCell = path[pathIndex];

        // Block if someone else is already there
        if (occupied.Contains(nextCell)) return;

        // World position of that cell
        Vector3 targetWorld = tilemap.GetCellCenterWorld(
            new Vector3Int(nextCell.x, nextCell.y, 0));

        // Move smoothly toward its center
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorld,
            MoveSpeed * Time.deltaTime);

        // When we’ve essentially arrived, snap in and advance
        if (Vector3.Distance(transform.position, targetWorld) < 0.01f)
        {
            // free our old cell
            occupied.Remove(currentCell);

            // claim the new one
            currentCell = nextCell;
            occupied.Add(currentCell);

            pathIndex++;
        }
    }
}
