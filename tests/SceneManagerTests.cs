using Godot;
using WAT;
using Fractural.SceneManagement;
using Fractural.Utils;
using Fractural.IO;
using System.Threading.Tasks;

namespace Tests
{
    [Start(nameof(Start))]
    public class SceneManagerTests : WAT.Test
    {
        public const float FadeBlackTransitionIn = 0.5f;
        public const float FadeBlackTransitionOut = 0.5f;
        private PackedScene _initialScene;
        private PackedScene _targetScene;
        private PackedScene _fadeBlackTransition;

        public void Start()
        {
            _initialScene = IO.LoadResourceOrNull<PackedScene>("./InitialScene.tscn");
            _targetScene = IO.LoadResourceOrNull<PackedScene>("./TargetScene.tscn");
            _fadeBlackTransition = IO.LoadResourceOrNull<PackedScene>("res://addons/FracturalSceneManagement/Transitions/FadeBlackTransition.tscn");
        }

        [Test]
        public void TestInitialSceneLoading()
        {
            Describe("When readying and has initial scene with instant transition time");

            var sceneManager = new SceneManager();
            sceneManager.IsSelfContained = true;
            sceneManager.AutoLoadScene = _initialScene;

            Assert.IsTrue(sceneManager.GetChildren().Count == 0, "Then the scene manager has no children at first.");

            AddChild(sceneManager);

            Assert.IsTrue(sceneManager.GetChildren().Count > 0, "Then the initial scene was created.");

            sceneManager.QueueFree();
        }

        [Test]
        public async Task TestNodeAddRemoveSignals()
        {
            Describe("When adding/removing a node from sceneManager");

            var sceneManager = new SceneManager();
            sceneManager.IsSelfContained = true;
            AddChild(sceneManager);
            var testNode = new Node();

            Watch(sceneManager, nameof(SceneManager.NodeAdded));
            sceneManager.AddChild(testNode);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.NodeAdded), GDUtils.GDParams(testNode), "Then NodeAdded signal should be emitted with correct node.");
            UnWatch(sceneManager, nameof(SceneManager.NodeAdded));

