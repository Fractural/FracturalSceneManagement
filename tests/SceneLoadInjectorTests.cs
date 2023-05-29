using Fractural.DependencyInjection;
using Fractural.IO;
using Fractural.SceneManagement;
using Godot;
using System.Threading.Tasks;
using WAT;

namespace Tests
{
    [Start(nameof(Start))]
    public class SceneLoadInjectorTests : WAT.Test
    {
        private PackedScene _sceneLoadInjectorTestScene;
        private PackedScene _fadeBlackTransition;

        public void Start()
        {
            _sceneLoadInjectorTestScene = IO.LoadResourceOrNull<PackedScene>("./SceneLoadInjectorTests.tscn");
            _fadeBlackTransition = IO.LoadResourceOrNull<PackedScene>("res://addons/FracturalSceneManagement/Transitions/FadeBlackTransition.tscn");
        }

        [Test]
        public async Task TestInjectionIntoLoadedScene()
        {
            Describe("When load new scene");

            var sceneManager = new SceneManager();
            sceneManager.IsSelfContained = true;

            var diContainer = new DIContainer();
            diContainer.AddChild(sceneManager);
            diContainer.Bind<SceneManager>().ToSingle(sceneManager);
            AddChild(diContainer);

            var sceneLoadInjector = new SceneLoadInjector();
            sceneLoadInjector.DIContainer = diContainer;
            sceneLoadInjector.SceneManager = sceneManager;
            diContainer.AddChild(sceneLoadInjector);

            var customTypeA = new CustomTypeA();
            AddChild(customTypeA);
            diContainer.Bind<CustomTypeA>().ToSingle(customTypeA);

            var customTypeB = new CustomTypeB();
            diContainer.Bind<CustomTypeB>().ToSingle(customTypeB);

            sceneManager.TransitionToScene(_sceneLoadInjectorTestScene, _fadeBlackTransition);

            await UntilTimeout(SceneManagerTests.FadeBlackTransitionIn + 0.1f);
            var prefabGrandchild = sceneManager.Root.GetNode<SceneLoadInjectorTestNode>("SceneLoadInjectorTests/Node/Child");
            var prefabChild = sceneManager.Root.GetNode<SceneLoadInjectorTestNode>("SceneLoadInjectorTests/Child");

            AssertSceneLoadInjectorTestNodePopulated(prefabGrandchild, customTypeA, customTypeB);
            AssertSceneLoadInjectorTestNodePopulated(prefabChild, customTypeA, customTypeB);

            diContainer.QueueFree();
        }

        private void AssertSceneLoadInjectorTestNodePopulated(SceneLoadInjectorTestNode testNode, CustomTypeA customTypeA, CustomTypeB customTypeB)
        {
            Assert.IsEqual(testNode.PropertyCustomTypeA, customTypeA, $"{testNode.Name}.{nameof(SceneLoadInjectorTestNode.PropertyCustomTypeA)} is injected");
            Assert.IsEqual(testNode.PropertyCustomTypeB, customTypeB, $"{testNode.Name}.{nameof(SceneLoadInjectorTestNode.PropertyCustomTypeB)} is injected");
            Assert.IsEqual(testNode.CustomADependency.DependencyValue, customTypeA, $"{testNode.Name}.{nameof(SceneLoadInjectorTestNode.CustomADependency)} is injected");
        }
    }
}