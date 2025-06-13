// Assets/UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for cell↔world conversions (auto-find if blank)")]
    public Tilemap tilemap;

    [Header("Separation (units avoid each other)")]
    [Tooltip("Radius (world units) to look for other units")]
    public float SeparationRadius = 0.6f;

    [Range(0f, 1f), Tooltip("Blend weight for separation steering")]
    public float SeparationWeight = 0.3f;

    private FlowField flow;
    private bool isMoving;

    void Awake()
    {
        // no Rigidbody needed anymore
        // just find the tilemap once
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null) tilemap = g.GetComponentInChildren<Tilemap>();
            if (tilemap == null)
                Debug.LogError($"[UnitMover:{name}] No Grid→Tilemap found!");
        }
    }

    /// <summary>
    /// Called by UnitOrderGiver once, after Generate(goal).
    /// </summary>
    public void SetFlowField(FlowField ff)
    {
        flow = ff;
        isMoving = (ff != null);
    }

    void Update()
    {
        if (!isMoving || flow == null) return;

        // 1) Sample current cell based on floating position
        Vector3 worldPos = transform.position;
        Vector3Int cell3 = tilemap.WorldToCell(worldPos);
        var cell = new Vector2Int(cell3.x, cell3.y);

        // 2) Get the flow-vector for this cell
        Vector2 dir = flow.GetDirection(cell);
        if (dir == Vector2.zero)
        {
            // we've arrived (or no path)
            isMoving = false;
            return;
        }

        // 3) Separation steering
        Vector2 sep = Vector2.zero;
        int count = 0;
        var hits = Physics2D.OverlapCircleAll(worldPos, SeparationRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<UnitMover>(out var other) && other != this)
            {
                Vector2 diff = (Vector2)worldPos - (Vector2)other.transform.position;
                float d = diff.magnitude;
                if (d > 0f && d < SeparationRadius)
                {
                    sep += diff.normalized * ((SeparationRadius - d) / SeparationRadius);
                    count++;
                }
            }
        }
        if (count > 0)
        {
            sep /= count;
            sep = sep.normalized;
        }

        // 4) Blend and move
        Vector2 desired = (dir * (1f - SeparationWeight) + sep * SeparationWeight).normalized;
        Vector3 delta = (Vector3)desired * MoveSpeed * Time.deltaTime;
        transform.position += delta;
    }
}
