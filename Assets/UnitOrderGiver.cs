// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Tilemap your units walk on (auto-find Grid→Tilemap)")]
    public Tilemap tilemap;

    [Tooltip("Which layers count as static obstacles")]
    public LayerMask obstacleMask;

    static readonly Vector2Int[] Neighbors = FlowField.Dirs;

    void Awake()
    {
        if (tilemap == null)
        {
            var g = GameObject.Find("Grid");
            if (g != null)
                tilemap = g.GetComponentInChildren<Tilemap>();
        }

        if (tilemap == null)
            Debug.LogError("[OrderGiver] No Tilemap found under GameObject 'Grid'!");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0;
            IssueMoveOrder(wp);
        }
    }

    void IssueMoveOrder(Vector3 worldDest)
    {
        if (tilemap == null) return;

        var sels = UnitSelector.GetSelectedUnits();
        if (sels.Count == 0) return;

        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0) return;

        // Calculate play area from the Ground sprite
        var groundSprite = Object.FindFirstObjectByType<SpriteRenderer>();
        if (groundSprite == null)
        {
            Debug.LogError("[OrderGiver] No SpriteRenderer found in scene to define ground bounds!");
            return;
        }

        Bounds gb = groundSprite.bounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        Vector3 worldMin = gb.min + half;
        Vector3 worldMax = gb.max - half;

        Vector3Int minC = tilemap.WorldToCell(worldMin);
        Vector3Int maxC = tilemap.WorldToCell(worldMax);

        int ox = minC.x, oy = minC.y;
        int w = maxC.x - minC.x + 1;
        int h = maxC.y - minC.y + 1;

        Vector3Int click = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(click.x, minC.x, maxC.x),
            Mathf.Clamp(click.y, minC.y, maxC.y)
        );

        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int> { baseGoal };
        var q = new Queue<Vector2Int>();
        q.Enqueue(baseGoal);

        while (goals.Count < movers.Count && q.Count > 0)
        {
            var cur = q.Dequeue();
            Vector3 ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));

            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f, obstacleMask) == null)
                goals.Add(cur);

            foreach (var d in Neighbors)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt)) q.Enqueue(nxt);
            }
        }

        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var mv = movers[i];
            var goal = goals[i];

            var field = new FlowField(ox, oy, w, h, tilemap, obstacleMask);
            field.Generate(goal);
            mv.SetFlowField(field);
        }
    }
}
