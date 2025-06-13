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
            tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
            Debug.LogError($"[UnitMover:{name}] No Tilemap found!");
    }

    public void SetFlowField(FlowField ff)
    {
        flow = ff;
        isMoving = ff != null;
    }

    void FixedUpdate()
    {
        if (!isMoving || flow == null) return;

        // Determine current grid cell
        Vector3 pos3 = transform.position;
        Vector3Int c3 = tilemap.WorldToCell(pos3);
        var cell = new Vector2Int(c3.x, c3.y);

        // Get static flow direction
        Vector2 dir = flow.GetDirection(cell);
        if (dir == Vector2.zero)
        {
            isMoving = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Compute separation steering
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
                if (dist > 0 && dist < SeparationRadius)
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

        // Blend flow and separation into final heading
        Vector2 desired = (dir * (1 - SeparationWeight) + sep * SeparationWeight).normalized;

        // Drive via linearVelocity (new API)
        rb.linearVelocity = desired * MoveSpeed;
    }
}
