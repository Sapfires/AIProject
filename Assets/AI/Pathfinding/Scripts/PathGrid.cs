using System.Collections.Generic;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// Divides the level into a grid of nodes. A node is unwalkable when a collider
    /// on the unwalkable layer overlaps it. Walkable nodes can carry a movement penalty
    /// (terrain type, closeness to obstacles) that makes A* prefer other routes.
    /// </summary>
    public class PathGrid : MonoBehaviour
    {
        [System.Serializable]
        public class TerrainType
        {
            public LayerMask terrainMask;
            public int terrainPenalty;
        }

        public LayerMask unwalkableMask;
        public Vector2 gridWorldSize = new Vector2(40, 40);
        public float nodeRadius = 0.5f;
        public bool displayGizmos = true;

        [Header("Movement penalties (part 4)")]
        public TerrainType[] walkableRegions = new TerrainType[0];
        public int obstacleProximityPenalty = 0;
        [Tooltip("Box blur radius (in nodes) applied to the penalty map for smoother paths.")]
        public int penaltyBlurSize = 0;

        Node[,] grid;
        float nodeDiameter;
        int gridSizeX, gridSizeY;
        LayerMask walkableMask;
        readonly Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();

        public int SizeX => gridSizeX;
        public int SizeY => gridSizeY;
        public int MaxSize => gridSizeX * gridSizeY;
        public int PenaltyMin { get; private set; }
        public int PenaltyMax { get; private set; }

        void Awake()
        {
            foreach (TerrainType region in walkableRegions)
            {
                walkableMask.value |= region.terrainMask.value;
                int layer = Mathf.RoundToInt(Mathf.Log(region.terrainMask.value, 2));
                walkableRegionsDictionary[layer] = region.terrainPenalty;
            }
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

                    int movementPenalty = 0;
                    var ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 100, walkableMask))
                        walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                    if (!walkable)
                        movementPenalty += obstacleProximityPenalty;

                    grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
                }
            }

            if (penaltyBlurSize > 0)
                BlurPenaltyMap(penaltyBlurSize);
        }

        /// <summary>
        /// Two-pass box blur of the penalty map. Spreads the high penalty of obstacles into
        /// the neighbouring walkable nodes so units keep some distance from walls.
        /// </summary>
        void BlurPenaltyMap(int blurSize)
        {
            int kernelSize = blurSize * 2 + 1;
            int kernelExtents = blurSize;
            var horizontal = new int[gridSizeX, gridSizeY];
            var vertical = new int[gridSizeX, gridSizeY];

            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = -kernelExtents; x <= kernelExtents; x++)
                    horizontal[0, y] += grid[Mathf.Clamp(x, 0, kernelExtents), y].movementPenalty;

                for (int x = 1; x < gridSizeX; x++)
                {
                    int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX - 1);
                    int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);
                    horizontal[x, y] = horizontal[x - 1, y]
                        - grid[removeIndex, y].movementPenalty
                        + grid[addIndex, y].movementPenalty;
                }
            }

            PenaltyMin = int.MaxValue;
            PenaltyMax = int.MinValue;
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = -kernelExtents; y <= kernelExtents; y++)
                    vertical[x, 0] += horizontal[x, Mathf.Clamp(y, 0, kernelExtents)];

                SetBlurred(x, 0, vertical[x, 0], kernelSize);
                for (int y = 1; y < gridSizeY; y++)
                {
                    int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY - 1);
                    int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);
                    vertical[x, y] = vertical[x, y - 1] - horizontal[x, removeIndex] + horizontal[x, addIndex];
                    SetBlurred(x, y, vertical[x, y], kernelSize);
                }
            }
        }

        void SetBlurred(int x, int y, int sum, int kernelSize)
        {
            int blurred = Mathf.RoundToInt((float)sum / (kernelSize * kernelSize));
            grid[x, y].movementPenalty = blurred;
            PenaltyMin = Mathf.Min(PenaltyMin, blurred);
            PenaltyMax = Mathf.Max(PenaltyMax, blurred);
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
                float t = Mathf.InverseLerp(PenaltyMin, PenaltyMax, n.movementPenalty);
                Gizmos.color = n.walkable ? Color.Lerp(Color.white, Color.black, t) : Color.red;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.05f));
            }
        }
    }
}
