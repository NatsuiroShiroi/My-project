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

    private Vector2Int lastCell, currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public static void ClearReservations()
    {
        occupied.Clear();
    }

    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        if (field == null)
        {
            flowField = null;
            return;
        }

        flowField = field;
        goalCell = goal;

        var c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;

        // Reserve starting cell and the goal cell immediately
        occupied.Add(currentCell);
        if (!occupied.Contains(goalCell))
            occupied.Add(goalCell);
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null)
            return;

        if (currentCell == goalCell)
        {
            var ex3 = tilemap.GetCellCenterWorld(new Vector3Int(goalCell.x, goalCell.y, 0));
            rb.MovePosition(new Vector2(ex3.x, ex3.y));
            flowField = null;
            return;
        }

        var nextCenter = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var nextPos = new Vector2(nextCenter.x, nextCenter.y);
        if ((rb.position - nextPos).sqrMagnitude < 0.001f)
        {
            occupied.Remove(lastCell);
            lastCell = currentCell;
            currentCell = nextCell;
        }

        if (nextCell == currentCell)
        {
            Vector2 dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero)
            {
                flowField = null;
                return;
            }

            float bestScore = float.NegativeInfinity;
            Vector2Int bestCell = currentCell;

            foreach (var d in new[]{
                Vector2Int.right, Vector2Int.left,
                Vector2Int.up,    Vector2Int.down,
                new Vector2Int(1,1), new Vector2Int(1,-1),
                new Vector2Int(-1,1), new Vector2Int(-1,-1)
            })
            {
                var cand = currentCell + d;
                if (!flowField.IsCellInBounds(cand)) continue;

                var candCenter3 = tilemap.GetCellCenterWorld(new Vector3Int(cand.x, cand.y, 0));
                bool blocked = Physics2D.OverlapBox(candCenter3, tilemap.cellSize * 0.9f, 0f) != null;

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

                Vector2 candDir = new Vector2(d.x, d.y).normalized;
                float score = Vector2.Dot(candDir, dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cand;
                }
            }

            if (bestCell == currentCell)
            {
                flowField = null;
                return;
            }

            occupied.Add(bestCell);
            nextCell = bestCell;
        }

        var target3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var target2 = new Vector2(target3.x, target3.y);
        var delta = target2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + delta.normalized * Mathf.Min(step, delta.magnitude));
    }
}
