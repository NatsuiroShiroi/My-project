using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMover : MonoBehaviour
{
    public float MoveSpeed = 3.0f;
    public Tilemap Tilemap; // Assign in inspector
    private Rigidbody2D rb;
    private FlowField flowField;
    private Vector2Int targetCell;
    private Vector2Int lastCell;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetFlowField(FlowField field, Vector2Int target)
    {
        flowField = field;
        targetCell = target;
    }

    void FixedUpdate()
    {
        if (flowField == null || Tilemap == null) return;

        Vector2 worldPos = rb.position;
        Vector3Int cell = Tilemap.WorldToCell(worldPos);
        Vector2Int currentCell = new Vector2Int(cell.x, cell.y);

        // Only update occupancy when entering a new cell
        if (currentCell != lastCell)
        {
            GridCellManager.Instance.ReleaseCell(lastCell);
            if (!GridCellManager.Instance.TryReserveCell(currentCell))
                return; // Cell occupied, do not move
            lastCell = currentCell;
        }

        // If reached destination
        if (currentCell == targetCell) return;

        // Follow flow field
        Vector2 dir = flowField.GetDirection(currentCell);
        if (dir == Vector2.zero) return;

        // Separation from nearby units (simple version)
        Vector2 sep = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody == null || hit.attachedRigidbody == rb) continue;
            sep += (Vector2)(worldPos - hit.attachedRigidbody.position);
        }

        Vector2 moveVec = (dir + sep.normalized * 0.7f).normalized * MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(worldPos + moveVec);
    }
}