            Watch(sceneManager, nameof(SceneManager.NodeRemoved));
            sceneManager.RemoveChild(testNode);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.NodeRemoved), GDUtils.GDParams(testNode), "Then NodeRemoved signal should be emitted with correct node.");
            UnWatch(sceneManager, nameof(SceneManager.NodeRemoved));

            sceneManager.AddChild(testNode);
            Watch(sceneManager, nameof(SceneManager.NodeRemoved));
            await UntilSignal(GetTree(), "idle_frame", 1f);
            testNode.QueueFree();
            await UntilSignal(GetTree(), "idle_frame", 1f);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.NodeRemoved), GDUtils.GDParams(testNode), "Then NodeRemoved signal should be emitted with correct node when node is freed.");
            UnWatch(sceneManager, nameof(SceneManager.NodeRemoved));

            sceneManager.QueueFree();
        }

        [Test]
        public async Task TestNodeAddRemoveSignalsOutsideSelfContained()
        {
            Describe("When adding/removing a node outside of sceneManager and sceneManager is self-contained");

            var sceneManager = new SceneManager();
            sceneManager.IsSelfContained = true;
            AddChild(sceneManager);
            var testNode = new Node();

            Watch(sceneManager, nameof(SceneManager.NodeAdded));
            AddChild(testNode);
            Assert.SignalWasNotEmitted(sceneManager, nameof(SceneManager.NodeAdded), "Then NodeAdded signal should not be emitted.");
            UnWatch(sceneManager, nameof(SceneManager.NodeAdded));

            Watch(sceneManager, nameof(SceneManager.NodeRemoved));
            RemoveChild(testNode);
            Assert.SignalWasNotEmitted(sceneManager, nameof(SceneManager.NodeRemoved), "Then NodeRemoved signal should not be emitted.");
            UnWatch(sceneManager, nameof(SceneManager.NodeRemoved));

            AddChild(testNode);
            Watch(sceneManager, nameof(SceneManager.NodeRemoved));
            await UntilSignal(GetTree(), "idle_frame", 1f);
            testNode.QueueFree();
            await UntilSignal(GetTree(), "idle_frame", 1f);
            Assert.SignalWasNotEmitted(sceneManager, nameof(SceneManager.NodeRemoved), "Then NodeRemoved signal should node bet emitted when node is freed.");
            UnWatch(sceneManager, nameof(SceneManager.NodeRemoved));

            sceneManager.QueueFree();
        }

        [Test]
        public async Task TestNodeAddRemoveSignalsOutside()
        {
            Describe("When adding/removing a node outside of sceneManager");

            var sceneManager = new SceneManager();
            AddChild(sceneManager);
            var testNode = new Node();

            Watch(sceneManager, nameof(SceneManager.NodeAdded));
            AddChild(testNode);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.NodeAdded), GDUtils.GDParams(testNode), "Then NodeAdded signal should be emitted with correct node.");
            UnWatch(sceneManager, nameof(SceneManager.NodeAdded));

            Watch(sceneManager, nameof(SceneManager.NodeRemoved));
            RemoveChild(testNode);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.NodeRemoved), GDUtils.GDParams(testNode), "Then NodeRemoved signal should be emitted with correct node.");
            UnWatch(sceneManager, nameof(SceneManager.NodeRemoved));

            AddChild(testNode);
            Watch(sceneManager, nameof(SceneManager.NodeRemoved));
            await UntilSignal(GetTree(), "idle_frame", 1f);
            testNode.QueueFree();
            await UntilSignal(GetTree(), "idle_frame", 1f);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.NodeRemoved), GDUtils.GDParams(testNode), "Then NodeRemoved signal should be emitted with correct node when node is freed.");
            UnWatch(sceneManager, nameof(SceneManager.NodeRemoved));

            sceneManager.QueueFree();
        }

        [Test]
        public async Task TestTransitionToScene()
        {
            var sceneManager = new SceneManager();
            sceneManager.IsSelfContained = true;
            AddChild(sceneManager);

            Watch(sceneManager, nameof(SceneManager.SceneLoaded));
            Watch(sceneManager, nameof(SceneManager.SceneReadied));
            sceneManager.TransitionToScene(_targetScene, _fadeBlackTransition);

            // Wait for fade black to first run
            await UntilTimeout(FadeBlackTransitionIn + 0.1f);
            var loadedTarget = FindNode("TargetScene", owned: false);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.SceneLoaded), GDUtils.GDParams(loadedTarget), "Then SceneLoaded is emitted");
            Assert.SignalWasNotEmitted(sceneManager, nameof(SceneManager.SceneReadied), "Then SceneReadied initially isn't emitted");

            await UntilTimeout(FadeBlackTransitionOut);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.SceneReadied), GDUtils.GDParams(loadedTarget), "Then SceneReadied finally is emitted after transition finishes");

            UnWatch(sceneManager, nameof(SceneManager.SceneLoaded));
            UnWatch(sceneManager, nameof(SceneManager.SceneReadied));
            sceneManager.QueueFree();
        }

        [Test]
        public void TestGoToScene()
        {
            var sceneManager = new SceneManager();
            sceneManager.IsSelfContained = true;
            AddChild(sceneManager);

            Watch(sceneManager, nameof(SceneManager.SceneLoaded));
            Watch(sceneManager, nameof(SceneManager.SceneReadied));
            sceneManager.GotoScene(_targetScene);

            var loadedTarget = FindNode("TargetScene", owned: false);
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.SceneLoaded), GDUtils.GDParams(loadedTarget), "Then SceneLoaded is is emitted");
            Assert.SignalWasEmittedWithArguments(sceneManager, nameof(SceneManager.SceneReadied), GDUtils.GDParams(loadedTarget), "Then SceneReadied is emitted");

            UnWatch(sceneManager, nameof(SceneManager.SceneLoaded));
            UnWatch(sceneManager, nameof(SceneManager.SceneReadied));
            sceneManager.QueueFree();
        }
    }
}