using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealRail.Editor
{
    public static class UpgradeSystemV3SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("RealRail/Configure Upgrade System V3")]
        public static void Configure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FindOrCreateEventSystem();
            var systems = FindRoot(scene, "Systems");
            var canvas = FindRoot(scene, "Canvas");
            var upgrades = systems.GetComponentInChildren<UpgradeSystem>(true);
            var selection = systems.GetComponent<UpgradeRewardSelection>() ?? systems.AddComponent<UpgradeRewardSelection>();
            var view = FindOrCreateSelectionView(canvas.transform);

            SetReference(selection, "upgradeSystem", upgrades);
            SetReference(selection, "selectionView", view);
            SetReference(systems.GetComponentInChildren<WaveDirector>(true), "upgradeRewardSelection", selection);
            SetReference(systems.GetComponent<GameplayDebugController>(), "upgradeRewardSelection", selection);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void FindOrCreateEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                var owner = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                owner.GetComponent<EventSystem>().sendNavigationEvents = true;
                return;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        static UpgradeSelectionView FindOrCreateSelectionView(Transform canvas)
        {
            var existing = canvas.Find("UpgradeSelectionOverlay");
            if (existing != null) return existing.GetComponent<UpgradeSelectionView>();

            var overlay = CreateUiObject("UpgradeSelectionOverlay", canvas);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            var backdrop = overlay.AddComponent<Image>();
            backdrop.color = new Color(0.03f, 0.06f, 0.12f, 0.86f);
            var view = overlay.AddComponent<UpgradeSelectionView>();

            var panel = CreateUiObject("Panel", overlay.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1040f, 430f);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.16f, 0.26f, 0.98f);
            var title = CreateText("Title", panel.transform, "CHOOSE AN UPGRADE", 38, new Color(0.98f, 0.86f, 0.35f));
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -56f);
            titleRect.sizeDelta = new Vector2(0f, 62f);

            var buttons = new Button[3];
            var texts = new Text[3];
            for (var index = 0; index < 3; index++)
            {
                var choice = CreateUiObject($"Choice{index + 1}", panel.transform);
                var rect = choice.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(285f, 210f);
                rect.anchoredPosition = new Vector2((index - 1) * 330f, -40f);
                var image = choice.AddComponent<Image>();
                image.color = new Color(0.16f, 0.34f, 0.51f, 1f);
                buttons[index] = choice.AddComponent<Button>();
                buttons[index].targetGraphic = image;
                texts[index] = CreateText("Label", choice.transform, string.Empty, 30, Color.white);
                var textRect = texts[index].GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            }

            SetReference(view, "panel", overlay);
            SetArray(view, "choiceButtons", buttons);
            SetArray(view, "choiceTexts", texts);
            return view;
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            result.transform.SetParent(parent, false);
            return result;
        }

        static Text CreateText(string name, Transform parent, string content, int fontSize, Color color)
        {
            var result = CreateUiObject(name, parent).AddComponent<Text>();
            result.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            result.text = content;
            result.fontSize = fontSize;
            result.fontStyle = FontStyle.Bold;
            result.color = color;
            result.alignment = TextAnchor.MiddleCenter;
            return result;
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string propertyName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
