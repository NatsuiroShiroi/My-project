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
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();
    }

    public void SetFlowField(FlowField ff)
    {
        flow = ff;
        isMoving = (flow != null);
    }

    void FixedUpdate()
    {
        if (!isMoving || flow == null)
            return;

        // current pos → cell
        Vector3 pos = transform.position;
        var c3 = tilemap.WorldToCell(pos);
        var cell = new Vector2Int(c3.x, c3.y);

        // static flow direction
        Vector2 dir = flow.GetDirection(cell);
        if (dir == Vector2.zero)
        {
            isMoving = false;
            rb.velocity = Vector2.zero;
            return;
        }

        // separation
        Vector2 sep = Vector2.zero; int cnt = 0;
        var hits = Physics2D.OverlapCircleAll(rb.position, SeparationRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody == rb) continue;
            if (hit.TryGetComponent<UnitMover>(out var _))
            {
                Vector2 d = rb.position - hit.attachedRigidbody.position;
                float dist = d.magnitude;
                if (dist > 0 && dist < SeparationRadius)
                {
                    sep += d.normalized * ((SeparationRadius - dist) / SeparationRadius);
                    cnt++;
                }
            }
        }
        if (cnt > 0) { sep /= cnt; sep = sep.normalized; }

        // blend and drive velocity
        Vector2 desired = (dir * (1 - SeparationWeight) + sep * SeparationWeight).normalized;
        rb.velocity = desired * MoveSpeed;
    }
}
