// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for cell↔world conversions (auto-find if blank)")]
    public Tilemap tilemap;

    // shared occupancy: which cells are taken
    private static readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    // this unit’s path and index
    private List<Vector2Int> path;
    private int pathIndex;

    void Awake()
    {
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null) tilemap = g.GetComponentInChildren<Tilemap>();
            if (tilemap == null) Debug.LogError($"[UnitMover:{name}] No Grid→Tilemap found!");
        }
    }

    /// <summary>Assigns a discrete center‐to‐center path.</summary>
    public void SetPath(List<Vector2Int> newPath)
    {
        // free previous reservation
        if (path != null && pathIndex > 0 && pathIndex - 1 < path.Count)
            occupied.Remove(path[pathIndex - 1]);

        path = newPath;
        pathIndex = 1;

        if (path != null && path.Count > 0)
        {
            // snap to start cell and reserve it
            var start = path[0];
            transform.position = tilemap.GetCellCenterWorld(
                new Vector3Int(start.x, start.y, 0));
            occupied.Add(start);
        }
    }

    void Update()
    {
        if (path == null || pathIndex >= path.Count) return;

        var targetCell = path[pathIndex];
        // block if occupied
        if (occupied.Contains(targetCell)) return;

        var targetPos = tilemap.GetCellCenterWorld(
            new Vector3Int(targetCell.x, targetCell.y, 0));

        // move smoothly
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            MoveSpeed * Time.deltaTime);

        // arrived?
        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            // free old cell
            var prev = path[pathIndex - 1];
            occupied.Remove(prev);
            // claim new
            occupied.Add(targetCell);
            pathIndex++;
        }
    }
}
