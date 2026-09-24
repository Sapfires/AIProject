using AIGames.Pathfinding;
using UnityEditor;
using UnityEngine;
using static AIGames.EditorTools.SceneBuilderUtils;

namespace AIGames.EditorTools
{
    public static class PathfindingSceneBuilder
    {
        public const int UnwalkableLayer = 8;
        public const int MudLayer = 9;

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

        // Mud patches (part 4): walkable, but with a movement penalty.
        static readonly Vector4[] MudPatches =
        {
            new Vector4(10, 0, 8, 10),
            new Vector4(-4, 2, 6, 8),
            new Vector4(-15, -12, 7, 6),
            new Vector4(12, 17, 10, 5),
        };

        [MenuItem("AI in Games/Build Pathfinding Scene (Parts 1-3)")]
        public static void Build()
        {
            var scene = NewScene();
            var (grid, visualizer) = BuildLevel();

            // Part 2: A* between a seeker and a target.
            var pathfinder = grid.gameObject.AddComponent<AStarPathfinder>();
            var seeker = new GameObject("Seeker");
            seeker.transform.position = new Vector3(-16, 0, -16);
            seeker.transform.rotation = Quaternion.Euler(0, 45, 0);
            KenneyModelSetup.AddCharacterModel(seeker.transform, "c", 1.8f);
            var target = Primitive(PrimitiveType.Sphere, "Target", new Vector3(16, 0.75f, 16), Vector3.one * 1.5f,
                Mat("PF_Target", new Color(0.90f, 0.25f, 0.25f)));

            var demo = new GameObject("Pathfinding Demo").AddComponent<PathfindingDemo>();
            demo.seeker = seeker.transform;
            demo.target = target.transform;
            demo.pathfinding = pathfinder;
            demo.visualizer = visualizer;
            demo.pathLine = AddLine("Path Line", Mat("PF_PathLine", new Color(0.1f, 0.3f, 0.8f), unlit: true));

            SaveScene(scene, "Assets/Scenes/Pathfinding.unity");
        }

        [MenuItem("AI in Games/Build Pathfinding Units Scene (Part 4)")]
        public static void BuildUnits()
        {
            var scene = NewScene();
            var (grid, _) = BuildLevel();

            grid.walkableRegions = new[]
            {
                new PathGrid.TerrainType { terrainMask = 1 << MudLayer, terrainPenalty = 20 }
            };
            grid.obstacleProximityPenalty = 60;
            grid.penaltyBlurSize = 2;

            var mud = new GameObject("Mud").transform;
            for (int i = 0; i < MudPatches.Length; i++)
            {
                var m = MudPatches[i];
                var patch = Primitive(PrimitiveType.Cube, $"Mud {i}", new Vector3(m.x, 0.02f, m.y), new Vector3(m.z, 0.04f, m.w),
                    Mat("PF_Mud", new Color(0.45f, 0.33f, 0.2f)), mud, MudLayer);
                // The floor texture already shows the penalty map, the mud collider is only for the raycast.
                patch.GetComponent<Renderer>().enabled = false;
            }

            grid.gameObject.AddComponent<AStarPathfinder>();
            grid.gameObject.AddComponent<PathRequestManager>();

            var target = Primitive(PrimitiveType.Sphere, "Target", new Vector3(16, 0.75f, 16), Vector3.one * 1.5f,
                Mat("PF_Target", new Color(0.90f, 0.25f, 0.25f)));
            var mover = target.AddComponent<TargetMover>();
            mover.floorMask = 1 << 0;
            var patrol = new GameObject("Target Patrol").transform;
            Vector3[] points = { new Vector3(16, 0, 16), new Vector3(-16, 0, 16), new Vector3(8, 0, -16) };
            mover.patrolPoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var p = new GameObject($"Point {i}").transform;
                p.SetParent(patrol);
                p.position = points[i];
                mover.patrolPoints[i] = p;
            }

            Vector3[] spawns = { new Vector3(-17, 0, -17), new Vector3(17, 0, -17), new Vector3(-17, 0, 6), new Vector3(0, 0, -5) };
            string[] characters = { "c", "j", "d", "e" };   // Kenney Blocky Characters matching the line colours
            Color[] colors =
            {
                new Color(0.25f, 0.75f, 0.35f), new Color(0.25f, 0.55f, 0.95f),
                new Color(0.95f, 0.65f, 0.15f), new Color(0.70f, 0.35f, 0.90f)
            };
            var units = new GameObject("Units").transform;
            for (int i = 0; i < spawns.Length; i++)
            {
                var go = new GameObject($"Unit {i}");
                go.transform.SetParent(units);
                go.transform.position = spawns[i];
                var model = KenneyModelSetup.AddCharacterModel(go.transform, characters[i], 1.8f);
                var unit = go.AddComponent<Unit>();
                unit.target = target.transform;
                unit.speed = 5f + i * 0.5f;
                unit.animator = model.GetComponent<Animator>();
                unit.pivotHeight = 0;
                unit.pathLine = AddLine($"Unit {i} Path", Mat($"PF_UnitLine{i}", colors[i], unlit: true));
                unit.pathLine.transform.SetParent(units);
            }

            SaveScene(scene, "Assets/Scenes/PathfindingUnits.unity");
        }

        static (PathGrid, GridVisualizer) BuildLevel()
        {
            AddCamera(new Vector3(-4, 42, -19), new Vector3(66, 0, 0), new Color(0.13f, 0.15f, 0.19f));
            AddSun();

            var gridGo = new GameObject("A* Grid");
            var grid = gridGo.AddComponent<PathGrid>();
            grid.unwalkableMask = 1 << UnwalkableLayer;
            grid.gridWorldSize = new Vector2(40, 40);
            grid.nodeRadius = 0.25f;

            var floor = Primitive(PrimitiveType.Quad, "Floor", Vector3.zero, new Vector3(40, 40, 1),
                Mat("PF_Floor", Color.white, unlit: true), gridGo.transform);
            floor.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var visualizer = gridGo.AddComponent<GridVisualizer>();
            visualizer.floor = floor.GetComponent<Renderer>();
            visualizer.pixelsPerNode = 6;

            var wallMat = Mat("PF_Wall", new Color(0.35f, 0.38f, 0.45f));
            var obstacles = new GameObject("Obstacles").transform;
            for (int i = 0; i < Walls.Length; i++)
            {
                var w = Walls[i];
                Primitive(PrimitiveType.Cube, $"Wall {i}", new Vector3(w.x, 1, w.y), new Vector3(w.z, 2, w.w),
                    wallMat, obstacles, UnwalkableLayer);
            }
            return (grid, visualizer);
        }

        static LineRenderer AddLine(string name, Material mat)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.sharedMaterial = mat;
            line.widthMultiplier = 0.25f;
            line.positionCount = 0;
            return line;
        }
    }
}
