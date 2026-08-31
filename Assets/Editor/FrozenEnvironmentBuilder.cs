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
