// Assets/UnitOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Tilemap your units walk on (auto-found if blank)")]
    public Tilemap tilemap;

    [Tooltip("SpriteRenderer on Ground defining the play area (auto-found if blank)")]
    public SpriteRenderer groundSprite;

    [Tooltip("Which layers count as static obstacles")]
    public LayerMask obstacleMask;

    // 8-way neighbors from FlowField
    static readonly Vector2Int[] Neighbors = FlowField.Dirs;

    void Awake()
    {
        // 1) Auto-find the Tilemap if not assigned
        if (tilemap == null)
        {
            var grid = GameObject.Find("Grid");
            if (grid != null)
                tilemap = grid.GetComponentInChildren<Tilemap>();
        }
        if (tilemap == null)
            Debug.LogError("[OrderGiver] Missing Tilemap! Make sure you have a 'Grid' GameObject with a Tilemap child.");

        // 2) Auto-find the Ground sprite if not assigned
        if (groundSprite == null)
        {
            var groundGO = GameObject.Find("Ground");
            if (groundGO != null)
                groundSprite = groundGO.GetComponent<SpriteRenderer>();
        }
        if (groundSprite == null)
            Debug.LogError("[OrderGiver] Missing Ground SpriteRenderer! Make sure you have a 'Ground' GameObject with a SpriteRenderer.");
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
        if (tilemap == null || groundSprite == null)
            return;

        // DEBUG
        Debug.Log($"[OrderGiver] Right‐click at {worldDest}");

        // 1) Only move selected units
        var sels = UnitSelector.GetSelectedUnits();
        Debug.Log($"[OrderGiver] Selected {sels.Count} units");
        if (sels.Count == 0) return;

        var movers = new List<UnitMover>();
        foreach (var go in sels)
            if (go.TryGetComponent<UnitMover>(out var mv))
                movers.Add(mv);
        Debug.Log($"[OrderGiver] {movers.Count} of those have UnitMover");
        if (movers.Count == 0) return;

        // 2) Compute play-area from groundSprite.bounds, inset half-cell
        Bounds gb = groundSprite.bounds;
        Vector3 half = tilemap.cellSize * 0.5f;
        Vector3 worldMin = gb.min + half;
        Vector3 worldMax = gb.max - half;

        Vector3Int minCell = tilemap.WorldToCell(worldMin);
        Vector3Int maxCell = tilemap.WorldToCell(worldMax);

        int ox = minCell.x, oy = minCell.y;
        int w = maxCell.x - minCell.x + 1;
        int h = maxCell.y - minCell.y + 1;

        // 3) Clamp click inside those cells
        Vector3Int clickC = tilemap.WorldToCell(worldDest);
        var baseGoal = new Vector2Int(
            Mathf.Clamp(clickC.x, minCell.x, maxCell.x),
            Mathf.Clamp(clickC.y, minCell.y, maxCell.y)
        );
        Debug.Log($"[OrderGiver] Clamped baseGoal = {baseGoal}");

        // 4) BFS outward for distinct free goals
        var goals = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int> { baseGoal };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(baseGoal);

        while (goals.Count < movers.Count && queue.Count > 0)
        {
            var cur = queue.Dequeue();
            Vector3 ctr = tilemap.GetCellCenterWorld(new Vector3Int(cur.x, cur.y, 0));

            if (Physics2D.OverlapBox(ctr, tilemap.cellSize * 0.9f, 0f, obstacleMask) == null)
                goals.Add(cur);

            foreach (var d in Neighbors)
            {
                var nxt = cur + d;
                if (nxt.x < minCell.x || nxt.x > maxCell.x ||
                    nxt.y < minCell.y || nxt.y > maxCell.y)
                    continue;
                if (seen.Add(nxt))
                    queue.Enqueue(nxt);
            }
        }

        Debug.Log($"[OrderGiver] Goals assigned: {string.Join(", ", goals)}");

        // 5) Build one FlowField per goal & hand off to each mover
        for (int i = 0; i < movers.Count && i < goals.Count; i++)
        {
            var unit = movers[i];
            var goal = goals[i];
            var field = new FlowField(ox, oy, w, h, tilemap, obstacleMask);
            field.Generate(goal);
            unit.SetFlowField(field);
            Debug.Log($"[OrderGiver] Unit {unit.name} → goal {goal}");
        }
    }
}
