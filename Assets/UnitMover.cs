// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for World↔Cell conversions")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int goalCell;

    // Track reservations per order
    private Vector2Int currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Clear all reservations when issuing a new order.
    /// </summary>
    public static void ClearReservations()
    {
        occupied.Clear();
    }

    /// <summary>
    /// Called once when a new order comes in.
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        flowField = field;
        goalCell = goal;

        // Figure out current cell
        var wp = rb.position;
        var c3 = tilemap.WorldToCell(wp);
        currentCell = new Vector2Int(c3.x, c3.y);
        nextCell = currentCell;

        // Reserve our starting cell
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null)
            return;

        // 1) If at goal, do nothing
        if (currentCell == goalCell)
            return;

        // 2) If we have arrived at nextCell center, snap & prepare for the next hop
        Vector3 center3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 center2 = new Vector2(center3.x, center3.y);
        if (Vector2.Distance(rb.position, center2) < 0.01f)
        {
            currentCell = nextCell;
        }

        // 3) If we need a new nextCell (i.e. after snapping or at start)
        if (nextCell == currentCell)
        {
            Vector2 dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero)
                return; // no valid direction

            // pick cardinal neighbor
            int dx = Mathf.RoundToInt(dir.x);
            int dy = Mathf.RoundToInt(dir.y);
            Vector2Int candidate = currentCell + new Vector2Int(dx, dy);

            // 4) Impenetrable terrain: check for obstacle collider in candidate cell
            Vector3 candCenter3 = tilemap.GetCellCenterWorld(new Vector3Int(candidate.x, candidate.y, 0));
            bool blocked = Physics2D.OverlapBox(
                candCenter3,
                tilemap.cellSize * 0.9f, // slightly smaller than full cell
                0f
            ) != null;
            if (blocked)
                return;

            // 5) Prevent overlap: only reserve if free
            if (occupied.Contains(candidate))
                return;

            // 6) Reserve and set nextCell
            occupied.Add(candidate);
            nextCell = candidate;
        }

        // 7) Move toward nextCell center
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
