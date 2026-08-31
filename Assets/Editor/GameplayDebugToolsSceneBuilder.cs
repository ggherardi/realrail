using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealRail.Editor
{
    public static class GameplayDebugToolsSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("RealRail/Configure Gameplay Debug Tools")]
        public static void Configure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var systems = FindRoot(scene, "Systems");
            var canvas = FindRoot(scene, "Canvas");
            var session = systems.GetComponentInChildren<GameSession>(true);
            var upgrades = systems.GetComponentInChildren<UpgradeSystem>(true);
            var controller = systems.GetComponent<GameplayDebugController>() ?? systems.AddComponent<GameplayDebugController>();
            var hud = FindOrCreateDebugHud(canvas.transform);

            SetReference(controller, "session", session);
            SetReference(controller, "upgradeSystem", upgrades);
            SetReference(controller, "debugHud", hud);
            SetReference(hud, "session", session);
            SetReference(hud, "upgradeSystem", upgrades);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static GameplayDebugHud FindOrCreateDebugHud(Transform canvas)
        {
            var existing = canvas.Find("GameplayDebugHud");
            if (existing != null)
            {
                return existing.GetComponent<GameplayDebugHud>();
            }

            var panel = new GameObject("GameplayDebugHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(GameplayDebugHud));
            panel.transform.SetParent(canvas, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(360f, 430f);

            var text = panel.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.75f, 0.95f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetReference(panel.GetComponent<GameplayDebugHud>(), "displayText", text);
            return panel.GetComponent<GameplayDebugHud>();
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
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
