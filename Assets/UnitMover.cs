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

    [Header("Separation (units avoid each other)")]
    [Tooltip("How far to look for nearby units")]
    public float SeparationRadius = 0.6f;
    [Range(0f, 1f), Tooltip("0 = no separation, 1 = full separation")]
    public float SeparationWeight = 0.2f;

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
    /// Give this unit a precomputed, cell-to-cell path.
    /// </summary>
    public void SetPath(List<Vector2Int> newPath)
    {
        path = newPath;
        if (path != null && path.Count >= 2)
        {
            pathIndex = 1;  // start heading toward the second cell
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

        // If we've reached the last waypoint, snap & stop
        if (pathIndex >= path.Count)
        {
            var endCell = path[path.Count - 1];
            Vector3 worldEnd = tilemap.GetCellCenterWorld(
                new Vector3Int(endCell.x, endCell.y, 0));
            rb.MovePosition((Vector2)worldEnd);
            isMoving = false;
            return;
        }

        // Compute world-space target for this waypoint
        Vector2Int cell = path[pathIndex];
        Vector3 target3 = tilemap.GetCellCenterWorld(
            new Vector3Int(cell.x, cell.y, 0));
        Vector2 target2 = (Vector2)target3;

        // Vector toward the target
        Vector2 toTarget = target2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;

        // Compute separation vector from nearby units
        Vector2 sep = Vector2.zero;
        int count = 0;
        var hits = Physics2D.OverlapCircleAll(rb.position, SeparationRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody != rb)
            {
                if (hit.TryGetComponent<UnitMover>(out var _))
                {
                    Vector2 diff = rb.position - hit.attachedRigidbody.position;
                    float dist = diff.magnitude;
                    if (dist > 0f && dist < SeparationRadius)
                    {
                        // Weighted by closeness
                        sep += diff.normalized * ((SeparationRadius - dist) / SeparationRadius);
                        count++;
                    }
                }
            }
        }
        if (count > 0)
        {
            sep /= count;
            sep = sep.normalized;
        }

        // Blend heading and separation
        Vector2 heading = toTarget.normalized;
        Vector2 desired = (heading * (1f - SeparationWeight) + sep * SeparationWeight).normalized;

        // Move: if close enough, snap to cell and advance
        if (toTarget.magnitude <= step)
        {
            rb.MovePosition(target2);
            pathIndex++;
        }
        else
        {
            rb.MovePosition(rb.position + desired * step);
        }
    }
}
