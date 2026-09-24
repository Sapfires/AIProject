using AIGames.Pathfinding;
using AIGames.Pathfinding.Gameplay;
using UnityEditor;
using UnityEngine;
using static AIGames.EditorTools.SceneBuilderUtils;

namespace AIGames.EditorTools
{
    /// <summary>
    /// Builds the Pathfinder gameplay scene (exercises 1-5): a dungeon made of Kenney tiles,
    /// robot guards patrolling on A* paths and a ninja player.
    /// </summary>
    public static class GameplaySceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/PathfinderGameplay.unity";
        const string Dungeon = "Assets/ThirdParty/Kenney/MiniDungeon/";
        const int UnwalkableLayer = PathfindingSceneBuilder.UnwalkableLayer;
        const int Half = 16;          // level spans -16..16 in x and z
        const float WallHeight = 1.8f;

        // Wall rectangles in cell coordinates (xMin, zMin, xMax, zMax), inclusive.
        static readonly RectInt[] Walls =
        {
            Cells(-16, -16, 15, -16), Cells(-16, 15, 15, 15),     // outer walls
            Cells(-16, -15, -16, 14), Cells(15, -15, 15, 14),
            Cells(-8, -15, -8, 4),                                // left room divider
            Cells(0, -3, 0, 14),                                  // centre wall
            Cells(8, -15, 8, 0),                                  // treasure room wall
            Cells(8, 6, 10, 6), Cells(13, 6, 14, 6),              // wall with a door
            Cells(-15, 7, -12, 7),
        };

        static readonly Vector2[] Columns = { new Vector2(-12, -4), new Vector2(4, -10), new Vector2(-4, 10), new Vector2(11, 11) };

        static RectInt Cells(int xMin, int zMin, int xMax, int zMax) => new RectInt(xMin, zMin, xMax - xMin + 1, zMax - zMin + 1);

        [MenuItem("AI in Games/Build Pathfinder Gameplay Scene (Exercises 1-5)")]
        public static void Build()
        {
            KenneyModelSetup.Setup();
            var scene = NewScene();
            AddCamera(new Vector3(0, 30, -24), new Vector3(55, 0, 0), new Color(0.08f, 0.08f, 0.1f));
            var sun = AddSun();
            sun.intensity = 1.6f;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.transform.rotation = Quaternion.Euler(55, -40, 0);
            RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.40f);

            var grid = BuildLevel();
            grid.gameObject.AddComponent<AStarPathfinder>();
            grid.gameObject.AddComponent<PathRequestManager>().requestsPerFrame = 4;

            // Exercise 1: robot guards patrolling on A* paths.
            AddEnemy("Robot Guard 1", "g", new Color(1f, 0.35f, 0.3f),
                new[] { new Vector3(-12, 0, -12), new Vector3(-12, 0, 1), new Vector3(-4, 0, 1), new Vector3(-4, 0, -12) });
            AddEnemy("Robot Guard 2", "h", new Color(0.7f, 0.45f, 1f),
                new[] { new Vector3(4, 0, 12), new Vector3(12, 0, 12), new Vector3(11.5f, 0, 2), new Vector3(4, 0, 2) });

