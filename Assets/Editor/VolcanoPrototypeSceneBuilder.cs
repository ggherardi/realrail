using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Editor
{
    /// <summary>Uses Unity serialization to wire the M3 prototype into the playable SampleScene and authored enemy variants.</summary>
    public static class VolcanoPrototypeSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        const string HeavyPrefabPath = "Assets/Prefabs/Enemy_Heavy.prefab";

        [MenuItem("RealRail/Configure Volcano Prototype")]
        public static void Configure()
        {
            ConfigureEnemy(EnemyPrefabPath, 1);
            ConfigureEnemy(HeavyPrefabPath, 2);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var systems = FindRoot(scene, "Systems");
            var prototype = systems.GetComponent<VolcanoPrototype>() ?? systems.AddComponent<VolcanoPrototype>();
            var debug = systems.GetComponent<GameplayDebugController>();
            var spawner = systems.GetComponentInChildren<EnemySpawner>(true);
            var hud = FindRoot(scene, "Canvas").transform.Find("GameplayDebugHud").GetComponent<GameplayDebugHud>();
            SetReference(prototype, "session", systems.GetComponentInChildren<GameSession>(true));
            SetReference(prototype, "lanes", systems.GetComponentInChildren<LaneLayout>(true));
            SetReference(spawner, "volcanoPrototype", prototype);
            SetReference(debug, "volcanoPrototype", prototype);
            SetReference(hud, "volcanoPrototype", prototype);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void ConfigureEnemy(string path, int damage)
        {
            var enemy = PrefabUtility.LoadPrefabContents(path);
            var attack = enemy.GetComponent<VolcanoEnemyAttack>() ?? enemy.AddComponent<VolcanoEnemyAttack>();
            var serialized = new SerializedObject(attack);
            serialized.FindProperty("damagePerHit").intValue = damage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(enemy, path);
            PrefabUtility.UnloadPrefabContents(enemy);
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
