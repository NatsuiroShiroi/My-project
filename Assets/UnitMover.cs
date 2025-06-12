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

    [Tooltip("Separation radius in world units")]
    public float SeparationRadius = 0.75f;

    [Tooltip("Weight of separation steering (0 = ignore, 1 = full separation)")]
    [Range(0f, 1f)]
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
    /// Sets the static path for this unit to follow, cell-to-cell.
    /// </summary>
    public void SetPath(List<Vector2Int> newPath)
    {
        path = newPath;
        if (path != null && path.Count >= 2)
        {
            pathIndex = 1;  // start moving toward the second point
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

        // 1) If at final waypoint, snap & stop
        if (pathIndex >= path.Count)
        {
            var end = path[path.Count - 1];
            Vector3 endWorld = tilemap.GetCellCenterWorld(new Vector3Int(end.x, end.y, 0));
            rb.MovePosition(new Vector2(endWorld.x, endWorld.y));
            isMoving = false;
            return;
        }

        // 2) Compute target direction to next waypoint
        var cell = path[pathIndex];
        Vector3 targetWorld3 = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        Vector2 targetWorld2 = new Vector2(targetWorld3.x, targetWorld3.y);
        Vector2 toTarget = targetWorld2 - rb.position;

        // 3) Compute separation steering
        Vector2 sep = Vector2.zero;
        int neighborCount = 0;
        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, SeparationRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody != rb)
            {
                Vector2 diff = rb.position - (Vector2)hit.attachedRigidbody.position;
                float dist = diff.magnitude;
                if (dist > 0f && dist < SeparationRadius)
                {
                    // weighted by inverse distance
                    sep += diff.normalized * ((SeparationRadius - dist) / SeparationRadius);
                    neighborCount++;
                }
            }
        }
        if (neighborCount > 0)
        {
            sep /= neighborCount;
            sep = sep.normalized;
        }

        // 4) Blend heading and separation
        Vector2 heading = toTarget.normalized;
        Vector2 desired = (heading * (1f - SeparationWeight) + sep * SeparationWeight).normalized;

        // 5) Move, advance waypoint on arrival
        float step = MoveSpeed * Time.fixedDeltaTime;
        if (toTarget.magnitude <= step)
        {
            rb.MovePosition(targetWorld2);
            pathIndex++;
        }
        else
        {
            rb.MovePosition(rb.position + desired * step);
        }
    }
}
