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

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public static void ClearReservations() => occupied.Clear();

    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        if (field == null) { flowField = null; return; }

        flowField = field;
        goalCell = goal;
        var c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (flowField == null || tilemap == null) return;

        // 1) Arrived at goal?
        if (currentCell == goalCell)
        {
            var ex = tilemap.GetCellCenterWorld(new Vector3Int(goalCell.x, goalCell.y, 0));
            rb.MovePosition(new Vector2(ex.x, ex.y));
            flowField = null;
            return;
        }

        // 2) At nextCell center?
        var nc3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var nc2 = new Vector2(nc3.x, nc3.y);
        if ((rb.position - nc2).sqrMagnitude < 0.001f)
        {
            occupied.Remove(lastCell);
            lastCell = currentCell;
            currentCell = nextCell;
        }

        // 3) Pick nextCell if needed
        if (nextCell == currentCell)
        {
            var dir = flowField.GetDirection(currentCell);
            if (dir == Vector2.zero) { flowField = null; return; }

            float bestScore = float.NegativeInfinity;
            Vector2Int best = currentCell;

            foreach (var d in dirs)
            {
                var cand = currentCell + d;
                if (!flowField.IsCellInBounds(cand)) continue;

                // terrain block
                var c3 = new Vector3Int(cand.x, cand.y, 0);
                var cent = tilemap.GetCellCenterWorld(c3);
                bool blocked = Physics2D.OverlapBox(cent, tilemap.cellSize * 0.9f, 0f) != null;

                // prevent corner clip
                if (!blocked && d.x != 0 && d.y != 0)
                {
                    var a1 = new Vector3Int(currentCell.x + d.x, currentCell.y, 0);
                    var a2 = new Vector3Int(currentCell.x, currentCell.y + d.y, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a1), tilemap.cellSize * 0.9f, 0f) != null
                     || Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a2), tilemap.cellSize * 0.9f, 0f) != null)
                        blocked = true;
                }
                if (blocked || occupied.Contains(cand)) continue;

                float score = Vector2.Dot(new Vector2(d.x, d.y).normalized, dir);
                if (score > bestScore) { bestScore = score; best = cand; }
            }

            if (best == currentCell) { flowField = null; return; }

            occupied.Add(best);
            nextCell = best;
        }

        // 4) Step toward nextCell
        var tgt3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        var tgt2 = new Vector2(tgt3.x, tgt3.y);
        var delta = tgt2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + delta.normalized * Mathf.Min(step, delta.magnitude));
    }

    // reuse dirs from FlowField for neighbor iteration
    private static readonly Vector2Int[] dirs = FlowFieldDirs();
    private static Vector2Int[] FlowFieldDirs() => new[]{
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(1,-1),
        new Vector2Int(-1,1),new Vector2Int(-1,-1)
    };
}
