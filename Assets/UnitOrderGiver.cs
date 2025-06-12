// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;

    [Tooltip("Which layers count as impassable static terrain")]
    public LayerMask obstacleMask;

    void Awake()
    {
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();

        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            IssueMoveOrder(wp);
        }
    }

    void IssueMoveOrder(Vector3 worldDest)
    {
        if (tilemap == null || groundSprite == null) return;

        // 1) Only selected units move
        var sels = UnitSelector.GetSelectedUnits();
        if (sels.Count == 0) return;

        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent(out UnitMover mv))
                movers.Add(mv);
        if (movers.Count == 0) return;

        // 2) Compute ground‐cell bounds (inset half‐cell)
        Bounds bs = groundSprite.bounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        Vector3Int minC = tilemap.WorldToCell(bs.min + half);
        Vector3Int maxC = tilemap.WorldToCell(bs.max - half);

        // 3) Clamp click inside those bounds
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minC.x, maxC.x),
            Mathf.Clamp(clickC.y, minC.y, maxC.y)
        );

        // 4) BFS outward to assign each mover a distinct free cell
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (goals.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();
            Vector3 ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));

            // skip if static obstacle
            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f, obstacleMask) == null)
                goals.Add(cur);

            foreach (var d in FlowField.Dirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt)) queue.Enqueue(nxt);
            }
        }

        // 5) Build & hand off each unit’s flow‐field
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
                obstacleMask      // ← your static‐terrain mask
            );
            field.Generate(goal);

            // back‐trace static path and assign it
            Vector3Int start3 = tilemap.WorldToCell(unit.transform.position);
            Vector2Int start = new Vector2Int(start3.x, start3.y);
            var path = field.GetPath(start, goal);
            unit.SetPath(path);
        }
    }
}
