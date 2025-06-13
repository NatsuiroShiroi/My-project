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
    [Tooltip("Radius (in world units) to look for other units")]
    public float SeparationRadius = 0.75f;

    [Range(0f, 1f), Tooltip("0 = zero separation, 1 = full separation")]
    public float SeparationWeight = 0.2f;

    Rigidbody2D rb;
    List<Vector2Int> path;
    int pathIndex;
    bool isMoving = false;

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
            Debug.LogError($"[UnitMover:{name}] no Tilemap assigned!");
    }

    /// <summary>
    /// Give the unit a precomputed, cell‐to‐cell path.
    /// </summary>
    public void SetPath(List<Vector2Int> newPath)
    {
        path = newPath;
        if (path != null && path.Count >= 2)
        {
            pathIndex = 1;  // move toward path[1]
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    void FixedUpdate()
    {
        if (!isMoving || path == null || tilemap == null)
            return;

        // Snap to final cell & stop
        if (pathIndex >= path.Count)
        {
            var end = path[path.Count - 1];
            Vector3 w = tilemap.GetCellCenterWorld(new Vector3Int(end.x, end.y, 0));
            rb.MovePosition((Vector2)w);
            isMoving = false;
            return;
        }

        // Compute target world‐position
        var cell = path[pathIndex];
        Vector3 wPos3 = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        Vector2 wPos2 = wPos3;

        // Vector toward that point
        Vector2 toTarget = (wPos2 - rb.position);
        float step = MoveSpeed * Time.fixedDeltaTime;

        // --- SEPARATION STEERING ---
        Vector2 sep = Vector2.zero;
        int count = 0;
        var hits = Physics2D.OverlapCircleAll(rb.position, SeparationRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody != rb)
            {
                if (hit.attachedRigidbody.TryGetComponent<UnitMover>(out var _))
                {
                    Vector2 diff = rb.position - hit.attachedRigidbody.position;
                    float dist = diff.magnitude;
                    if (dist > 0f && dist < SeparationRadius)
                    {
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
        Vector2 head = toTarget.normalized;
        Vector2 desired = (head * (1 - SeparationWeight) + sep * SeparationWeight).normalized;

        // Move & advance waypoint when close enough
        if (toTarget.magnitude <= step)
        {
            rb.MovePosition(wPos2);
            pathIndex++;
        }
        else
        {
            rb.MovePosition(rb.position + desired * step);
        }
    }
}
