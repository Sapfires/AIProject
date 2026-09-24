using AIGames.Swarm;
using UnityEditor;
using UnityEngine;
using static AIGames.EditorTools.SceneBuilderUtils;

namespace AIGames.EditorTools
{
    public static class SwarmSceneBuilder
    {
        const string SpaceKit = "Assets/ThirdParty/Kenney/SpaceKit/";
        public static float ShipYaw = 0f;   // rotation that makes the ship models face +Z
        const int ObstacleLayer = PathfindingSceneBuilder.UnwalkableLayer;
        static readonly Vector3 ArenaSize = new Vector3(40, 14, 40);

        [MenuItem("AI in Games/Build Swarm Scene")]
        public static void Build()
        {
            var scene = NewScene();
            AddCamera(new Vector3(0, 17, -28), new Vector3(27, 0, 0), new Color(0.05f, 0.06f, 0.12f));
            AddSun();

            // Visible ground, invisible walls and ceiling that keep the swarm inside the arena.
            var arena = new GameObject("Arena").transform;
            Primitive(PrimitiveType.Cube, "Ground", new Vector3(0, -0.5f, 0), new Vector3(ArenaSize.x + 4, 1, ArenaSize.z + 4),
                Mat("SW_Ground", new Color(0.23f, 0.22f, 0.27f)), arena, ObstacleLayer);
            AddInvisibleWall(arena, "Ceiling", new Vector3(0, ArenaSize.y + 0.5f, 0), new Vector3(ArenaSize.x, 1, ArenaSize.z));
            AddInvisibleWall(arena, "Wall N", new Vector3(0, ArenaSize.y / 2, ArenaSize.z / 2 + 0.5f), new Vector3(ArenaSize.x, ArenaSize.y, 1));
            AddInvisibleWall(arena, "Wall S", new Vector3(0, ArenaSize.y / 2, -ArenaSize.z / 2 - 0.5f), new Vector3(ArenaSize.x, ArenaSize.y, 1));
            AddInvisibleWall(arena, "Wall E", new Vector3(ArenaSize.x / 2 + 0.5f, ArenaSize.y / 2, 0), new Vector3(1, ArenaSize.y, ArenaSize.z));
            AddInvisibleWall(arena, "Wall W", new Vector3(-ArenaSize.x / 2 - 0.5f, ArenaSize.y / 2, 0), new Vector3(1, ArenaSize.y, ArenaSize.z));

            var rockMat = Mat("SW_Rock", new Color(0.50f, 0.48f, 0.45f));
            var obstacles = new GameObject("Obstacles").transform;
            Vector3[] pillars = { new Vector3(-8, 0, 4), new Vector3(7, 0, -5), new Vector3(0, 0, 12), new Vector3(-13, 0, -9), new Vector3(13, 0, 9) };
            for (int i = 0; i < pillars.Length; i++)
            {
                float h = 6 + i % 3 * 2.5f;
                var pillar = Primitive(PrimitiveType.Cylinder, $"Pillar {i}", pillars[i] + Vector3.up * h / 2, new Vector3(2.2f, h / 2, 2.2f),
                    rockMat, obstacles, ObstacleLayer);
                // Kenney crystal rock as the visual, the cylinder stays as the (invisible) collider.
                pillar.GetComponent<Renderer>().enabled = false;
                KenneyModelSetup.AddFittedModel(pillar.transform.parent, SpaceKit + (i % 2 == 0 ? "rock_crystalsLargeA" : "rock_crystalsLargeB") + ".fbx",
                    pillar.transform.position, new Vector3(2.6f, h, 2.6f)).name = $"Crystal Rock {i}";
            }
            AddMeteor(obstacles, "Rock", new Vector3(-2, 5, -4), 4, rockMat);
            AddMeteor(obstacles, "Rock 2", new Vector3(10, 9, 3), 3, rockMat);

            var target = Primitive(PrimitiveType.Sphere, "Swarm Target", new Vector3(0, 6, 0), Vector3.one * 0.6f,
                Mat("SW_Target", new Color(1f, 0.95f, 0.4f), unlit: true));
            Object.DestroyImmediate(target.GetComponent<Collider>());
            target.AddComponent<SwarmTarget>();

            var predator = new GameObject("Predator");
            predator.transform.position = new Vector3(15, 8, -15);
            var predatorModel = KenneyModelSetup.AddCenteredModel(predator.transform, SpaceKit + "craft_racer.fbx", 2.4f);
            predatorModel.transform.localRotation = Quaternion.Euler(0, ShipYaw, 0);

            var swarmGo = new GameObject("Swarm");
            swarmGo.transform.position = new Vector3(0, 6, -6);
            var swarm = swarmGo.AddComponent<SwarmManager>();
            swarm.boidCount = 180;
            swarm.boidMaterial = Mat("SW_Boid", Color.white);
            string[] ships = { "craft_speederA", "craft_speederB", "craft_speederC", "craft_speederD" };
            swarm.boidModels = new GameObject[ships.Length];
            for (int i = 0; i < ships.Length; i++)
                swarm.boidModels[i] = KenneyModelSetup.SaveCenteredPrefab(SpaceKit + ships[i] + ".fbx",
                    $"Assets/AI/Swarm/Prefabs/{ships[i]}.prefab", 0.8f, ShipYaw);
            swarm.settings.obstacleMask = 1 << ObstacleLayer;
            swarm.target = target.transform;
            swarm.predator = predator.transform;
            swarm.colors = new Gradient();
            swarm.colors.SetKeys(
                new[] { new GradientColorKey(new Color(0.15f, 0.35f, 0.85f), 0), new GradientColorKey(new Color(0.2f, 0.85f, 0.9f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });

            var hunter = predator.AddComponent<Predator>();
            hunter.swarm = swarm;

            SaveScene(scene, "Assets/Scenes/Swarm.unity");
        }

        static void AddMeteor(Transform parent, string name, Vector3 centre, float size, Material mat)
        {
            var sphere = Primitive(PrimitiveType.Sphere, name, centre, Vector3.one * size, mat, parent, ObstacleLayer);
            sphere.GetComponent<Renderer>().enabled = false;
            KenneyModelSetup.AddFittedModel(parent, SpaceKit + "meteor_detailed.fbx", centre, Vector3.one * size * 1.1f).name = name + " Model";
        }

        static void AddInvisibleWall(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name) { layer = ObstacleLayer };
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = size;
        }
    }
}
