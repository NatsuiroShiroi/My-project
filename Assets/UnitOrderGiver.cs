// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;

    [Tooltip("LayerMask for static terrain obstacles")]
    public LayerMask ObstacleMask;

    void Awake()
    {
        var g = GameObject.Find("Grid");
        if (g != null)
            tilemap = g.GetComponentInChildren<Tilemap>();
        var gr = GameObject.Find("Ground");
        if (gr != null)
            groundSprite = gr.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            IssueMoveOrder(wp);
        }
    }

    void IssueMoveOrder(Vector3 worldDest)
    {
        if (tilemap == null || groundSprite == null) return;

        // gather units, BFS‐assign distinct goals, etc. (unchanged)

        // when you create each FlowField, now pass ObstacleMask:
        var field = new FlowField(
            minCell.x, minCell.y,
            width, height,
            tilemap,
            ObstacleMask
        );
        field.Generate(goalCell);
        unit.SetPath(field.GetPath(startCell, goalCell));
    }
}
