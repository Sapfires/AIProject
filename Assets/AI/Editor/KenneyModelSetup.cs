using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AIGames.EditorTools
{
    /// <summary>
    /// Prepares the Kenney CC0 models (Assets/ThirdParty/Kenney) for the demos:
    /// generic rig with looping idle/walk/sprint clips and an Animator Controller
    /// that blends between them by speed.
    /// </summary>
    public static class KenneyModelSetup
    {
        public const string CharacterFolder = "Assets/ThirdParty/Kenney/BlockyCharacters";
        public const string ControllerPath = "Assets/AI/Common/Animation/BlockyCharacter.controller";
        static readonly string[] LoopClips = { "idle", "walk", "sprint" };

        [MenuItem("AI in Games/Setup Kenney Models")]
        public static void Setup()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { CharacterFolder }))
                SetupCharacterImporter(AssetDatabase.GUIDToAssetPath(guid));
            CreateController();
        }

        static void SetupCharacterImporter(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
                clip.loopTime = LoopClips.Contains(clip.name);
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static void CreateController()
        {
            string folder = System.IO.Path.GetDirectoryName(ControllerPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/AI/Common", "Animation");

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var clips = AssetDatabase.LoadAllAssetsAtPath(CharacterFolder + "/character-a.fbx")
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);

            var tree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(clips["idle"], 0f);
            tree.AddChild(clips["walk"], 2.5f);
            tree.AddChild(clips["sprint"], 5f);

            var state = controller.layers[0].stateMachine.AddState("Locomotion");
            state.motion = tree;
            controller.layers[0].stateMachine.defaultState = state;
            AssetDatabase.SaveAssets();
        }

        /// <summary>Instantiates a character model as a child of the given object, scaled to a height.</summary>
        public static GameObject AddCharacterModel(Transform parent, string letter, float height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterFolder}/character-{letter}.fbx");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * (height / 2.7f); // Kenney characters are 2.7 units tall
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            return model;
        }

        /// <summary>
        /// Places a model so that its bounds exactly fill the given size (non-uniform scale),
        /// centred on a position; used to put rock models over invisible primitive colliders.
        /// </summary>
        public static GameObject AddFittedModel(Transform parent, string assetPath, Vector3 centre, Vector3 size)
        {
            var pivot = AddCenteredModel(parent, assetPath, 1f).transform;
            // AddCenteredModel scaled uniformly to a max extent of 1; stretch to the target size.
            var bounds = RendererBounds(pivot.gameObject);
            Vector3 current = bounds.size;
            pivot.localScale = new Vector3(
                pivot.localScale.x * size.x / Mathf.Max(current.x, 0.0001f),
                pivot.localScale.y * size.y / Mathf.Max(current.y, 0.0001f),
                pivot.localScale.z * size.z / Mathf.Max(current.z, 0.0001f));
            pivot.position = centre;
            return pivot.gameObject;
        }

        static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        /// <summary>
        /// Saves a centred, scaled and rotated copy of a model as a prefab (used for the swarm ships).
        /// </summary>
        public static GameObject SaveCenteredPrefab(string assetPath, string prefabPath, float size, float yaw)
        {
            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            var model = AddCenteredModel(root.transform, assetPath, size);
            model.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            string folder = System.IO.Path.GetDirectoryName(prefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Instantiates a Kenney model under a pivot object so that its bounds are centred on
        /// the pivot (Space Kit models are authored with an offset) and scaled to a size.
        /// </summary>
        public static GameObject AddCenteredModel(Transform parent, string assetPath, float size, bool alignBottom = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var pivot = new GameObject("Model").transform;
            pivot.SetParent(parent, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pivot);
            model.transform.localPosition = Vector3.zero;

            var bounds = new Bounds(model.transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            Vector3 offset = model.transform.position - bounds.center;
            if (alignBottom)
                offset.y = model.transform.position.y - bounds.min.y;
            model.transform.position += offset;

            float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            pivot.localScale = Vector3.one * (size / Mathf.Max(maxExtent, 0.0001f));
            return pivot.gameObject;
        }
    }
}
