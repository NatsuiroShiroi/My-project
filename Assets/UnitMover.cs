// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Reference your Grid→Tilemap here")]
    public Tilemap Tilemap;

    // Shared occupancy set prevents two units stepping into the same cell
    private static HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int targetCell;
    private Vector2Int lastCell;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Called by your order‐giver to hand this unit its pathing field
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int dest)
    {
        flowField = field;
        targetCell = dest;
    }

    void FixedUpdate()
    {
        if (flowField == null || Tilemap == null)
            return;

        Vector2 worldPos = rb.position;
        Vector3Int cell3 = Tilemap.WorldToCell(worldPos);
        var curCell = new Vector2Int(cell3.x, cell3.y);

        // 1) Claim occupancy on entering a new cell
        if (curCell != lastCell)
        {
            occupiedCells.Remove(lastCell);
            if (occupiedCells.Contains(curCell))
                return;          // someone else is here; wait
            occupiedCells.Add(curCell);
            lastCell = curCell;
        }

        // 2) If at destination, stop
        if (curCell == targetCell)
            return;

        // 3) Sample flow‐field gradient
        Vector2 dir = flowField.GetDirection(curCell);
        if (dir == Vector2.zero)
            return;

        // 4) Simple local separation (diagonal dodge)
        Vector2 sep = Vector2.zero;
        foreach (var hit in Physics2D.OverlapCircleAll(worldPos, 0.5f))
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody != rb)
                sep += (worldPos - (Vector2)hit.attachedRigidbody.position);
        }

        // 5) Combine steering and move
        Vector2 move = (dir + sep.normalized * 0.7f).normalized
                       * MoveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(worldPos + move);
    }
}
