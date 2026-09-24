using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIGames.EditorTools
{
    /// <summary>
    /// Shared helpers for the editor scripts that build the demo scenes.
    /// </summary>
    public static class SceneBuilderUtils
    {
        const string MaterialFolder = "Assets/AI/Materials";

        public static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            AddToBuildSettings(path);
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path))
                return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static Material Mat(string name, Color color, bool unlit = false)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/AI", "Materials");

            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Camera AddCamera(Vector3 position, Vector3 euler, Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.fieldOfView = 55;
            cam.farClipPlane = 300;
            go.AddComponent<AudioListener>();
            return cam;
        }

        public static Light AddSun()
        {
            var go = new GameObject("Directional Light");
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
            return light;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Vector3 pos, Vector3 scale,
            Material mat, Transform parent = null, int layer = 0)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.layer = layer;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
