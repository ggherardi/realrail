using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Editor
{
    /// <summary>Serializes the opt-in batch simulation tooling into the gameplay scene.</summary>
    public static class SimulationRunnerSceneSetup
    {
        const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/RealRail/Configure Batch Simulation")]
        public static void Configure()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            var systems = FindRoot(scene, "Systems");
            if (systems == null)
            {
                Debug.LogError("RealRail batch simulation setup could not find the Systems root.");
                return;
            }

            var runner = systems.GetComponent<SimulationRunner>() ?? Undo.AddComponent<SimulationRunner>(systems);
            var session = Object.FindAnyObjectByType<GameSession>();
            var director = Object.FindAnyObjectByType<WaveDirector>();
            var telemetry = Object.FindAnyObjectByType<RunTelemetry>();
            var upgrades = Object.FindAnyObjectByType<UpgradeSystem>();
            var selection = Object.FindAnyObjectByType<UpgradeRewardSelection>();
            var bot = Object.FindAnyObjectByType<PlayerBot>();

            SetReference(runner, "session", session);
            SetReference(runner, "waveDirector", director);
            SetReference(runner, "telemetry", telemetry);
            SetReference(runner, "upgradeSystem", upgrades);
            SetReference(runner, "rewardSelection", selection);
            SetReference(runner, "playerBot", bot);
            SetValue(runner, "simulationEnabled", false);
            SetValue(runner, "simulationSpeed", 4f);
            SetValue(runner, "maximumRuns", 10);
            SetReference(bot, "upgradeSystem", upgrades);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Configured opt-in batch simulation. Set run count, speed, and PlayerBot profile in the Inspector, then enable Simulation Runner.");
        }

        /// <summary>Batch-mode entry point for creating validated Unity scene serialization.</summary>
        public static void ApplyToSampleScene() => Configure();

        static void SetReference(Object owner, string propertyName, Object value)
        {
            if (owner == null) return;
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValue(Object owner, string propertyName, bool value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValue(Object owner, string propertyName, float value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValue(Object owner, string propertyName, int value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }
    }
}
