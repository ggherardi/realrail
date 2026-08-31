using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RealRail.Editor
{
    /// <summary>
    /// Authors the presentation-only Frozen theme underneath VisualEnvironment.
    /// GameplayArena and LaneLayout deliberately remain outside this builder.
    /// </summary>
    public static class FrozenEnvironmentBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string ToonPath = "Assets/ThirdParty/ToonFantasyNature/";
        [MenuItem("RealRail/Build Frozen Environment Baseline")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var visualEnvironment = FindRoot(scene, "VisualEnvironment").transform;
            var existing = visualEnvironment.Find("Frozen");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var frozen = new GameObject("Frozen").transform;
            frozen.SetParent(visualEnvironment, false);

            var snow = LoadOrCreateMaterial("Assets/Materials/FrozenSnow.mat", new Color(0.67f, 0.82f, 0.88f), 0.08f);
            var ice = LoadOrCreateMaterial("Assets/Materials/FrozenIce.mat", new Color(0.30f, 0.72f, 0.82f), 0.42f);
            var continuation = CreateGroup("GroundContinuation", frozen);
            CreateCube("SnowApproach", continuation, new Vector3(0f, -0.42f, 50f), new Vector3(30f, 0.55f, 28f), snow);
            CreateCube("IceRiver", continuation, new Vector3(0f, -0.12f, 47f), new Vector3(5.5f, 0.08f, 16f), ice);

            ApplyFrozenAtmosphere();
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(ToonPath + "Skyboxes/TFF_Skybox_Day_01A.mat");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void ApplyFrozenAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.53f, 0.68f, 0.76f);
            RenderSettings.fogStartDistance = 32f;
            RenderSettings.fogEndDistance = 105f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.56f, 0.69f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.38f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.18f, 0.23f);
            RenderSettings.ambientIntensity = 0.9f;
        }

        static Transform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(shape, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
        }

        static Material LoadOrCreateMaterial(string path, Color color, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
