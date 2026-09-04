using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealRail.Editor
{
    /// <summary>Unity-serialized setup for the optional, default-disabled simulation controller.</summary>
    public static class PlayerBotSceneSetup
    {
        const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/RealRail/Add Player Bot Foundation To Open Scene")]
        public static void AddPlayerBotFoundation()
        {
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            if (motor == null)
            {
                Debug.LogError("RealRail Player Bot setup could not find a PlayerMotor in the open scene.");
                return;
            }

            var bot = motor.GetComponent<PlayerBot>();
            if (bot == null)
            {
                bot = Undo.AddComponent<PlayerBot>(motor.gameObject);
            }

            var session = Object.FindAnyObjectByType<GameSession>();
            var telemetry = session != null ? session.GetComponent<RunTelemetry>() : null;
            if (session != null && telemetry == null)
            {
                telemetry = Undo.AddComponent<RunTelemetry>(session.gameObject);
            }

            var upgradeSystem = Object.FindAnyObjectByType<UpgradeSystem>();
            var selection = Object.FindAnyObjectByType<UpgradeRewardSelection>();
            SetReference(bot, "motor", motor);
            SetReference(bot, "session", session);
            SetReference(bot, "rewardSelection", selection);
            SetReference(telemetry, "session", session);
            SetReference(telemetry, "upgradeSystem", upgradeSystem);

            EditorSceneManager.MarkSceneDirty(motor.gameObject.scene);
            Debug.Log("Added Player Bot foundation. PlayerBot remains disabled by default; enable it through its component for a simulation run.");
        }

        /// <summary>Batch-mode entry point that opens and saves the authored gameplay scene through Unity serialization.</summary>
        public static void ApplyToSampleScene()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            AddPlayerBotFoundation();
            EditorSceneManager.SaveScene(scene);
        }

        static void SetReference(Object owner, string propertyName, Object value)
        {
            if (owner == null) return;
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Tools/RealRail/Add Player Bot Foundation To Open Scene", true)]
        static bool ValidateAddFoundation() => !Application.isPlaying;
    }
}
