using System.Collections.Generic;
using System.Diagnostics;
using AIGames.Common;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// Finds a path between a seeker and a target whenever one of them moves and paints
    /// the explored nodes and the resulting path onto the grid. Optionally benchmarks the
    /// list-based open set (part 2) against the heap-based one (part 3).
    /// </summary>
    public class PathfindingDemo : MonoBehaviour
    {
        public Transform seeker;
        public Transform target;
        public AStarPathfinder pathfinding;
        public GridVisualizer visualizer;
        public LineRenderer pathLine;
        public Color closedColor = new Color(0.98f, 0.80f, 0.45f);
        public Color pathColor = new Color(0.20f, 0.55f, 0.95f);

        [Header("Benchmark (part 3)")]
        public bool benchmark = true;
        public int benchmarkRuns = 20;

        Vector3 lastSeekerPos, lastTargetPos;
        List<Node> path;

        void Update()
        {
            if (seeker.position == lastSeekerPos && target.position == lastTargetPos)
                return;
            lastSeekerPos = seeker.position;
            lastTargetPos = target.position;

            string timing = benchmark ? Benchmark() : "";
            path = pathfinding.FindPath(seeker.position, target.position);

            visualizer.Redraw();
            visualizer.PaintNodes(pathfinding.LastClosedSet, closedColor);
            visualizer.PaintNodes(path, pathColor);
            visualizer.Apply();

            DrawLine();
            DemoHUD.Show(benchmark ? "Pathfinder 3 - Heap optimisation" : "Pathfinder 2 - A* algorithm",
                $"Explored nodes: {pathfinding.LastClosedSet.Count}, " +
                $"path: {(path == null ? "none" : path.Count + " nodes")}\n" +
                timing +
                "Orange = closed set, blue = final path");
        }

        string Benchmark()
        {
            // Warm up once so JIT compilation is not measured.
            pathfinding.FindPathList(seeker.position, target.position);
            pathfinding.FindPathHeap(seeker.position, target.position);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < benchmarkRuns; i++)
                pathfinding.FindPathList(seeker.position, target.position);
            double listMs = sw.Elapsed.TotalMilliseconds / benchmarkRuns;

            sw.Restart();
            for (int i = 0; i < benchmarkRuns; i++)
                pathfinding.FindPathHeap(seeker.position, target.position);
            double heapMs = sw.Elapsed.TotalMilliseconds / benchmarkRuns;

            UnityEngine.Debug.Log($"[A*] list: {listMs:F2} ms, heap: {heapMs:F2} ms ({listMs / heapMs:F1}x)");
            return $"List open set: {listMs:F2} ms\nHeap open set: {heapMs:F2} ms  ({listMs / heapMs:F1}x faster)\n";
        }

        void DrawLine()
        {
            if (pathLine == null)
                return;
            if (path == null)
            {
                pathLine.positionCount = 0;
                return;
            }
            pathLine.positionCount = path.Count + 1;
            pathLine.SetPosition(0, seeker.position + Vector3.up * 0.1f);
            for (int i = 0; i < path.Count; i++)
                pathLine.SetPosition(i + 1, path[i].worldPosition + Vector3.up * 0.1f);
        }
    }
}
