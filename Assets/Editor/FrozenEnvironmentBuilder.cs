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
        const string SnowShaderPath = "Assets/ThirdParty/BruteForce/SnowIce/URP/BF_SnowIceNoTessURP.shader";

        [MenuItem("RealRail/Build Frozen Environment V2")]
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
            var heroSnow = LoadOrCreateHeroSnowMaterial();

            var continuation = CreateGroup("FrozenApproach", frozen);
            CreateCube("SnowApproach", continuation, new Vector3(0f, -0.42f, 50f), new Vector3(30f, 0.55f, 28f), snow);
            CreateCube("IceRiver", continuation, new Vector3(0f, -0.12f, 47f), new Vector3(5.5f, 0.08f, 16f), ice);

            var skyline = CreateGroup("ToonSkyline", frozen);
            var hero = CreateToonPrefab("HeroCliff_SnowOverlay", "Prefabs/Rocks/TFF_Rock_Large_06A.prefab", skyline,
                new Vector3(-8.4f, -1.6f, 57f), new Vector3(0f, 13f, 0f), new Vector3(2.15f, 2.15f, 2.15f));
            CreateHeroSnowOverlay(hero, heroSnow);
            CreateToonPrefab("SecondaryCliff_Right", "Prefabs/Rocks/TFF_Rock_Large_05A.prefab", skyline,
                new Vector3(10.5f, -1.4f, 60f), new Vector3(0f, -29f, 0f), new Vector3(2.55f, 2.55f, 2.55f));

            var rocks = CreateGroup("ToonMidgroundRocks", frozen);
            CreateToonPrefab("Rock_Left_Midground", "Prefabs/Rocks/TFF_Rock_Medium_07A.prefab", rocks,
                new Vector3(-10.6f, -0.1f, 32f), new Vector3(0f, 115f, 0f), new Vector3(2.2f, 2.2f, 2.2f));
            CreateToonPrefab("Rock_Right_Midground", "Prefabs/Rocks/TFF_Rock_Medium_07A.prefab", rocks,
                new Vector3(10.8f, -0.1f, 40f), new Vector3(0f, -62f, 0f), new Vector3(2.45f, 2.45f, 2.45f));

            var vegetation = CreateGroup("ToonSparseVegetation", frozen);
            CreateToonPrefab("Pine_Left", "Prefabs/Vegetation/Trees/TFF_Pine_Tree_03A.prefab", vegetation,
                new Vector3(-12.4f, -0.2f, 37f), new Vector3(0f, 12f, 0f), new Vector3(2.15f, 2.7f, 2.15f));
            CreateToonPrefab("Pine_Right", "Prefabs/Vegetation/Trees/TFF_Pine_Tree_05A.prefab", vegetation,
                new Vector3(12.7f, -0.2f, 34f), new Vector3(0f, -18f, 0f), new Vector3(2f, 2.45f, 2f));

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

        static GameObject CreateToonPrefab(string name, string relativePath, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ToonPath + relativePath);
            if (source == null)
            {
                throw new System.InvalidOperationException("Missing approved Toon Fantasy Nature asset: " + relativePath);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localEulerAngles = rotation;
            instance.transform.localScale = scale;

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            }

            return instance;
        }

        static void CreateHeroSnowOverlay(GameObject hero, Material snowMaterial)
        {
            var overlays = new System.Collections.Generic.Dictionary<Renderer, Renderer>();
            foreach (var source in hero.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                var overlay = new GameObject("SnowOverlay");
                overlay.transform.SetParent(source.transform, false);
                overlay.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                overlay.AddComponent<MeshRenderer>().sharedMaterial = snowMaterial;
                GameObjectUtility.SetStaticEditorFlags(overlay, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
                overlays[source] = overlay.GetComponent<Renderer>();
            }

            var lodGroup = hero.GetComponentInChildren<LODGroup>();
            if (lodGroup == null)
            {
                return;
            }

            var lods = lodGroup.GetLODs();
            for (var i = 0; i < lods.Length; i++)
            {
                var renderers = new System.Collections.Generic.List<Renderer>(lods[i].renderers);
                foreach (var renderer in lods[i].renderers)
                {
                    if (overlays.TryGetValue(renderer, out var overlay))
                    {
                        renderers.Add(overlay);
                    }
                }
                lods[i].renderers = renderers.ToArray();
            }
            lodGroup.SetLODs(lods);
        }

        static Material LoadOrCreateHeroSnowMaterial()
        {
            const string path = "Assets/Materials/FrozenHeroSnowOverlay.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SnowShaderPath);
            if (shader == null)
            {
                throw new System.InvalidOperationException("Missing Brute Force no-tessellation URP shader.");
            }

            material = new Material(shader) { name = "FrozenHeroSnowOverlay", renderQueue = 3000 };
            material.SetColor("_Color", new Color(0.62f, 0.75f, 0.82f, 0.58f));
            material.SetColor("_TransitionColor", new Color(0.75f, 0.86f, 0.92f, 0.45f));
            material.SetFloat("_ISADD", 1f);
            material.EnableKeyword("IS_ADD");
            material.SetFloat("_USERT", 0f);
            material.DisableKeyword("USE_RT");
            material.SetFloat("_USEFOG", 0f);
            material.DisableKeyword("USE_FOG");
            material.SetFloat("_UsePR", 0f);
            material.SetFloat("_DisplacementStrength", 0f);
            material.SetFloat("_DisplacementOffset", 0f);
            material.SetFloat("_AddSnowStrength", 0.45f);
            material.SetFloat("_RemoveSnowStrength", 0.65f);
            material.SetFloat("_SnowScale", 0.85f);
            AssetDatabase.CreateAsset(material, path);
            return material;
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
