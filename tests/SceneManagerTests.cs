using Godot;
using WAT;
using Fractural.SceneManagement;
using Fractural.Utils;
using Fractural.IO;

namespace Tests
{
    [Start(nameof(Initialize))]
    public class SceneManagerTests : WAT.Test
    {
        private SceneManager sceneManager;
        private PackedScene initialScene;
        private PackedScene targetScene;

        public void Initialize()
        {
            initialScene = IO.LoadResource<PackedScene>("./InitialScene.tscn").Data;
            targetScene = IO.LoadResource<PackedScene>("./TargetScene.tscn").Data;
        }

        [Test]
        public void TestInitialSceneLoading()
        {
            Describe("When readying");

            sceneManager = new SceneManager();
            sceneManager.IsSelfContained = true;
            sceneManager.AutoLoadInititalScene = true;
            sceneManager.InitialScene = initialScene;

            Assert.IsTrue(sceneManager.GetChildren().Count == 0, "Then the scene manager has no children at first.");

            AddChild(sceneManager);

            Assert.IsTrue(sceneManager.GetChildren().Count > 0, "Then the initial scene was created.");
        }
    }
}