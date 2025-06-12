// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    public float MoveSpeed = 3f;
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int goalCell;
    private Vector2Int currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public static void ClearReservations() => occupied.Clear();

    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        flowField = field;
        goalCell = goal;
        var wp = rb.position;
        var c3 = tilemap.WorldToCell(wp);
        currentCell = new Vector2Int(c3.x, c3.y);
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null) return;
        if (currentCell == goalCell) return;

        // Snap to center & update currentCell
        Vector3 center3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 center2 = new Vector2(center3.x, center3.y);
        if ((rb.position - center2).sqrMagnitude < 0.01f)
        {
            currentCell = nextCell;
        }

        // Choose nextCell when needed
        if (nextCell == currentCell)
        {
            Vector2 dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero) return;

            // Score all 8 neighbors by alignment with dir
            var bestScore = float.NegativeInfinity;
            Vector2Int bestCell = currentCell;
            foreach (var d in new[] {
                Vector2Int.right, Vector2Int.left,
                Vector2Int.up, Vector2Int.down,
                new Vector2Int(1,1), new Vector2Int(1,-1),
                new Vector2Int(-1,1), new Vector2Int(-1,-1)
            })
            {
                var cand = currentCell + d;
                // bounds check
                var candCenter3 = tilemap.GetCellCenterWorld(new Vector3Int(cand.x, cand.y, 0));
                // obstacle check
                if (Physics2D.OverlapBox(candCenter3, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;
                // free check
                if (occupied.Contains(cand))
                    continue;

                // alignment score
                float score = Vector2.Dot(d.normalized, dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cand;
                }
            }
            // reserve & set
            if (bestCell != currentCell)
            {
                occupied.Add(bestCell);
                nextCell = bestCell;
            }
        }

        // Move toward nextCell center
        Vector3 t3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 tgt2 = new Vector2(t3.x, t3.y);
        Vector2 delta = tgt2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + delta.normalized * Mathf.Min(step, delta.magnitude));
    }
}
