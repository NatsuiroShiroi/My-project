// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for world↔cell conversions (optional, auto-found if blank)")]
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

        // Auto-assign the Tilemap if none set in Inspector
        if (tilemap == null)
        {
            var gridGO = GameObject.Find("Grid");
            if (gridGO != null)
                tilemap = gridGO.GetComponentInChildren<Tilemap>();

            if (tilemap == null)
                Debug.LogError($"[UnitMover:{name}] Tilemap not assigned or found!");
        }
    }

    /// <summary>
    /// Clear all cell reservations at the start of a new order.
    /// </summary>
    public static void ClearReservations() => occupied.Clear();

    /// <summary>
    /// Assigns a new flow field and goal. Pass null to cancel any movement.
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        if (field == null)
        {
            // Cancel any prior motion
            isMoving = false;
            flowField = null;
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError($"[UnitMover:{name}] Cannot move: Tilemap missing.");
            return;
        }

        flowField = field;
        goalCell = goal;
        isMoving = true;

        // Initialize cell tracking & reserve start cell
        Vector3Int c3 = tilemap.WorldToCell(rb.position);
        currentCell = new Vector2Int(c3.x, c3.y);
        lastCell = currentCell;
        nextCell = currentCell;
        occupied.Add(currentCell);
    }

    void FixedUpdate()
    {
        if (!isMoving || tilemap == null || flowField == null)
            return;

        // 1) Arrived at goal?
        if (currentCell == goalCell)
        {
            // Snap exactly to center
            Vector3 exact3 = tilemap.GetCellCenterWorld(new Vector3Int(goalCell.x, goalCell.y, 0));
            rb.MovePosition(new Vector2(exact3.x, exact3.y));
            isMoving = false;
            return;
        }

        // 2) Arrived at nextCell center?
        Vector3 nextCtr3 = tilemap.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Vector2 nextCtr2 = new Vector2(nextCtr3.x, nextCtr3.y);
        if ((rb.position - nextCtr2).sqrMagnitude < 0.001f)
        {
            occupied.Remove(lastCell);
            lastCell = currentCell;
            currentCell = nextCell;
        }

        // 3) Pick a new nextCell if needed
        if (nextCell == currentCell)
        {
            // Get the best‐neighbor by true cost, not dot‐score
            List<(Vector2Int dir, float cost)> neigh = new();
            foreach (var d in FlowField.Dirs)
            {
                var cand = currentCell + d;
                if (!flowField.IsCellInBounds(cand)) continue;

                // Terrain collider
                var ctr3 = tilemap.GetCellCenterWorld(new Vector3Int(cand.x, cand.y, 0));
                if (Physics2D.OverlapBox(ctr3, tilemap.cellSize * 0.9f, 0f) != null)
                    continue;

                // Prevent diagonal corner‐cutting
                if (d.x != 0 && d.y != 0)
                {
                    var a1 = new Vector3Int(currentCell.x + d.x, currentCell.y, 0);
                    var a2 = new Vector3Int(currentCell.x, currentCell.y + d.y, 0);
                    if (Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a1), tilemap.cellSize * 0.9f, 0f) != null ||
                        Physics2D.OverlapBox(tilemap.GetCellCenterWorld(a2), tilemap.cellSize * 0.9f, 0f) != null)
                        continue;
                }

                // Collect cost
                float cVal = flowField.GetCost(cand);
                neigh.Add((d, cVal));
            }

            // Sort by ascending Dijkstra cost
            neigh.Sort((a, b) => a.cost.CompareTo(b.cost));

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

            // If no progress, stop
            if (chosen == currentCell)
            {
                isMoving = false;
                return;
            }

            occupied.Add(chosen);
            nextCell = chosen;
        }

        // 4) Step toward nextCell center
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
