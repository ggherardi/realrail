using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealRail.Tests
{
    public sealed class MainMenuSceneTests
    {
        const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
        const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        Scene _scene;

        [OneTimeSetUp]
        public void OpenMainMenuScene()
        {
            _scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void Scene_HasAuthoredMenuHierarchyAndWiredController()
        {
            var canvas = FindRoot("Canvas");
            Assert.NotNull(FindRoot("Main Camera"));
            Assert.NotNull(FindRoot("EventSystem").GetComponent<EventSystem>());
            Assert.NotNull(canvas.GetComponent<Canvas>());
            Assert.NotNull(canvas.GetComponent<GraphicRaycaster>());

            var controller = canvas.GetComponent<MainMenuController>();
            Assert.NotNull(controller);
            var serialized = new SerializedObject(controller);
            Assert.AreEqual("SampleScene", serialized.FindProperty("gameplaySceneName").stringValue);

            var mainPanel = serialized.FindProperty("mainPanel").objectReferenceValue as GameObject;
            var settingsPanel = serialized.FindProperty("settingsPanel").objectReferenceValue as GameObject;
            Assert.NotNull(mainPanel);
            Assert.NotNull(settingsPanel);
            Assert.AreEqual("MainPanel", mainPanel.name);
            Assert.AreEqual("SettingsPanel", settingsPanel.name);
            Assert.IsTrue(mainPanel.activeSelf);
            Assert.IsFalse(settingsPanel.activeSelf);

            AssertButton(mainPanel.transform, "PlayButton", controller, "Play");
            AssertButton(mainPanel.transform, "SettingsButton", controller, "OpenSettings");
            AssertButton(mainPanel.transform, "QuitButton", controller, "Quit");
            AssertButton(settingsPanel.transform, "BackButton", controller, "CloseSettings");
            Assert.AreEqual("REALRAIL", mainPanel.transform.Find("Title").GetComponent<Text>().text);
            Assert.AreEqual("Settings coming soon", settingsPanel.transform.Find("PlaceholderText").GetComponent<Text>().text);

            controller.OpenSettings();
            Assert.IsFalse(mainPanel.activeSelf);
            Assert.IsTrue(settingsPanel.activeSelf);
            controller.CloseSettings();
            Assert.IsTrue(mainPanel.activeSelf);
            Assert.IsFalse(settingsPanel.activeSelf);
        }

        [Test]
        public void BuildSettings_StartWithMenuAndIncludeGameplay()
        {
            Assert.AreEqual(MainMenuScenePath, EditorBuildSettings.scenes[0].path);
            Assert.IsTrue(EditorBuildSettings.scenes[0].enabled);
            Assert.IsTrue(System.Array.Exists(EditorBuildSettings.scenes, scene => scene.enabled && scene.path == GameplayScenePath));
        }

        GameObject FindRoot(string name)
        {
            foreach (var root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        static void AssertButton(Transform parent, string name, MainMenuController target, string methodName)
        {
            var button = parent.Find(name).GetComponent<Button>();
            Assert.NotNull(button);
            Assert.AreEqual(1, button.onClick.GetPersistentEventCount());
            Assert.AreSame(target, button.onClick.GetPersistentTarget(0));
            Assert.AreEqual(methodName, button.onClick.GetPersistentMethodName(0));
        }
    }
}
