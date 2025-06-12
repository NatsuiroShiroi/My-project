// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;
    [Tooltip("Tilemap for conversions")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int goalCell;
    private Vector2Int lastCell, currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private bool isMoving = false;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public static void ClearReservations() { occupied.Clear(); }

    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        if (field == null)
        {
            isMoving = false;
            return;
        }
        flowField = field; goalCell = goal; isMoving = true;
        var c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (!isMoving || tilemap == null) return;

        // Snap & stop if at final goal
        if (currentCell == goalCell)
        {
            var ex = tilemap.GetCellCenterWorld(new Vector3Int(goalCell.x, goalCell.y, 0));
            rb.MovePosition(new Vector2(ex.x, ex.y));
            isMoving = false;
            return;
        }

        // Arrived at nextCell center?
        var nc3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var nc2 = new Vector2(nc3.x, nc3.y);
        if ((rb.position - nc2).sqrMagnitude < 0.001f)
        {
            occupied.Remove(lastCell);
            lastCell = currentCell;
            currentCell = nextCell;
        }

        // Pick new nextCell if needed
        if (nextCell == currentCell)
        {
            // Gather neighbors sorted by cost ascending
            var neighborCosts = new List<(Vector2Int dir, float cost)>();
            foreach (var d in FlowField.Dirs)
            {
                var cand = currentCell + d;
                if (!flowField.IsCellInBounds(cand)) continue;

                // terrain block
                var ctr3 = tilemap.GetCellCenterWorld(new Vector3Int(cand.x, cand.y, 0));
                if (Physics2D.OverlapBox(ctr3, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;

                // corner‐cut prevention
                if (d.x != 0 && d.y != 0)
                {
                    var a1 = new Vector3Int(currentCell.x + d.x, currentCell.y, 0);
                    var a2 = new Vector3Int(currentCell.x, currentCell.y + d.y, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a1), tilemap.cellSize * 0.9f, 0f) != null
                     || Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a2), tilemap.cellSize * 0.9f, 0f) != null)
                        continue;
                }

                neighborCosts.Add((d, flowField.GetCost(cand)));
            }

            // sort by cost ascending
            neighborCosts.Sort((a, b) => a.cost.CompareTo(b.cost));
            Vector2Int best = currentCell;
            foreach (var (d, cVal) in neighborCosts)
            {
                var cand = currentCell + d;
                if (occupied.Contains(cand))
                    continue;    // skip occupied
                best = cand;
                break;
            }

            // if no valid neighbor => stop
            if (best == currentCell)
            {
                isMoving = false;
                return;
            }

            occupied.Add(best);
            nextCell = best;
        }

        // Step toward nextCell center
        var tgt3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var tgt2 = new Vector2(tgt3.x, tgt3.y);
        var delta = tgt2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + delta.normalized * Mathf.Min(step, delta.magnitude));
    }
}
