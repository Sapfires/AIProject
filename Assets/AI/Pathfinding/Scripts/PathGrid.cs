using System.Collections.Generic;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// Divides the level into a grid of nodes. A node is unwalkable when a collider
    /// on the unwalkable layer overlaps it.
    /// </summary>
    public class PathGrid : MonoBehaviour
    {
        public LayerMask unwalkableMask;
        public Vector2 gridWorldSize = new Vector2(40, 40);
        public float nodeRadius = 0.5f;
        public bool displayGizmos = true;

        Node[,] grid;
        float nodeDiameter;
        int gridSizeX, gridSizeY;

        public int SizeX => gridSizeX;
        public int SizeY => gridSizeY;
        public int MaxSize => gridSizeX * gridSizeY;

        void Awake()
        {
            CreateGrid();
        }

        public void CreateGrid()
        {
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
            grid = new Node[gridSizeX, gridSizeY];

            Vector3 worldBottomLeft = transform.position
                - Vector3.right * gridWorldSize.x / 2
                - Vector3.forward * gridWorldSize.y / 2;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft
                        + Vector3.right * (x * nodeDiameter + nodeRadius)
                        + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius * 0.9f, unwalkableMask);
                    grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }
        }

        public Node GetNode(int x, int y) => grid[x, y];

        public List<Node> GetNeighbours(Node node)
        {
            var neighbours = new List<Node>(8);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int checkX = node.gridX + dx;
                    int checkY = node.gridY + dy;
                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                        neighbours.Add(grid[checkX, checkY]);
                }
            }
            return neighbours;
        }

        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - transform.position;
            float percentX = Mathf.Clamp01((local.x + gridWorldSize.x / 2) / gridWorldSize.x);
            float percentY = Mathf.Clamp01((local.z + gridWorldSize.y / 2) / gridWorldSize.y);
            int x = Mathf.Clamp(Mathf.FloorToInt(gridSizeX * percentX), 0, gridSizeX - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(gridSizeY * percentY), 0, gridSizeY - 1);
            return grid[x, y];
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
            if (grid == null || !displayGizmos)
                return;

            foreach (Node n in grid)
            {
                Gizmos.color = n.walkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.6f);
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }
}
