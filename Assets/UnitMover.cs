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

    // Track last, current, and next reserved cells
    private Vector2Int lastCell, currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Clear all cell reservations at the start of a new order.
    /// </summary>
    public static void ClearReservations()
    {
        occupied.Clear();
    }

    /// <summary>
    /// Assigns a new flow field and goal. Pass null to cancel any movement.
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        if (field == null)
        {
            // Cancel any ongoing movement
            flowField = null;
            return;
        }

        flowField = field;
        goalCell = goal;

        // Initialize cell tracking and reserve starting cell
        Vector3Int c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null)
            return;

        // 1) If reached goal, snap to its center exactly and stop
        if (currentCell == goalCell)
        {
            Vector3 exact3 = tilemap.GetCellCenterWorld(new Vector3Int(goalCell.x, goalCell.y, 0));
            rb.MovePosition(new Vector2(exact3.x, exact3.y));
            flowField = null;
            return;
        }

        // 2) When at nextCell center, advance and free the last reservation
        Vector3 nextCtr3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 nextCtr2 = new Vector2(nextCtr3.x, nextCtr3.y);
        if ((rb.position - nextCtr2).sqrMagnitude < 0.001f)
        {
            occupied.Remove(lastCell);
            lastCell = currentCell;
            currentCell = nextCell;
        }

        // 3) If need a new nextCell, pick best neighbor
        if (nextCell == currentCell)
        {
            Vector2 dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero)
            {
                // No valid path: cancel
                flowField = null;
                return;
            }

            float bestScore = float.NegativeInfinity;
            Vector2Int bestCell = currentCell;

            foreach (var d in new[]{
                Vector2Int.right, Vector2Int.left,
                Vector2Int.up,    Vector2Int.down,
                new Vector2Int(1, 1),   new Vector2Int(1, -1),
                new Vector2Int(-1, 1),  new Vector2Int(-1, -1)
            })
            {
                Vector2Int cand = currentCell + d;

                // Terrain blocking
                Vector3Int cand3 = new Vector3Int(cand.x, cand.y, 0);
                Vector3 ctr3 = tilemap.GetCellCenterWorld(cand3);
                bool blocked = Physics2D.OverlapBox(ctr3, tilemap.cellSize * 0.9f, 0f) != null;

                // Prevent diagonal corner-cutting
                if (!blocked && d.x != 0 && d.y != 0)
                {
                    var adj1 = new Vector3Int(currentCell.x + d.x, currentCell.y, 0);
                    var adj2 = new Vector3Int(currentCell.x, currentCell.y + d.y, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(adj1), tilemap.cellSize * 0.9f, 0f) != null ||
                        Physics2D.OverlapBox(tilemap.GetCellCenterWorld(adj2), tilemap.cellSize * 0.9f, 0f) != null)
                    {
                        blocked = true;
                    }
                }

                if (blocked || occupied.Contains(cand))
                    continue;

                Vector2 d2 = new Vector2(d.x, d.y).normalized;
                float score = Vector2.Dot(d2, dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cand;
                }
            }

            // If no progress, cancel movement
            if (bestCell == currentCell)
            {
                flowField = null;
                return;
            }

            // Reserve and set nextCell
            occupied.Add(bestCell);
            nextCell = bestCell;
        }

        // 4) Move toward nextCell center
        Vector3 tgt3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 tgt2 = new Vector2(tgt3.x, tgt3.y);
        Vector2 delta = tgt2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;

        if (delta.magnitude <= step)
            rb.MovePosition(tgt2);
        else
            rb.MovePosition(rb.position + delta.normalized * step);
    }
}
