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

        [MenuItem("RealRail/Build Frozen Environment V1")]
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
            var snowShade = LoadOrCreateMaterial("Assets/Materials/FrozenSnowShade.mat", new Color(0.41f, 0.61f, 0.70f), 0.06f);
            var ice = LoadOrCreateMaterial("Assets/Materials/FrozenIce.mat", new Color(0.30f, 0.72f, 0.82f), 0.42f);
            var rock = LoadOrCreateMaterial("Assets/Materials/FrozenRock.mat", new Color(0.20f, 0.29f, 0.36f), 0.12f);
            var rockLight = LoadOrCreateMaterial("Assets/Materials/FrozenRockLight.mat", new Color(0.42f, 0.54f, 0.61f), 0.08f);
            var frost = LoadOrCreateMaterial("Assets/Materials/FrozenFrost.mat", new Color(0.78f, 0.91f, 0.95f), 0.18f);

            var continuation = CreateGroup("FrozenApproach", frozen);
            CreateCube("SnowApproach", continuation, new Vector3(0f, -0.42f, 62f), new Vector3(34f, 0.55f, 45f), snow);
            CreateCube("IceRiver", continuation, new Vector3(0f, -0.12f, 53f), new Vector3(7.5f, 0.08f, 27f), ice);
            CreateCube("LeftSnowbank", continuation, new Vector3(-11.5f, 0.05f, 49f), new Vector3(11f, 0.9f, 22f), snowShade);
            CreateCube("RightSnowbank", continuation, new Vector3(11.5f, 0.05f, 49f), new Vector3(11f, 0.9f, 22f), snowShade);

            var iceFormations = CreateGroup("IceFormations", frozen);
            CreateIceSpire(iceFormations, new Vector3(-13.5f, 1.25f, 34f), 1.0f, 3.0f, ice, frost);
            CreateIceSpire(iceFormations, new Vector3(13.5f, 1.1f, 38f), 0.85f, 2.7f, ice, frost);
            CreateIceSpire(iceFormations, new Vector3(-15f, 1.4f, 48f), 1.2f, 4.2f, ice, frost);
            CreateIceSpire(iceFormations, new Vector3(15f, 1.45f, 51f), 1.25f, 4.4f, ice, frost);

            var mountains = CreateGroup("DistantMountains", frozen);
            CreateMountain(mountains, new Vector3(-22f, 5.0f, 70f), 10f, 13f, rock, rockLight, frost);
            CreateMountain(mountains, new Vector3(-11f, 6.4f, 75f), 12f, 17f, rock, rockLight, frost);
            CreateMountain(mountains, new Vector3(1f, 5.2f, 79f), 14f, 16f, rock, rockLight, frost);
            CreateMountain(mountains, new Vector3(14f, 7.1f, 74f), 13f, 19f, rock, rockLight, frost);
            CreateMountain(mountains, new Vector3(25f, 4.8f, 69f), 10f, 13f, rock, rockLight, frost);

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

        static void CreateMountain(Transform parent, Vector3 position, float width, float height, Material rock, Material rockLight, Material frost)
        {
            var root = CreateGroup("Mountain", parent);
            root.localPosition = position;
            CreateCylinder("Base", root, new Vector3(0f, -height * 0.28f, 0f), new Vector3(width, height * 0.44f, width * 0.7f), rock);
            CreateCylinder("Mid", root, new Vector3(0.45f, height * 0.02f, 0f), new Vector3(width * 0.68f, height * 0.42f, width * 0.52f), rockLight);
            CreateCylinder("Peak", root, new Vector3(-0.25f, height * 0.31f, 0f), new Vector3(width * 0.33f, height * 0.36f, width * 0.27f), frost);
        }

        static void CreateIceSpire(Transform parent, Vector3 position, float width, float height, Material ice, Material frost)
        {
            var root = CreateGroup("IceSpire", parent);
            root.localPosition = position;
            CreateCylinder("Body", root, new Vector3(0f, 0f, 0f), new Vector3(width, height, width * 0.75f), ice);
            CreateCylinder("Tip", root, new Vector3(0f, height * 0.55f, 0f), new Vector3(width * 0.45f, height * 0.45f, width * 0.32f), frost);
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

        static void CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var shape = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
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
