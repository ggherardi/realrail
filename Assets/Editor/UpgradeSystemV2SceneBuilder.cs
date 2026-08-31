using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealRail.Editor
{
    public static class UpgradeSystemV2SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("RealRail/Configure Upgrade System V2")]
        public static void Configure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var systems = FindRoot(scene, "Systems");
            var player = FindRoot(scene, "Player");
            var director = systems.GetComponentInChildren<WaveDirector>(true);
            var upgradeSystem = systems.GetComponentInChildren<UpgradeSystem>(true) ?? systems.AddComponent<UpgradeSystem>();
            var autoFire = player.GetComponent<AutoFire>();
            var hud = Object.FindAnyObjectByType<HudView>(FindObjectsInactive.Include);
            var feedback = FindOrCreateFeedbackText(hud.transform);

            var waves = new SerializedObject(director).FindProperty("waves");
            SetTriggers(waves.GetArrayElementAtIndex(0), 8);
            SetTriggers(waves.GetArrayElementAtIndex(1), 14, 28);
            SetTriggers(waves.GetArrayElementAtIndex(2), 21, 46);

            SetReference(director, "upgradeSystem", upgradeSystem);
            SetReference(autoFire, "upgradeSystem", upgradeSystem);
            SetReference(hud, "upgradeSystem", upgradeSystem);
            SetReference(hud, "upgradeText", feedback);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void SetTriggers(SerializedProperty wave, params int[] values)
        {
            var triggers = wave.FindPropertyRelative("UpgradeTriggerKillCounts");
            triggers.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                triggers.GetArrayElementAtIndex(index).intValue = values[index];
            }
            wave.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Text FindOrCreateFeedbackText(Transform canvas)
        {
            var existing = canvas.Find("UpgradeFeedback");
            if (existing != null)
            {
                return existing.GetComponent<Text>();
            }

            var feedback = new GameObject("UpgradeFeedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            feedback.transform.SetParent(canvas, false);
            var rect = feedback.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(620f, 52f);
            var text = feedback.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.98f, 0.86f, 0.35f);
            text.alignment = TextAnchor.MiddleCenter;
            return text;
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
