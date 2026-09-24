using AIGames.Pathfinding;
using UnityEditor;
using UnityEngine;
using static AIGames.EditorTools.SceneBuilderUtils;

namespace AIGames.EditorTools
{
    public static class PathfindingSceneBuilder
    {
        public const int UnwalkableLayer = 8;

        // Obstacle layout: centre (x, z) and size (x, z) in world units.
        static readonly Vector4[] Walls =
        {
            new Vector4(-10, -6, 1.5f, 24),
            new Vector4(4, 6, 1.5f, 28),
            new Vector4(13, -8, 10, 1.5f),
            new Vector4(-3, 12, 8, 4),
            new Vector4(14, 10, 4, 4),
            new Vector4(-15, 14, 4, 4),
            new Vector4(-3, -14, 4, 6),
            new Vector4(15, -15, 3, 3),
            new Vector4(-16, -2, 6, 1.5f),
        };

        [MenuItem("AI in Games/Build Pathfinding Scene")]
        public static void Build()
        {
            var scene = NewScene();
            AddCamera(new Vector3(-4, 42, -19), new Vector3(66, 0, 0), new Color(0.13f, 0.15f, 0.19f));
            AddSun();

            var floorMat = Mat("PF_Floor", Color.white, unlit: true);
            var wallMat = Mat("PF_Wall", new Color(0.35f, 0.38f, 0.45f));

            var gridGo = new GameObject("A* Grid");
            var grid = gridGo.AddComponent<PathGrid>();
            grid.unwalkableMask = 1 << UnwalkableLayer;
            grid.gridWorldSize = new Vector2(40, 40);
            grid.nodeRadius = 0.5f;

            var floor = Primitive(PrimitiveType.Quad, "Floor", Vector3.zero, new Vector3(40, 40, 1), floorMat, gridGo.transform);
            floor.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var visualizer = gridGo.AddComponent<GridVisualizer>();
            visualizer.floor = floor.GetComponent<Renderer>();

            var obstacles = new GameObject("Obstacles").transform;
            for (int i = 0; i < Walls.Length; i++)
            {
                var w = Walls[i];
                Primitive(PrimitiveType.Cube, $"Wall {i}", new Vector3(w.x, 1, w.y), new Vector3(w.z, 2, w.w),
                    wallMat, obstacles, UnwalkableLayer);
            }

            // Part 2: A* between a seeker and a target.
            var pathfinder = gridGo.AddComponent<AStarPathfinder>();
            var seeker = Primitive(PrimitiveType.Capsule, "Seeker", new Vector3(-16, 1, -16), Vector3.one,
                Mat("PF_Seeker", new Color(0.25f, 0.75f, 0.35f)));
            var target = Primitive(PrimitiveType.Sphere, "Target", new Vector3(16, 0.75f, 16), Vector3.one * 1.5f,
                Mat("PF_Target", new Color(0.90f, 0.25f, 0.25f)));

            var lineGo = new GameObject("Path Line");
            var line = lineGo.AddComponent<LineRenderer>();
            line.sharedMaterial = Mat("PF_PathLine", new Color(0.1f, 0.3f, 0.8f), unlit: true);
            line.widthMultiplier = 0.25f;
            line.positionCount = 0;

            var demo = new GameObject("Pathfinding Demo").AddComponent<PathfindingDemo>();
            demo.seeker = seeker.transform;
            demo.target = target.transform;
            demo.pathfinding = pathfinder;
            demo.visualizer = visualizer;
            demo.pathLine = line;

            SaveScene(scene, "Assets/Scenes/Pathfinding.unity");
        }
    }
}
