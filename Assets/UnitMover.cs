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

    /// <summary>Called for each unit: sets up its static path.</summary>
    public void SetPath(List<Vector2Int> newPath)
    {
        if (newPath == null || newPath.Count < 2)
        {
            isMoving = false;
            return;
        }
        path = newPath;
        pathIndex = 1;  // we start moving toward path[1]
        isMoving = true;
    }

    void FixedUpdate()
    {
        if (!isMoving || tilemap == null) return;

        // 1) If we've reached the final cell, snap & stop
        if (pathIndex >= path.Count)
        {
            var end = path[path.Count - 1];
            var ex3 = tilemap.GetCellCenterWorld(new Vector3Int(end.x, end.y, 0));
            rb.MovePosition(new Vector2(ex3.x, ex3.y));
            isMoving = false;
            return;
        }

        // 2) Target the next waypoint
        var cell = path[pathIndex];
        var target3 = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        Vector2 target2 = new Vector2(target3.x, target3.y);
        Vector2 toTarget = target2 - rb.position;

        // 3) Compute simple separation from nearby units
        Vector2 sep = Vector2.zero;
        var hits = Physics2D.OverlapCircleAll(rb.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody != rb)
            {
                Vector2 away = rb.position - (Vector2)hit.attachedRigidbody.position;
                sep += away.normalized / (away.magnitude + 0.1f);
            }
        }

        // 4) Combine heading + separation
        Vector2 desired = (toTarget.normalized + sep * 0.5f).normalized;

        // 5) Step, and if we reach the waypoint this frame, advance index
        float step = MoveSpeed * Time.fixedDeltaTime;
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
