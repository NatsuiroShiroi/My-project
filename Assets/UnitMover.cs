// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Same Tilemap assigned in OrderGiver")]
    public Tilemap Tilemap;

    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int targetCell;
    private Vector2Int lastCell;
    private static HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Called by OrderGiver when issuing the move
    /// </summary>
    public void SetFlowField(FlowField field, Vector2Int goal)
    {
        flowField = field;
        targetCell = goal;
        Debug.Log($"[UnitMover:{name}] Received flow field. Target = {targetCell}");
    }

    void FixedUpdate()
    {
        if (flowField == null)
            return;  // no order yet

        if (Tilemap == null)
        {
            Debug.LogError($"[UnitMover:{name}] Tilemap not assigned!");
            return;
        }

        // 1) Which cell am I in?
        Vector2 worldPos = rb.position;
        Vector3Int c3 = Tilemap.WorldToCell(worldPos);
        Vector2Int cur = new Vector2Int(c3.x, c3.y);

        // 2) Occupancy check on new cell
        if (cur != lastCell)
        {
            occupied.Remove(lastCell);
            if (occupied.Contains(cur))
            {
                Debug.Log($"[UnitMover:{name}] cell {cur} occupied, waiting.");
                return;
            }
            occupied.Add(cur);
            lastCell = cur;
            Debug.Log($"[UnitMover:{name}] Entered cell {cur}");
        }

        // 3) Reached destination?
        if (cur == targetCell)
        {
            Debug.Log($"[UnitMover:{name}] Reached target cell.");
            return;
        }

        // 4) Sample flow field
        Vector2 dir = flowField.GetDirection(cur);
        if (dir == Vector2.zero)
        {
            Debug.Log($"[UnitMover:{name}] No direction at cell {cur} → stuck?");
            return;
        }

        // 5) Local separation
        Vector2 sep = Vector2.zero;
        foreach (var hit in Physics2D.OverlapCircleAll(worldPos, 0.5f))
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody != rb)
                sep += worldPos - (Vector2)hit.attachedRigidbody.position;
        }

        // 6) Compute move vector
        Vector2 move = (dir + sep.normalized * 0.7f).normalized
                       * MoveSpeed * Time.fixedDeltaTime;
        if (move == Vector2.zero)
        {
            Debug.Log($"[UnitMover:{name}] Computed zero move vector.");
            return;
        }

        // 7) Perform movement
        rb.MovePosition(worldPos + move);
        Debug.Log($"[UnitMover:{name}] Moving {move.magnitude:F3} units towards {dir}");
    }
}
