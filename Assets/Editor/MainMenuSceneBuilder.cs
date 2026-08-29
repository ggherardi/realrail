using RealRail;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RealRail.Editor
{
    public static class MainMenuSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenuScene.unity";
        const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("RealRail/Create Main Menu Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();

            var canvas = CreateCanvas();
            var controller = canvas.gameObject.AddComponent<MainMenuController>();
            var mainPanel = CreatePanel("MainPanel", canvas, new Color(0.035f, 0.07f, 0.11f, 1f));
            var settingsPanel = CreatePanel("SettingsPanel", canvas, new Color(0.035f, 0.07f, 0.11f, 1f));

            CreateMainPanel(mainPanel.transform, controller);
            CreateSettingsPanel(settingsPanel.transform, controller);
            settingsPanel.SetActive(false);
            SetSerializedReferences(controller, mainPanel, settingsPanel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
        }

        static void CreateCamera()
        {
            var camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            var component = camera.AddComponent<Camera>();
            component.clearFlags = CameraClearFlags.SolidColor;
            component.backgroundColor = new Color(0.02f, 0.04f, 0.07f);
            camera.AddComponent<AudioListener>();
        }

        static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<EventSystem>().sendNavigationEvents = true;
        }

        static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static GameObject CreatePanel(string name, Canvas canvas, Color backgroundColor)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = backgroundColor;
            return panel;
        }

        static void CreateMainPanel(Transform parent, MainMenuController controller)
        {
            CreateText("Title", parent, "REALRAIL", 90, FontStyle.Bold, new Vector2(0f, 250f), new Vector2(920f, 120f), new Color(0.93f, 0.96f, 0.98f));
            CreateText("Subtitle", parent, "DEFEND THE LINE", 22, FontStyle.Normal, new Vector2(0f, 166f), new Vector2(520f, 42f), new Color(0.38f, 0.72f, 0.88f));
            CreateButton("PlayButton", parent, "PLAY", new Vector2(0f, 55f), new Color(0.10f, 0.48f, 0.65f), controller.Play);
            CreateButton("SettingsButton", parent, "SETTINGS", new Vector2(0f, -55f), new Color(0.12f, 0.22f, 0.31f), controller.OpenSettings);
            CreateButton("QuitButton", parent, "QUIT", new Vector2(0f, -165f), new Color(0.12f, 0.22f, 0.31f), controller.Quit);
            CreateText("Footer", parent, "REALRAIL  •  EARLY BUILD", 15, FontStyle.Normal, new Vector2(0f, -430f), new Vector2(600f, 30f), new Color(0.42f, 0.52f, 0.61f));
        }

        static void CreateSettingsPanel(Transform parent, MainMenuController controller)
        {
            CreateText("SettingsTitle", parent, "SETTINGS", 56, FontStyle.Bold, new Vector2(0f, 145f), new Vector2(720f, 90f), new Color(0.93f, 0.96f, 0.98f));
            CreateText("PlaceholderText", parent, "Settings coming soon", 28, FontStyle.Normal, new Vector2(0f, 25f), new Vector2(720f, 60f), new Color(0.63f, 0.72f, 0.79f));
            CreateButton("BackButton", parent, "BACK", new Vector2(0f, -125f), new Color(0.12f, 0.22f, 0.31f), controller.CloseSettings);
        }

        static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, Vector2 position, Vector2 size, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        static void CreateButton(string name, Transform parent, string label, Vector2 position, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(360f, 76f);

            var image = buttonObject.GetComponent<Image>();
            image.color = color;
            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.20f);
            button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, onClick);

            CreateText("Label", buttonObject.transform, label, 26, FontStyle.Bold, Vector2.zero, new Vector2(330f, 64f), Color.white);
        }

        static void SetSerializedReferences(MainMenuController controller, GameObject mainPanel, GameObject settingsPanel)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("gameplaySceneName").stringValue = "SampleScene";
            serialized.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            serialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
        }
    }
}
