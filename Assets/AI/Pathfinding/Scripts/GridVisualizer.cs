using System.Collections.Generic;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// Draws the grid into a texture on a floor quad so it is visible in the Game view
    /// (gizmos are only visible in the Scene view).
    /// </summary>
    [RequireComponent(typeof(PathGrid))]
    public class GridVisualizer : MonoBehaviour
    {
        public Renderer floor;
        public int pixelsPerNode = 8;
        public Color walkableColor = new Color(0.86f, 0.88f, 0.84f);
        public Color unwalkableColor = new Color(0.78f, 0.25f, 0.22f);
        public Color gridLineColor = new Color(0.70f, 0.72f, 0.68f);
        [Tooltip("Colour of the most expensive walkable node (part 4 movement penalties).")]
        public Color highPenaltyColor = new Color(0.42f, 0.33f, 0.24f);

        PathGrid grid;
        Texture2D texture;
        Color[] pixels;

        void Start()
        {
            grid = GetComponent<PathGrid>();
            texture = new Texture2D(grid.SizeX * pixelsPerNode, grid.SizeY * pixelsPerNode, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color[texture.width * texture.height];
            floor.material.SetTexture("_BaseMap", texture);
            floor.material.SetColor("_BaseColor", Color.white);
            Redraw();
        }

        public void Redraw()
        {
            for (int x = 0; x < grid.SizeX; x++)
                for (int y = 0; y < grid.SizeY; y++)
                    PaintNode(grid.GetNode(x, y), BaseColor(grid.GetNode(x, y)));
            Apply();
        }

        protected virtual Color BaseColor(Node node)
        {
            if (!node.walkable)
                return unwalkableColor;
            if (grid.PenaltyMax <= grid.PenaltyMin)
                return walkableColor;
            float t = Mathf.InverseLerp(grid.PenaltyMin, grid.PenaltyMax, node.movementPenalty);
            return Color.Lerp(walkableColor, highPenaltyColor, t);
        }

        public void PaintNodes(IEnumerable<Node> nodes, Color color)
        {
            if (nodes == null)
                return;
            foreach (Node n in nodes)
                PaintNode(n, color);
        }

        public void PaintNode(Node node, Color color)
        {
            int px = node.gridX * pixelsPerNode;
            int py = node.gridY * pixelsPerNode;
            for (int i = 0; i < pixelsPerNode; i++)
            {
                for (int j = 0; j < pixelsPerNode; j++)
                {
                    bool border = i == 0 || j == 0;
                    pixels[(py + j) * texture.width + px + i] = border ? color * gridLineColor : color;
                }
            }
        }

        public void Apply()
        {
            texture.SetPixels(pixels);
            texture.Apply();
        }
    }
}