            SaveScene(scene, ScenePath);
        }

        static PathGrid BuildLevel()
        {
            var gridGo = new GameObject("A* Grid");
            var grid = gridGo.AddComponent<PathGrid>();
            grid.unwalkableMask = 1 << UnwalkableLayer;
            grid.gridWorldSize = new Vector2(Half * 2, Half * 2);
            grid.nodeRadius = 0.25f;
            grid.obstacleProximityPenalty = 30;
            grid.penaltyBlurSize = 1;
            grid.displayGizmos = false;

            var level = new GameObject("Dungeon").transform;

            // Floor: 2x2 m tiles plus a collider for mouse raycasts.
            var floor = new GameObject("Floor").transform;
            floor.SetParent(level);
            var floorPrefab = Load("floor");
            for (int x = -Half; x < Half; x += 2)
                for (int z = -Half; z < Half; z += 2)
                    Place(floorPrefab, floor, new Vector3(x + 1, 0, z + 1), new Vector3(2, 1, 2));
            var floorCollider = new GameObject("Floor Collider").AddComponent<BoxCollider>();
            floorCollider.transform.SetParent(floor);
            floorCollider.center = new Vector3(0, -0.05f, 0);
            floorCollider.size = new Vector3(Half * 2, 0.1f, Half * 2);

            // Walls: one invisible box collider per rectangle, Kenney wall blocks as visuals.
            var walls = new GameObject("Walls").transform;
            walls.SetParent(level);
            var wallPrefab = Load("wall");
            for (int i = 0; i < Walls.Length; i++)
            {
                RectInt r = Walls[i];
                var wall = new GameObject($"Wall {i}") { layer = UnwalkableLayer }.transform;
                wall.SetParent(walls);
                wall.position = new Vector3(r.x + r.width / 2f, WallHeight / 2, r.y + r.height / 2f);
                wall.gameObject.AddComponent<BoxCollider>().size = new Vector3(r.width, WallHeight, r.height);
                for (int x = r.xMin; x < r.xMax; x++)
                    for (int z = r.yMin; z < r.yMax; z++)
                        Place(wallPrefab, wall, new Vector3(x + 0.5f, 0, z + 0.5f), new Vector3(1, WallHeight / 1.1f, 1));
            }

            // Columns and props: obstacles for the pathfinding.
            var props = new GameObject("Props").transform;
            props.SetParent(level);
            foreach (var c in Columns)
                AddObstacle(props, "column", new Vector3(c.x, 0, c.y), new Vector3(1.6f, 1.6f, 1.6f), new Vector3(0.8f, WallHeight, 0.8f));
            AddObstacle(props, "barrel", new Vector3(-14, 0, -2), Vector3.one * 1.5f, new Vector3(0.8f, 1, 0.8f));
            AddObstacle(props, "barrel", new Vector3(6, 0, 13.5f), Vector3.one * 1.5f, new Vector3(0.8f, 1, 0.8f));
            AddObstacle(props, "table", new Vector3(-12, 0, 11), Vector3.one * 1.5f, new Vector3(1.5f, 1, 1.5f));
            AddObstacle(props, "rocks", new Vector3(3, 0, -4), Vector3.one * 1.5f, new Vector3(1.2f, 1, 1.2f));
            Place(Load("banner"), props, new Vector3(-4, 0, 14.4f), Vector3.one * 1.5f);
            Place(Load("banner"), props, new Vector3(4, 0, 14.4f), Vector3.one * 1.5f);
            return grid;
        }

        static void AddObstacle(Transform parent, string model, Vector3 pos, Vector3 scale, Vector3 colliderSize)
        {
            var go = new GameObject(model) { layer = UnwalkableLayer };
            go.transform.SetParent(parent);
            go.transform.position = pos + Vector3.up * colliderSize.y / 2;
            go.AddComponent<BoxCollider>().size = colliderSize;
            Place(Load(model), go.transform, pos, scale);
        }

        public static GameObject AddCharacter(string name, string letter, Vector3 position, int layer)
        {
            var root = new GameObject(name) { layer = layer };
            root.transform.position = position;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.9f, 0);
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;

            var model = KenneyModelSetup.AddCharacterModel(root.transform, letter, 1.8f);
            var mover = root.AddComponent<PathMover>();
            mover.animator = model.GetComponent<Animator>();
            return root;
        }

        static LineRenderer AddPathLine(Transform parent, Color color)
        {
            var line = new GameObject("Path Line").AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.useWorldSpace = true;
            line.sharedMaterial = Mat($"GP_Line_{ColorUtility.ToHtmlStringRGB(color)}", color, unlit: true);
            line.widthMultiplier = 0.12f;
            line.positionCount = 0;
            return line;
        }

        static EnemyAI AddEnemy(string name, string letter, Color color, Vector3[] points)
        {
            var patrol = new GameObject($"{name} Patrol").AddComponent<PatrolPath>();
            patrol.gizmoColor = color;
            for (int i = 0; i < points.Length; i++)
            {
                var p = new GameObject($"Point {i}").transform;
                p.SetParent(patrol.transform);
                p.position = points[i];
            }

            var go = AddCharacter(name, letter, points[0], PathfindingSceneBuilder.MudLayer + 1);
            go.GetComponent<PathMover>().pathLine = AddPathLine(go.transform, color);
            var enemy = go.AddComponent<EnemyAI>();
            enemy.patrol = patrol;
            return enemy;
        }

        static GameObject Load(string model) => AssetDatabase.LoadAssetAtPath<GameObject>(Dungeon + model + ".fbx");

        static GameObject Place(GameObject prefab, Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.isStatic = true;
            return go;
        }
    }
}
