// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for world↔cell conversions")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int goalCell;

    // Track current, next, and last reserved cells
    private Vector2Int lastCell, currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Clear all reservations when a new order is issued.
    /// </summary>
    public static void ClearReservations()
    {
        occupied.Clear();
    }

    /// <summary>
    /// Called once at order time.
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        // Cancel if goal is already occupied
        if (occupied.Contains(goal))
        {
            flowField = null;
            return;
        }

        flowField = field;
        goalCell = goal;

        // Initialize cell tracking
        Vector3Int c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;

        // Reserve starting cell
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null)
            return;

        // 1) If we reached the goal, snap to exact center and clear order
        if (currentCell == goalCell)
        {
            Vector3 exact3 = tilemap.GetCellCenterWorld(new Vector3Int(goalCell.x, goalCell.y, 0));
            rb.MovePosition(new Vector2(exact3.x, exact3.y));
            flowField = null;
            return;
        }

        // 2) Snap & free when arriving at nextCell
        Vector3 nextCenter3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 nextCenter2 = new Vector2(nextCenter3.x, nextCenter3.y);
        if ((rb.position - nextCenter2).sqrMagnitude < 0.001f)
        {
            // Free the last cell reservation
            occupied.Remove(lastCell);

            // Advance
            lastCell = currentCell;
            currentCell = nextCell;
        }

        // 3) Pick a new nextCell if needed
        if (nextCell == currentCell)
        {
            Vector2 dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero)
            {
                // No path forward: cancel order
                flowField = null;
                return;
            }

            // Score 8 neighbors by alignment
            float bestScore = float.NegativeInfinity;
            Vector2Int bestCell = currentCell;

            foreach (var d in new[]{
                Vector2Int.right, Vector2Int.left,
                Vector2Int.up,    Vector2Int.down,
                new Vector2Int(1,1),   new Vector2Int(1,-1),
                new Vector2Int(-1,1),  new Vector2Int(-1,-1)
            })
            {
                Vector2Int cand = currentCell + d;

                // Bounds check is implicit: flowField will give zero dir outside
                // Terrain block check:
                Vector3Int cand3 = new Vector3Int(cand.x, cand.y, 0);
                Vector3 candCenter3 = tilemap.GetCellCenterWorld(cand3);
                bool blocked = Physics2D.OverlapBox(candCenter3, tilemap.cellSize * 0.9f, 0f) != null;

                // Prevent “corner‐cutting” on diagonals
                if (!blocked && d.x != 0 && d.y != 0)
                {
                    // Must also have both cardinal neighbors free
                    Vector2Int c1 = currentCell + new Vector2Int(d.x, 0);
                    Vector2Int c2 = currentCell + new Vector2Int(0, d.y);
                    Vector3Int c13 = new Vector3Int(c1.x, c1.y, 0);
                    Vector3Int c23 = new Vector3Int(c2.x, c2.y, 0);
                    Vector3 c1world = tilemap.GetCellCenterWorld(c13);
                    Vector3 c2world = tilemap.GetCellCenterWorld(c23);
                    if (Physics2D.OverlapBox(c1world, tilemap.cellSize * 0.9f, 0f) != null ||
                        Physics2D.OverlapBox(c2world, tilemap.cellSize * 0.9f, 0f) != null)
                        blocked = true;
                }

                if (blocked)
                    continue;

                // Avoid other units
                if (occupied.Contains(cand))
                    continue;

                // Alignment scoring
                Vector2 d2 = new Vector2(d.x, d.y).normalized;
                float score = Vector2.Dot(d2, dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cand;
                }
            }

            // If no progress possible, cancel the order
            if (bestCell == currentCell)
            {
                flowField = null;
                return;
            }

            // Reserve & set nextCell
            occupied.Add(bestCell);
            nextCell = bestCell;
        }

        // 4) Move toward nextCell’s center
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
