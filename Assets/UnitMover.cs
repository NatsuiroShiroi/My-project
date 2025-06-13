// Assets/UnitMover.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for cell↔world conversions (auto-found if blank)")]
    public Tilemap tilemap;

    // The shared flow field for this move order
    private FlowField flow;
    private bool isMoving;

    void Awake()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();
    }

    /// <summary>
    /// Called once by UnitOrderGiver after FlowField.Generate(goal).
    /// </summary>
    public void SetFlowField(FlowField ff)
    {
        flow = ff;
        isMoving = (flow != null);
    }

    void Update()
    {
        if (!isMoving || flow == null) return;

        // 1) Determine our current cell
        Vector3 pos = transform.position;
        var cell3 = tilemap.WorldToCell(pos);
        var cell = new Vector2Int(cell3.x, cell3.y);

        // 2) Get the flow‐vector for that cell
        Vector2 dir = flow.GetDirection(cell);

        // 3) If zero, we’ve arrived (or can’t reach)
        if (dir == Vector2.zero)
        {
            isMoving = false;
            return;
        }

        // 4) Step smoothly along that vector
        Vector3 delta = (Vector3)dir * MoveSpeed * Time.deltaTime;
        transform.position += delta;
    }
}
