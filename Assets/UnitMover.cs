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

    // Cell stepping
    private Vector2Int lastCell, currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private bool isMoving = false;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public static void ClearReservations() => occupied.Clear();

    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        // Cancel any prior motion
        if (field == null)
        {
            CancelMovement();
            return;
        }

        flowField = field;
        goalCell = goal;
        isMoving = true;

        // Initialize cell occupancy
        var c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    private void CancelMovement()
    {
        isMoving = false;
        flowField = null;
        // Snap to exact center of currentCell
        var center3 = tilemap.GetCellCenterWorld(new Vector3Int(currentCell.x, currentCell.y, 0));
        rb.MovePosition(new Vector2(center3.x, center3.y));
    }

    void FixedUpdate()
    {
        if (!isMoving || tilemap == null)
            return;

        // 1) Arrived at goal?
        if (currentCell == goalCell)
        {
            CancelMovement();
            return;
        }

        // 2) At center of nextCell?
        var nextCenter3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var nextCenter2 = new Vector2(nextCenter3.x, nextCenter3.y);
        if ((rb.position - nextCenter2).sqrMagnitude < 0.001f)
        {
            // free old
            occupied.Remove(lastCell);
            lastCell = currentCell;
            currentCell = nextCell;
        }

        // 3) Determine new nextCell if needed
        if (nextCell == currentCell)
        {
            var dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero)
            {
                CancelMovement();
                return;
            }

            float bestScore = float.NegativeInfinity;
            Vector2Int best = currentCell;

            foreach (var d in FlowFieldDirs)
            {
                var cand = currentCell + d;
                if (!flowField.IsCellInBounds(cand)) continue;

                // terrain block
                var ctr3 = tilemap.GetCellCenterWorld(new Vector3Int(cand.x, cand.y, 0));
                bool blocked = Physics2D.OverlapBox(ctr3, tilemap.cellSize * 0.9f, 0f) != null;

                // prevent corner cutting
                if (!blocked && d.x != 0 && d.y != 0)
                {
                    var a1 = new Vector3Int(currentCell.x + d.x, currentCell.y, 0);
                    var a2 = new Vector3Int(currentCell.x, currentCell.y + d.y, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a1), tilemap.cellSize * 0.9f, 0f) != null ||
                        Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a2), tilemap.cellSize * 0.9f, 0f) != null)
                        blocked = true;
                }

                if (blocked || occupied.Contains(cand))
                    continue;

                float score = Vector2.Dot(new Vector2(d.x, d.y).normalized, dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cand;
                }
            }

            if (best == currentCell)
            {
                // no valid move
                CancelMovement();
                return;
            }

            occupied.Add(best);
            nextCell = best;
        }

        // 4) Move toward nextCell center
        var target3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var target2 = new Vector2(target3.x, target3.y);
        var delta = target2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;

        if (delta.magnitude <= step)
            rb.MovePosition(target2);
        else
            rb.MovePosition(rb.position + delta.normalized * step);
    }

    // Reuse the same 8 dirs as FlowField
    private static Vector2Int[] FlowFieldDirs => new[] {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(1,-1),
        new Vector2Int(-1,1), new Vector2Int(-1,-1)
    };
}
