// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for world↔cell conversions (optional)")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int goalCell;
    private Vector2Int lastCell, currentCell, nextCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private bool isMoving = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null) tilemap = g.GetComponentInChildren<Tilemap>();
        }
        if (tilemap == null)
            Debug.LogError($"[UnitMover:{name}] Tilemap not assigned!");
    }

    public static void ClearReservations() => occupied.Clear();

    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        if (field == null)
        {
            isMoving = false;
            return;
        }
        if (tilemap == null) return;

        flowField = field;
        goalCell = goal;
        isMoving = true;

        var c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (!isMoving || tilemap == null || flowField == null)
            return;

        // Snap & stop if at goal
        if (currentCell == goalCell)
        {
            var ex = tilemap.GetCellCenterWorld(
                new Vector3Int(goalCell.x, goalCell.y, 0));
            rb.MovePosition(new Vector2(ex.x, ex.y));
            isMoving = false;
            return;
        }

        // Only free when we've actually moved into a new cell
        if (nextCell != currentCell)
        {
            var center3 = tilemap.GetCellCenterWorld(
                new Vector3Int(nextCell.x, nextCell.y, 0));
            var center2 = new Vector2(center3.x, center3.y);
            if ((rb.position - center2).sqrMagnitude < 0.001f)
            {
                occupied.Remove(currentCell);
                lastCell = currentCell;
                currentCell = nextCell;
            }
        }

        // Pick nextCell if needed
        if (nextCell == currentCell)
        {
            // gather valid neighbors with cost
            var neigh = new List<(Vector2Int d, float c)>();
            foreach (var d in FlowField.Dirs)
            {
                var cand = currentCell + d;
                if (!flowField.IsCellInBounds(cand)) continue;

                // terrain block
                var ctr3 = tilemap.GetCellCenterWorld(
                    new Vector3Int(cand.x, cand.y, 0));
                if (Physics2D.OverlapBox(ctr3, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;

                // corner cut prevention
                if (d.x != 0 && d.y != 0)
                {
                    var a1 = new Vector3Int(currentCell.x + d.x, currentCell.y, 0);
                    var a2 = new Vector3Int(currentCell.x, currentCell.y + d.y, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a1),
                        tilemap.cellSize * 0.9f, 0f) != null ||
                        Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a2),
                        tilemap.cellSize * 0.9f, 0f) != null)
                        continue;
                }

                neigh.Add((d, flowField.GetCost(cand)));
            }

            neigh.Sort((a, b) => a.c.CompareTo(b.c));

            Vector2Int chosen = currentCell;
            foreach (var (d, _) in neigh)
            {
                var cand = currentCell + d;
                if (!occupied.Contains(cand))
                {
                    chosen = cand;
                    break;
                }
            }

            if (chosen == currentCell)
            {
                isMoving = false;
                return;
            }

            occupied.Add(chosen);
            nextCell = chosen;
        }

        // Move toward nextCell
        var t3 = tilemap.GetCellCenterWorld(
            new Vector3Int(nextCell.x, nextCell.y, 0));
        var t2 = new Vector2(t3.x, t3.y);
        var delta = t2 - rb.position;
        float step = MoveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + delta.normalized * Mathf.Min(step, delta.magnitude));
    }
}
