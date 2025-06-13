// Assets/UnitMover.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Units per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for world↔cell conversions (auto-find if blank)")]
    public Tilemap tilemap;

    [Tooltip("Radius (world units) to avoid other units")]
    public float SeparationRadius = 0.6f;

    [Range(0f, 1f), Tooltip("Blend weight for separation")]
    public float SeparationWeight = 0.3f;

    private Rigidbody2D rb;
    private FlowField flow;
    private bool isMoving;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Start()
    {
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null) tilemap = g.GetComponentInChildren<Tilemap>();
            if (tilemap == null) Debug.LogError($"[UnitMover:{name}] No Grid→Tilemap found!");
        }
    }

    /// <summary>Called by OrderGiver once, after Generate(goal).</summary>
    public void SetFlowField(FlowField ff)
    {
        flow = ff;
        isMoving = (ff != null);
    }

    void FixedUpdate()
    {
        if (!isMoving || flow == null) return;

        // current float‐pos → cell
        Vector3 pos3 = transform.position;
        Vector3Int c3 = tilemap.WorldToCell(pos3);
        var cell = new Vector2Int(c3.x, c3.y);

        // static flow vector
        Vector2 dir = flow.GetDirection(cell);
        if (dir == Vector2.zero)
        {
            isMoving = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // separation steering
        Vector2 sep = Vector2.zero; int cnt = 0;
        var hits = Physics2D.OverlapCircleAll(rb.position, SeparationRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody == rb) continue;
            if (hit.TryGetComponent<UnitMover>(out _))
            {
                var d = rb.position - hit.attachedRigidbody.position;
                float dist = d.magnitude;
                if (dist > 0f && dist < SeparationRadius)
                {
                    sep += d.normalized * ((SeparationRadius - dist) / SeparationRadius);
                    cnt++;
                }
            }
        }
        if (cnt > 0) { sep /= cnt; sep = sep.normalized; }

        // final blended heading
        Vector2 desired = (dir * (1 - SeparationWeight) + sep * SeparationWeight).normalized;
        rb.linearVelocity = desired * MoveSpeed;
    }
}
