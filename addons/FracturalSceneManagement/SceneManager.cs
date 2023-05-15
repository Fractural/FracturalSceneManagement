using Fractural.Commons;
using Fractural.Utils;
using Godot;
using Godot.Collections;

namespace Fractural.SceneManagement
{
    // Manages transitions between scenes.
    [RegisteredType(nameof(SceneManager), "res://addons/FracturalSceneManagement/Assets/scenemanager.svg")]
    [Tool]
    public class SceneManager : Node
    {
        [Signal]
        public delegate void OnSceneLoaded(Node loadedScene);
        [Signal]
        public delegate void OnSceneReadied(Node readiedScene);
        [Signal]
        public delegate void OnNodeAdded(Node addedNode);
        [Signal]
        public delegate void OnNodeRemoved(Node removedNode);

        [Export]
        public bool IsSelfContained { get; set; }
        private bool _autoLoadInitialScene;
        [Export]
        public bool AutoLoadInititalScene
        {
            get => _autoLoadInitialScene;
            set
            {
                _autoLoadInitialScene = value;
                PropertyListChangedNotify();
            }
        }
        public float InitialSceneLoadDelay { get; set; }
        public PackedScene InitialSceneLoadTransition { get; set; }
        public PackedScene InitialScene { get; set; }

        [Export]
        public int TransitionCanvasLayer { get; set; } = 100;

        public Node CurrentScene
        {
            get
            {
                if (IsSelfContained)
                    return _currentScene;
                else
                    return GetTree().CurrentScene;
            }
            set
            {
                if (IsSelfContained)
                    _currentScene = value;
                else
                    GetTree().CurrentScene = value;
            }
        }
        private Node _currentScene;
        public Node Root
        {
            get
            {
                if (IsSelfContained)
                    return this;
                else
                    return GetTree().Root;
            }
        }

        private CanvasLayer _transitionCanvasLayer;

        public override void _Ready()
        {
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
            _transitionCanvasLayer = new CanvasLayer();
            _transitionCanvasLayer.Layer = TransitionCanvasLayer;
            AddChild(_transitionCanvasLayer);
            GetTree().Connect("node_added", this, nameof(ListenOnNodeAdded));
            GetTree().Connect("node_removed", this, nameof(ListenOnNodeRemoved));
            if (AutoLoadInititalScene)
                CallDeferred(nameof(GotoInitialScene));
        }

        public async void GotoInitialScene()
        {
            await ToSignal(GetTree().CreateTimer(InitialSceneLoadDelay), "timeout");
            if (InitialScene != null)
            {
                if (InitialSceneLoadTransition != null)
                    TransitionToScene(InitialScene, InitialSceneLoadTransition);
                else
                    GotoScene(InitialScene);
            }
        }

        public void GotoScene(PackedScene scene)
        {
            CurrentScene?.QueueFree();

            Node instance = scene.Instance();

            EmitSignal(nameof(OnSceneLoaded), instance);

            Root.AddChild(instance);
            CurrentScene = instance;

            EmitSignal(nameof(OnSceneReadied), instance);
        }

        public async void TransitionToScene(PackedScene scene, PackedScene transition)
        {
            SceneTransition transitionInstance = transition.Instance<SceneTransition>();
            _transitionCanvasLayer.AddChild(transitionInstance);

            transitionInstance.TransitionIn();

            await ToSignal(transitionInstance, nameof(SceneTransition.OnTransitionedIn));

            CurrentScene?.QueueFree();
            Node instance = scene.Instance();

            EmitSignal(nameof(OnSceneLoaded), instance);

            Root.AddChild(instance);
            CurrentScene = instance;

            transitionInstance.TransitionOut();

            await ToSignal(transitionInstance, nameof(SceneTransition.OnTransitionedOut));
            transitionInstance.QueueFree();

            EmitSignal(nameof(OnSceneReadied), instance);
        }

        public void GotoScene(string scene_path)
        {
            GotoScene(ResourceLoader.Load<PackedScene>(scene_path));
        }

        public override Array _GetPropertyList()
        {
            var builder = new PropertyListBuilder();
            builder.AddItem(
                name: nameof(InitialScene),
                type: Variant.Type.Object,
                hintString: nameof(PackedScene),
                usage: AutoLoadInititalScene ? PropertyUsageFlags.Default : PropertyUsageFlags.Noeditor
            );
            builder.AddItem(
                name: nameof(InitialSceneLoadTransition),
                type: Variant.Type.Object,
                hintString: nameof(PackedScene),
                usage: AutoLoadInititalScene ? PropertyUsageFlags.Default : PropertyUsageFlags.Noeditor
            );
            builder.AddItem(
                name: nameof(InitialSceneLoadDelay),
                type: Variant.Type.Real,
                usage: AutoLoadInititalScene ? PropertyUsageFlags.Default : PropertyUsageFlags.Noeditor
            );
            return builder.Build();
        }

        private void ListenOnNodeAdded(Node addedNode)
        {
            if (IsSelfContained && !addedNode.HasParent(this))
                return;

            EmitSignal(nameof(OnNodeAdded), addedNode);
        }

        private void ListenOnNodeRemoved(Node removedNode)
        {
            if (IsSelfContained && !removedNode.HasParent(this))
                return;

            EmitSignal(nameof(OnNodeAdded), removedNode);
        }
    }
}