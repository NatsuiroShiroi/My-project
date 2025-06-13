// Assets/UnitMover.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Tiles per second")]
    public float MoveSpeed = 3f;

    [Tooltip("Tilemap for world↔cell conversions (auto-found)")]
    public Tilemap tilemap;

    private FlowField flow;
    private bool isMoving;

    void Awake()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();
    }

    public void SetFlowField(FlowField ff)
    {
        flow = ff;
        isMoving = (ff != null);
    }

    void Update()
    {
        if (!isMoving || flow == null) return;

        // current cell
        var pos = transform.position;
        var c3 = tilemap.WorldToCell(pos);
        var cell = new Vector2Int(c3.x, c3.y);

        // flow dir
        Vector2 dir = flow.GetDirection(cell);
        if (dir == Vector2.zero)
        {
            isMoving = false;
            return;
        }

        // step
        transform.position += (Vector3)dir * MoveSpeed * Time.deltaTime;
    }
}
