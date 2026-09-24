using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// A single cell of the pathfinding grid.
    /// </summary>
    public class Node : IHeapItem<Node>
    {
        public bool walkable;
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;
        public int movementPenalty;

        // A* costs: g = distance from the start node, h = heuristic distance to the target.
        public int gCost;
        public int hCost;
        public Node parent;

        public int FCost => gCost + hCost;
        public int HeapIndex { get; set; }

        public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY, int movementPenalty = 0)
        {
            this.movementPenalty = movementPenalty;
            this.walkable = walkable;
            this.worldPosition = worldPosition;
            this.gridX = gridX;
            this.gridY = gridY;
        }

        /// <summary>
        /// Higher priority (positive result) means lower f cost, ties broken by lower h cost.
        /// </summary>
        public int CompareTo(Node other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0)
                compare = hCost.CompareTo(other.hCost);
            return -compare;
        }
    }
}
