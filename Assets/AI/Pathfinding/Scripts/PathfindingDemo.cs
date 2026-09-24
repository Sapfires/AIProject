using System.Collections.Generic;
using AIGames.Common;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// Finds a path between a seeker and a target whenever one of them moves and paints
    /// the explored nodes and the resulting path onto the grid.
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

        Vector3 lastSeekerPos, lastTargetPos;
        List<Node> path;

        void Update()
        {
            if (seeker.position == lastSeekerPos && target.position == lastTargetPos)
                return;
            lastSeekerPos = seeker.position;
            lastTargetPos = target.position;

            path = pathfinding.FindPath(seeker.position, target.position);

            visualizer.Redraw();
            visualizer.PaintNodes(pathfinding.LastClosedSet, closedColor);
            visualizer.PaintNodes(path, pathColor);
            visualizer.Apply();

            DrawLine();
            DemoHUD.Show("Pathfinder 2 - A* algorithm",
                $"Explored nodes: {pathfinding.LastClosedSet.Count}\n" +
                $"Path length: {(path == null ? "no path" : path.Count + " nodes")}\n" +
                "Orange = closed set, blue = final path");
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
