using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Editor
{
    public static class RailgunPrototypeSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("RealRail/Configure Railgun Prototype")]
        public static void Configure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var systems = FindRoot(scene, "Systems");
            var player = FindRoot(scene, "Player");
            var prototype = systems.GetComponent<RailgunPrototype>() ?? systems.AddComponent<RailgunPrototype>();
            var debug = systems.GetComponent<GameplayDebugController>();
            var hud = FindRoot(scene, "Canvas").transform.Find("GameplayDebugHud").GetComponent<GameplayDebugHud>();
            var autoFire = player.GetComponent<AutoFire>();

            SetReference(autoFire, "railgunPrototype", prototype);
            SetReference(debug, "railgunPrototype", prototype);
            SetReference(debug, "autoFire", autoFire);
            SetReference(debug, "enemySpawner", systems.GetComponentInChildren<EnemySpawner>(true));
            SetReference(hud, "railgunPrototype", prototype);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
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
