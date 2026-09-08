using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Editor
{
    /// <summary>Uses Unity serialization to wire the M2 prototype into its authored scene and enemy base prefab.</summary>
    public static class CryoStormPrototypeSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";

        [MenuItem("RealRail/Configure Cryo Storm Prototype")]
        public static void Configure()
        {
            ConfigureEnemyPrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var systems = FindRoot(scene, "Systems");
            var prototype = systems.GetComponent<CryoStormPrototype>() ?? systems.AddComponent<CryoStormPrototype>();
            var debug = systems.GetComponent<GameplayDebugController>();
            var hud = FindRoot(scene, "Canvas").transform.Find("GameplayDebugHud").GetComponent<GameplayDebugHud>();

            SetReference(prototype, "session", systems.GetComponentInChildren<GameSession>(true));
            SetReference(prototype, "enemySpawner", systems.GetComponentInChildren<EnemySpawner>(true));
            SetLayerMask(prototype, "enemyLayers", 1 << LayerMask.NameToLayer(GameplayLayers.Enemy));
            SetReference(debug, "cryoStormPrototype", prototype);
            SetReference(hud, "cryoStormPrototype", prototype);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void ConfigureEnemyPrefab()
        {
            var enemy = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            if (enemy.GetComponent<FrostStatus>() == null) enemy.AddComponent<FrostStatus>();
            PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(enemy);
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLayerMask(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
