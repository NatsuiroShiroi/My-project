// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    private Tilemap tilemap;
    private SpriteRenderer groundSprite;

    void Awake()
    {
        // Auto-find Grid → Tilemap
        var gridGO = GameObject.Find("Grid");
        if (gridGO != null)
            tilemap = gridGO.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
            Debug.LogError("[OrderGiver] Grid→Tilemap not found!");

        // Auto-find Ground → SpriteRenderer
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
            groundSprite = groundGO.GetComponent<SpriteRenderer>();
        if (groundSprite == null)
            Debug.LogError("[OrderGiver] Ground→SpriteRenderer not found!");
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

    private void IssueMoveOrder(Vector3 worldDest)
    {
        if (tilemap == null || groundSprite == null)
            return;

        // 1) Only act if units are selected:
        var sels = UnitSelector.GetSelectedUnits();
        if (sels.Count == 0)
            return;

        // 2) Collect their UnitMover components:
        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent(out UnitMover mv))
                movers.Add(mv);
        if (movers.Count == 0)
            return;

        // 3) Compute playable bounds inset half a cell:
        Bounds bs = groundSprite.bounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        Vector3 minW = bs.min + half, maxW = bs.max - half;
        Vector3Int minC = tilemap.WorldToCell(minW);
        Vector3Int maxC = tilemap.WorldToCell(maxW);

        // 4) Clamp click inside:
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        Vector2Int baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minC.x, maxC.x),
            Mathf.Clamp(clickC.y, minC.y, maxC.y)
        );

        // 5) BFS to pick one free goal per mover:
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var q = new Queue<Vector2Int>();
        q.Enqueue(baseGoal);
        seen.Add(baseGoal);

        while (goals.Count < movers.Count && q.Count > 0)
        {
            var cur = q.Dequeue();
            // skip if terrain blocks this cell
            Vector3 ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));
            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f) == null)
                goals.Add(cur);
            // enqueue neighbors
            foreach (var d in FlowField.Dirs)
            {
                var nxt = cur + d;
                if (nxt.x < minC.x || nxt.x > maxC.x || nxt.y < minC.y || nxt.y > maxC.y)
                    continue;
                if (seen.Add(nxt))
                    q.Enqueue(nxt);
            }
        }

        // 6) For each mover, compute static path and hand it off:
        int w = maxC.x - minC.x + 1, h = maxC.y - minC.y + 1;
        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];

            // build flow field over terrain only
            var field = new FlowField(minC.x, minC.y, w, h, tilemap);
            field.Generate(goal);

            // back-trace a static path
            Vector3Int start3 = tilemap.WorldToCell(unit.transform.position);
            Vector2Int start = new Vector2Int(start3.x, start3.y);
            List<Vector2Int> path = field.GetPath(start, goal);

            // assign the precomputed path
            unit.SetPath(path);
        }
    }
}
