// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap used for World↔Cell conversions")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int goalCell;

    // Track which cell we're on and which we're moving to
    private Vector2Int lastCell, nextCell;
    private static HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Called once when a new order arrives.
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        flowField = field;
        goalCell = goal;
        lastCell = new Vector2Int(int.MinValue, int.MinValue);
        nextCell = goal; // placeholder
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null)
            return;

        // 1) Figure out which cell we're actually in
        Vector2 worldPos = rb.position;
        Vector3Int c3 = tilemap.WorldToCell(worldPos);
        Vector2Int curCell = new Vector2Int(c3.x, c3.y);

        // 2) If we've reached our final goal, stop
        if (curCell == goalCell)
            return;

        // 3) When we enter a brand‐new cell, release the old and reset nextCell
        if (curCell != lastCell)
        {
            occupiedCells.Remove(lastCell);
            lastCell = curCell;
            nextCell = curCell;
        }

        // 4) If we need a fresh “next cell,” pick it via the flow field
        if (nextCell == curCell)
        {
            Vector2 dir = flowField.GetDirection(curCell);
            if (dir == Vector2.zero)
                return; // no path

            // Round to a cardinal neighbor
            int dx = Mathf.RoundToInt(dir.x);
            int dy = Mathf.RoundToInt(dir.y);
            Vector2Int candidate = curCell + new Vector2Int(dx, dy);

            // If occupied, wait here
            if (occupiedCells.Contains(candidate))
                return;

            // Reserve it and move on
            occupiedCells.Add(candidate);
            nextCell = candidate;
        }

        // 5) Move straight toward the center of nextCell
        Vector3Int target3 = new Vector3Int(nextCell.x, nextCell.y, 0);
        Vector3 targetWorld3 = tilemap.GetCellCenterWorld(target3);

        // **Fix: cast to Vector2 before subtracting Vector2 worldPos**
        Vector2 targetWorld2 = new Vector2(targetWorld3.x, targetWorld3.y);
        Vector2 toTarget = targetWorld2 - worldPos;

        float step = MoveSpeed * Time.fixedDeltaTime;
        if (toTarget.magnitude <= step)
        {
            // Snap to center if we can reach it this frame
            rb.MovePosition(targetWorld2);
        }
        else
        {
            rb.MovePosition(worldPos + toTarget.normalized * step);
        }
    }
}
