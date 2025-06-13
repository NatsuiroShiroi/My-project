// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Tilemap that units walk on (auto-found if blank)")]
    public Tilemap tilemap;

    [Tooltip("Which layers are static obstacles")]
    public LayerMask obstacleMask;

    // 8-way neighbor offsets for BFS
    private static readonly Vector2Int[] Neighbors = FlowField.Dirs;

    void Awake()
    {
        // auto-find your Grid → Tilemap if you forgot to hook it up
        if (tilemap == null)
        {
            var grid = GameObject.Find("Grid");
            if (grid != null)
                tilemap = grid.GetComponentInChildren<Tilemap>();
            if (tilemap == null)
                Debug.LogError("[OrderGiver] No Tilemap assigned or found under ‘Grid’!");
        }
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
        if (tilemap == null) return;

        // 1) Only march selected units
        var sels = UnitSelector.GetSelectedUnits();
        if (sels.Count == 0) return;

        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        if (movers.Count == 0) return;

        // 2) Compute the playable bounds (inset half-cell)
        Bounds bs = tilemap.localBounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        Vector3Int minC = tilemap.WorldToCell(bs.min + half);
        Vector3Int maxC = tilemap.WorldToCell(bs.max - half);

        // 3) Clamp your click inside
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minC.x, maxC.x),
            Mathf.Clamp(clickC.y, minC.y, maxC.y)
        );

        // 4) BFS outward to pick distinct free goal cells
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int> { baseGoal };
        var q = new Queue<Vector2Int>();
        q.Enqueue(baseGoal);

        while (goals.Count < movers.Count && q.Count > 0)
        {
            var cur = q.Dequeue();
            Vector3 ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));

            // skip if static obstacle here
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

        // 5) Build & hand off a flow-field per mover
        int w = maxC.x - minC.x + 1;
        int h = maxC.y - minC.y + 1;

        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];

            var field = new FlowField(
                minC.x, minC.y,
                w, h,
                tilemap,
                obstacleMask
            );
            field.Generate(goal);

            unit.SetFlowField(field);
        }
    }
}
