// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap used for World↔Cell conversions")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int goalCell;
    private Vector2Int currentCell, nextCell;

    // Shared occupancy prevents overlap
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Clear all reservations at the start of a new order.
    /// </summary>
    public static void ClearReservations()
    {
        occupied.Clear();
    }

    /// <summary>
    /// Called once when a new move order arrives.
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        flowField = field;
        goalCell = goal;

        // Determine and reserve our starting cell
        Vector3Int c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null)
            return;

        // If we've reached the goal, stop moving
        if (currentCell == goalCell)
            return;

        // Snap to the center of nextCell if we're close enough
        Vector3 center3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 center2 = new Vector2(center3.x, center3.y);
        if ((rb.position - center2).sqrMagnitude < 0.01f)
        {
            currentCell = nextCell;
        }

        // If we need a new nextCell, pick it
        if (nextCell == currentCell)
        {
            Vector2 dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero)
                return; // no valid direction

            // Score all 8 neighbors and choose the best
            float bestScore = float.NegativeInfinity;
            Vector2Int bestCell = currentCell;

            var directions = new Vector2Int[] {
                Vector2Int.right, Vector2Int.left,
                Vector2Int.up,    Vector2Int.down,
                new Vector2Int(1, 1),   new Vector2Int(1, -1),
                new Vector2Int(-1, 1),  new Vector2Int(-1, -1)
            };

            foreach (var d in directions)
            {
                Vector2Int candidate = currentCell + d;

                // Check terrain collision
                Vector3Int cand3 = new Vector3Int(candidate.x, candidate.y, 0);
                Vector3 candCenter3 = tilemap.GetCellCenterWorld(cand3);
                if (Physics2D.OverlapBox(candCenter3, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;

                // Check unit occupancy
                if (occupied.Contains(candidate))
                    continue;

                // Compute alignment score
                Vector2 d2 = new Vector2(d.x, d.y).normalized;
                float score = Vector2.Dot(d2, dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            // Reserve and set nextCell if it's changed
            if (bestCell != currentCell)
            {
                occupied.Add(bestCell);
                nextCell = bestCell;
            }
        }

        // Move toward the center of nextCell
        Vector3 target3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 target2 = new Vector2(target3.x, target3.y);
        Vector2 delta = target2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;

        if (delta.magnitude <= step)
            rb.MovePosition(target2);
        else
            rb.MovePosition(rb.position + delta.normalized * step);
    }
}
