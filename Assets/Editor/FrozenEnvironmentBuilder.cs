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
        const string ModelsPath = "Assets/ThirdParty/Quaternius/FrozenEnvironment/Models/";

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
            var stone = LoadOrCreateMaterial("Assets/Materials/FrozenMountainStone.mat", new Color(0.25f, 0.33f, 0.39f), 0.05f);
            var dirt = LoadOrCreateMaterial("Assets/Materials/FrozenMountainDirt.mat", new Color(0.31f, 0.37f, 0.38f), 0.03f);
            var rock = LoadOrCreateMaterial("Assets/Materials/FrozenRock.mat", new Color(0.20f, 0.29f, 0.36f), 0.12f);
            var pine = LoadOrCreateMaterial("Assets/Materials/FrozenPine.mat", new Color(0.16f, 0.31f, 0.31f), 0.04f);

            var continuation = CreateGroup("FrozenApproach", frozen);
            CreateCube("SnowApproach", continuation, new Vector3(0f, -0.42f, 50f), new Vector3(30f, 0.55f, 28f), snow);
            CreateCube("IceRiver", continuation, new Vector3(0f, -0.12f, 47f), new Vector3(5.5f, 0.08f, 16f), ice);

            var mountains = CreateGroup("DistantMountains", frozen);
            CreateAsset("MountainGroup_Main", "Mountain_Group_1", mountains, new Vector3(-3f, 3f, 61f), new Vector3(0f, -8f, 0f), new Vector3(4f, 6.5f, 3.3f), stone, snow, dirt);
            CreateAsset("MountainLarge_Left", "MountainLarge_Single", mountains, new Vector3(-17f, 2f, 57f), new Vector3(0f, 28f, 0f), new Vector3(3.8f, 10f, 3.2f), stone, snow, dirt);
            CreateAsset("MountainGroup_Right", "Mountain_Group_2", mountains, new Vector3(16f, 2.5f, 59f), new Vector3(0f, -34f, 0f), new Vector3(3.8f, 6f, 3.3f), stone, snow, dirt);

            var rocks = CreateGroup("MidgroundRocks", frozen);
            CreateAsset("Rock_Left_Near", "Rock_Snow_4", rocks, new Vector3(-9.8f, 0.1f, 27f), new Vector3(0f, 142f, 0f), new Vector3(2.6f, 2.4f, 2.6f), rock, snow);
            CreateAsset("Rock_Left_Far", "Rock_Snow_1", rocks, new Vector3(-12.2f, 0.05f, 37f), new Vector3(0f, 38f, 0f), new Vector3(3.1f, 2.5f, 3.1f), rock, snow);
            CreateAsset("Rock_Left_Back", "Rock_Snow_6", rocks, new Vector3(-15.8f, 0.05f, 45f), new Vector3(0f, 233f, 0f), new Vector3(2.9f, 2.4f, 2.9f), rock, snow);
            CreateAsset("Rock_Right_Near", "Rock_Snow_6", rocks, new Vector3(9.8f, 0.05f, 29f), new Vector3(0f, 300f, 0f), new Vector3(2.3f, 2.0f, 2.3f), rock, snow);
            CreateAsset("Rock_Right_Far", "Rock_Snow_4", rocks, new Vector3(12.6f, 0.05f, 39f), new Vector3(0f, 85f, 0f), new Vector3(3.0f, 2.5f, 3.0f), rock, snow);
            CreateAsset("Rock_Right_Back", "Rock_Snow_1", rocks, new Vector3(16.5f, 0.05f, 47f), new Vector3(0f, 191f, 0f), new Vector3(3.4f, 2.8f, 3.4f), rock, snow);

            var vegetation = CreateGroup("SnowVegetation", frozen);
            CreateAsset("Pine_Left_Back", "PineTree_Snow_1", vegetation, new Vector3(-11.5f, 0.05f, 34f), new Vector3(0f, 18f, 0f), new Vector3(2.8f, 3.6f, 2.8f), dirt, pine, snow);
            CreateAsset("Pine_Right_Back", "PineTree_Snow_3", vegetation, new Vector3(11.5f, 0.05f, 36f), new Vector3(0f, 328f, 0f), new Vector3(2.7f, 3.5f, 2.7f), dirt, pine, snow);
            CreateAsset("Pine_Right_Far", "PineTree_Snow_1", vegetation, new Vector3(16f, 0.05f, 45f), new Vector3(0f, 342f, 0f), new Vector3(2.1f, 2.8f, 2.1f), dirt, pine, snow);

            ApplyFrozenAtmosphere();
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

        static void CreateAsset(string name, string assetName, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale, params Material[] materials)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsPath + assetName + ".fbx");
            if (source == null)
            {
                throw new System.InvalidOperationException("Missing approved Quaternius asset: " + assetName);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localEulerAngles = rotation;
            // Quaternius FBXs use a 100x imported root scale. Preserve that
            // conversion factor while applying the authored composition scale.
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                var assigned = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < assigned.Length; i++)
                {
                    assigned[i] = materials[Mathf.Min(i, materials.Length - 1)];
                }
                renderer.sharedMaterials = assigned;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            }
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
