// Assets/UnitMover.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Units per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for cell↔world conversions (auto-find if blank)")]
    public Tilemap tilemap;

    [Tooltip("Radius to avoid other units in world units")]
    public float SeparationRadius = 0.6f;

    [Range(0f, 1f), Tooltip("Blend weight for separation")]
    public float SeparationWeight = 0.3f;

    private Rigidbody2D rb;
    private FlowField flow;
    private bool isMoving;

    void Awake()
    {
        // 1) Get or add our Rigidbody2D safely
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // 2) Find the Tilemap once all Awake calls have run
        if (tilemap == null)
        {
            var grid = GameObject.Find("Grid");
            if (grid != null)
                tilemap = grid.GetComponentInChildren<Tilemap>();
        }
        if (tilemap == null)
            Debug.LogError($"[UnitMover:{name}] No Tilemap found in scene!");
    }

    /// <summary>Called by the OrderGiver once, when you hand off a fresh flow field.</summary>
    public void SetFlowField(FlowField ff)
    {
        flow = ff;
        isMoving = (ff != null);
    }

    void FixedUpdate()
    {
        if (!isMoving || flow == null) return;

        // 3) Determine current cell from our world position
        Vector3 pos3 = transform.position;
        Vector3Int c3 = tilemap.WorldToCell(pos3);
        var cell = new Vector2Int(c3.x, c3.y);

        // 4) Sample the static flow direction
        Vector2 dir = flow.GetDirection(cell);
        if (dir == Vector2.zero)
        {
            // arrived or no path
            isMoving = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 5) Build a small separation steer
        Vector2 sep = Vector2.zero;
        int cnt = 0;
        var hits = Physics2D.OverlapCircleAll(rb.position, SeparationRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody == rb) continue;
            if (hit.TryGetComponent<UnitMover>(out _))
            {
                Vector2 d = rb.position - hit.attachedRigidbody.position;
                float dist = d.magnitude;
                if (dist > 0f && dist < SeparationRadius)
                {
                    sep += d.normalized * ((SeparationRadius - dist) / SeparationRadius);
                    cnt++;
                }
            }
        }
        if (cnt > 0)
        {
            sep /= cnt;
            sep = sep.normalized;
        }

        // 6) Blend flow + separation → final velocity
        Vector2 desired = (dir * (1 - SeparationWeight) + sep * SeparationWeight).normalized;
        rb.linearVelocity = desired * MoveSpeed;
    }
}
