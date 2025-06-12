// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for cell↔world conversions (auto-found if blank)")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private List<Vector2Int> path;
    private int pathIndex;
    private bool isMoving = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null)
                tilemap = g.GetComponentInChildren<Tilemap>();
        }
        if (tilemap == null)
            Debug.LogError($"[UnitMover:{name}] Tilemap not assigned!");
    }

    /// <summary>
    /// Assigns a precomputed, cell‐to‐cell path for this unit to follow.
    /// </summary>
    public void SetPath(List<Vector2Int> newPath)
    {
        path = newPath;
        if (path != null && path.Count >= 2)
        {
            pathIndex = 1;  // start heading toward path[1]
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    void FixedUpdate()
    {
        if (!isMoving || tilemap == null || path == null)
            return;

        // ───── Skip any occupied waypoint(s) ─────
        // This lets you jump diagonally over dynamic blockage
        while (pathIndex < path.Count - 1 && IsNextCellOccupied(path[pathIndex]))
            pathIndex++;

        // ───── Snap & stop when you reach the final cell ─────
        if (pathIndex >= path.Count)
        {
            var end = path[path.Count - 1];
            Vector3 endWorld = tilemap.GetCellCenterWorld(
                new Vector3Int(end.x, end.y, 0));
            rb.MovePosition(new Vector2(endWorld.x, endWorld.y));
            isMoving = false;
            return;
        }

        // ───── Move toward the current waypoint ─────
        var cell = path[pathIndex];
        Vector3 target3 = tilemap.GetCellCenterWorld(
            new Vector3Int(cell.x, cell.y, 0));
        Vector2 target2 = new Vector2(target3.x, target3.y);
        Vector2 delta = target2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;

        if (delta.magnitude <= step)
        {
            rb.MovePosition(target2);
            pathIndex++;
        }
        else
        {
            rb.MovePosition(rb.position + delta.normalized * step);
        }
    }

    /// <summary>
    /// Returns true if another UnitMover currently occupies that cell.
    /// </summary>
    private bool IsNextCellOccupied(Vector2Int cell)
    {
        var center = tilemap.GetCellCenterWorld(
            new Vector3Int(cell.x, cell.y, 0));
        // Check for any overlapping UnitMover (skip static terrain colliders)
        var hits = Physics2D.OverlapCircleAll(center, tilemap.cellSize.x * 0.4f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<UnitMover>(out var other) && other != this)
                return true;
        }
        return false;
    }
}
