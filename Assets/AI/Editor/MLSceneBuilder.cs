using AIGames.MachineLearning;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using static AIGames.EditorTools.SceneBuilderUtils;

namespace AIGames.EditorTools
{
    public static class MLSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MLRollerBall.unity";
        public const string ModelPath = "Assets/AI/MachineLearning/Models/RollerBall.onnx";
        const int AreasPerSide = 4;
        const float AreaSpacing = 14f;

        [MenuItem("AI in Games/Build ML RollerBall Scene")]
        public static void Build()
        {
            var scene = NewScene();
            AddCamera(new Vector3(0, 46, -40), new Vector3(50, 0, 0), new Color(0.12f, 0.13f, 0.18f));
            AddSun();

            var floorMat = Mat("ML_Floor", new Color(0.75f, 0.78f, 0.82f));
            var agentMat = Mat("ML_Agent", new Color(0.20f, 0.50f, 0.95f));
            var targetMat = Mat("ML_Target", new Color(0.95f, 0.55f, 0.15f));
            var model = AssetDatabase.LoadAssetAtPath<Object>(ModelPath);

            BehaviorParameters first = null;
            float offset = (AreasPerSide - 1) * AreaSpacing / 2;
            for (int x = 0; x < AreasPerSide; x++)
            {
                for (int z = 0; z < AreasPerSide; z++)
                {
                    var area = new GameObject($"Training Area {x * AreasPerSide + z}").transform;
                    area.position = new Vector3(x * AreaSpacing - offset, 0, z * AreaSpacing - offset);

                    Primitive(PrimitiveType.Plane, "Floor", Vector3.zero, Vector3.one, floorMat, area);
                    var target = Primitive(PrimitiveType.Cube, "Target", new Vector3(3, 0.5f, 3), Vector3.one, targetMat, area);
                    // Kenney treasure chest as the visual of the target cube.
                    target.GetComponent<Renderer>().enabled = false;
                    var chest = KenneyModelSetup.AddCenteredModel(target.transform, "Assets/ThirdParty/Kenney/MiniDungeon/chest.fbx", 1.2f, alignBottom: true);
                    chest.transform.localPosition = new Vector3(0, -0.5f, 0);

                    var agentGo = Primitive(PrimitiveType.Sphere, "RollerAgent", new Vector3(0, 0.5f, 0), Vector3.one, agentMat, area);
                    agentGo.AddComponent<Rigidbody>();
                    var bp = agentGo.AddComponent<BehaviorParameters>();
                    bp.BehaviorName = "RollerBall";
                    bp.BrainParameters.VectorObservationSize = 8;
                    bp.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(2);
                    if (model != null)
                        AssignModel(bp, model);

                    var agent = agentGo.AddComponent<RollerAgent>();
                    agent.target = target.transform;
                    agent.MaxStep = 1000;
                    agentGo.AddComponent<DecisionRequester>().DecisionPeriod = 10;

                    if (first == null)
                        first = bp;
                }
            }

            new GameObject("Roller Stats").AddComponent<RollerStats>().sampleAgent = first;
            SaveScene(scene, ScenePath);
        }

        static void AssignModel(BehaviorParameters bp, Object model)
        {
            // Model is typed as the inference engine's ModelAsset; assign through SerializedObject
            // so this code does not depend on the exact type name of the package version.
            var so = new SerializedObject(bp);
            so.FindProperty("m_Model").objectReferenceValue = model;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("AI in Games/Build RollerBall Training Player")]
        public static string BuildTrainingPlayer()
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/RollerBall/RollerBall.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            return $"{report.summary.result} in {report.summary.totalTime}, errors: {report.summary.totalErrors}";
        }
    }
}
