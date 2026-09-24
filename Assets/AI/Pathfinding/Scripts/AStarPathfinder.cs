using System.Collections.Generic;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// A* search over the PathGrid. Moving straight costs 10, moving diagonally costs 14
    /// (an integer approximation of 10 * sqrt(2)).
    /// </summary>
    [RequireComponent(typeof(PathGrid))]
    public class AStarPathfinder : MonoBehaviour
    {
        [Tooltip("Use the binary heap for the open set (part 3). When off, a plain list is scanned.")]
        public bool useHeap = true;

        PathGrid grid;

        /// <summary>Nodes evaluated by the last search, kept for visualisation.</summary>
        public HashSet<Node> LastClosedSet { get; private set; } = new HashSet<Node>();

        void Awake()
        {
            grid = GetComponent<PathGrid>();
        }

        public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            return useHeap ? FindPathHeap(startPos, targetPos) : FindPathList(startPos, targetPos);
        }

        /// <summary>Part 2 version: the open set is a list, finding the best node is O(n).</summary>
        public List<Node> FindPathList(Vector3 startPos, Vector3 targetPos)
        {
            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);
            LastClosedSet = new HashSet<Node>();
            if (!startNode.walkable || !targetNode.walkable)
                return null;

            var openSet = new List<Node> { startNode };
            var closedSet = LastClosedSet;
            startNode.gCost = 0;
            startNode.hCost = GetDistance(startNode, targetNode);
            startNode.parent = null;

            while (openSet.Count > 0)
            {
                // Pick the open node with the lowest f cost (ties broken by h cost).
                Node current = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < current.FCost ||
                        openSet[i].FCost == current.FCost && openSet[i].hCost < current.hCost)
                        current = openSet[i];
                }

                openSet.Remove(current);
                closedSet.Add(current);

                if (current == targetNode)
                    return RetracePath(startNode, targetNode);

                foreach (Node neighbour in grid.GetNeighbours(current))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                        continue;

                    int newCost = current.gCost + GetDistance(current, neighbour);
                    bool inOpen = openSet.Contains(neighbour);
                    if (newCost < neighbour.gCost || !inOpen)
                    {
                        neighbour.gCost = newCost;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = current;
                        if (!inOpen)
                            openSet.Add(neighbour);
                    }
                }
            }
            return null;
        }

        /// <summary>Part 3 version: the open set is a binary heap, finding the best node is O(log n).</summary>
        public List<Node> FindPathHeap(Vector3 startPos, Vector3 targetPos)
        {
            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);
            LastClosedSet = new HashSet<Node>();
            if (!startNode.walkable || !targetNode.walkable)
                return null;

            var openSet = new Heap<Node>(grid.MaxSize);
            var closedSet = LastClosedSet;
            startNode.gCost = 0;
            startNode.hCost = GetDistance(startNode, targetNode);
            startNode.parent = null;
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node current = openSet.RemoveFirst();
                closedSet.Add(current);

                if (current == targetNode)
                    return RetracePath(startNode, targetNode);

                foreach (Node neighbour in grid.GetNeighbours(current))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                        continue;

                    int newCost = current.gCost + GetDistance(current, neighbour) + neighbour.movementPenalty;
                    bool inOpen = openSet.Contains(neighbour);
                    if (newCost < neighbour.gCost || !inOpen)
                    {
                        neighbour.gCost = newCost;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = current;
                        if (!inOpen)
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
            }
            return null;
        }

        static List<Node> RetracePath(Node startNode, Node endNode)
        {
            var path = new List<Node>();
            Node current = endNode;
            while (current != startNode)
            {
                path.Add(current);
                current = current.parent;
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Part 4: keeps only the nodes where the direction of travel changes, so units get
        /// a short list of waypoints instead of one point per grid cell.
        /// </summary>
        public static Vector3[] SimplifyPath(List<Node> path)
        {
            var waypoints = new List<Vector3>();
            Vector2 directionOld = Vector2.zero;
            for (int i = 1; i < path.Count; i++)
            {
                var directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
                if (directionNew != directionOld)
                    waypoints.Add(path[i - 1].worldPosition);
                directionOld = directionNew;
            }
            if (path.Count > 0)
                waypoints.Add(path[path.Count - 1].worldPosition);
            return waypoints.ToArray();
        }

        public static int GetDistance(Node a, Node b)
        {
            int dstX = Mathf.Abs(a.gridX - b.gridX);
            int dstY = Mathf.Abs(a.gridY - b.gridY);
            return dstX > dstY
                ? 14 * dstY + 10 * (dstX - dstY)
                : 14 * dstX + 10 * (dstY - dstX);
        }
    }
}
