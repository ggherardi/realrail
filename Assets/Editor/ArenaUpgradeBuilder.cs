using RealRail;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Editor
{
    public static class ArenaUpgradeBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const float LaneCenter = 3.25f;
        const float LaneWidth = 5.5f;

        [MenuItem("RealRail/Upgrade Gameplay Arena V1")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var environment = FindRoot(scene, "Environment");
            var layout = FindRoot(scene, "Systems").GetComponentInChildren<LaneLayout>(true);
            var leftLane = environment.transform.Find("LeftLane");
            var rightLane = environment.transform.Find("RightLane");
            var divider = environment.transform.Find("Divider");

            SetLayout(layout);
            CreateLane(leftLane, -LaneCenter, LoadMaterial("Assets/Materials/LeftLane.mat"));
            CreateLane(rightLane, LaneCenter, LoadMaterial("Assets/Materials/RightLane.mat"));
            CreateDivider(divider, LoadMaterial("Assets/Materials/Divider.mat"));

            var gameplayArena = ReplaceChild(environment.transform, "GameplayArena");
            var trim = LoadOrCreateMaterial("Assets/Materials/ArenaTrim.mat", new Color(0.11f, 0.14f, 0.18f));
            var walkway = LoadOrCreateMaterial("Assets/Materials/ArenaWalkway.mat", new Color(0.22f, 0.25f, 0.29f));
            var rail = LoadOrCreateMaterial("Assets/Materials/ArenaRail.mat", new Color(0.075f, 0.09f, 0.12f));

            CreateCube("LeftOuterCurb", gameplayArena, new Vector3(-6.08f, 0.22f, 19.5f), new Vector3(0.32f, 0.44f, 39f), trim);
            CreateCube("RightOuterCurb", gameplayArena, new Vector3(6.08f, 0.22f, 19.5f), new Vector3(0.32f, 0.44f, 39f), trim);
            CreateCube("LeftInnerCurb", gameplayArena, new Vector3(-0.68f, 0.2f, 19.5f), new Vector3(0.2f, 0.4f, 39f), trim);
            CreateCube("RightInnerCurb", gameplayArena, new Vector3(0.68f, 0.2f, 19.5f), new Vector3(0.2f, 0.4f, 39f), trim);

            var visualEnvironment = ReplaceRoot(scene, "VisualEnvironment");
            CreateCube("LeftSideWalkway", visualEnvironment, new Vector3(-8.15f, -0.12f, 19.5f), new Vector3(3.8f, 0.28f, 43f), walkway);
            CreateCube("RightSideWalkway", visualEnvironment, new Vector3(8.15f, -0.12f, 19.5f), new Vector3(3.8f, 0.28f, 43f), walkway);
            CreateCube("FrontApron", visualEnvironment, new Vector3(0f, -0.12f, -1.5f), new Vector3(20f, 0.28f, 3f), walkway);
            CreateCube("FarStructuralCap", visualEnvironment, new Vector3(0f, -0.1f, 41.1f), new Vector3(20f, 0.5f, 1.8f), trim);

            CreateRailRun("LeftRail", visualEnvironment, -9.85f, rail);
            CreateRailRun("RightRail", visualEnvironment, 9.85f, rail);
            CreateEndCapPosts("FarPosts", visualEnvironment, rail);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void SetLayout(LaneLayout layout)
        {
            var serialized = new SerializedObject(layout);
            serialized.FindProperty("leftLaneX").floatValue = -LaneCenter;
            serialized.FindProperty("rightLaneX").floatValue = LaneCenter;
            serialized.FindProperty("laneWidth").floatValue = LaneWidth;
            serialized.FindProperty("strafeMinX").floatValue = -5.75f;
            serialized.FindProperty("strafeMaxX").floatValue = 5.75f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateLane(Transform lane, float x, Material material)
        {
            lane.position = new Vector3(x, 0f, 19.5f);
            lane.localScale = new Vector3(LaneWidth, 0.35f, 39f);
            lane.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CreateDivider(Transform divider, Material material)
        {
            divider.position = new Vector3(0f, 0.75f, 21f);
            divider.localScale = new Vector3(0.4f, 1.5f, 36f);
            divider.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CreateRailRun(string name, Transform parent, float x, Material material)
        {
            var railRoot = new GameObject(name).transform;
            railRoot.SetParent(parent, false);
            for (var z = 1.5f; z <= 39f; z += 7.5f)
            {
                CreateCube("Post", railRoot, new Vector3(x, 1.0f, z), new Vector3(0.22f, 2f, 0.22f), material);
            }
            CreateCube("TopRail", railRoot, new Vector3(x, 1.55f, 20f), new Vector3(0.16f, 0.16f, 40f), material);
            CreateCube("MidRail", railRoot, new Vector3(x, 0.85f, 20f), new Vector3(0.12f, 0.12f, 40f), material);
        }

        static void CreateEndCapPosts(string name, Transform parent, Material material)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            foreach (var x in new[] { -9.85f, -6.1f, 6.1f, 9.85f })
            {
                CreateCube("Post", root, new Vector3(x, 1.25f, 40.8f), new Vector3(0.28f, 2.5f, 0.28f), material);
            }
        }

        static void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
        }

        static Transform ReplaceChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
            var created = new GameObject(name).transform;
            created.SetParent(parent, false);
            return created;
        }

        static Transform ReplaceRoot(Scene scene, string name)
        {
            var existing = FindRoot(scene, name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
            return new GameObject(name).transform;
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

        static Material LoadMaterial(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

        static Material LoadOrCreateMaterial(string path, Color color)
        {
            var material = LoadMaterial(path);
            if (material != null)
            {
                return material;
            }
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
