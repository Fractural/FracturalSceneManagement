using Fractural.Commons;
using Fractural.Utils;
using Godot;
using Godot.Collections;

namespace Fractural.SceneManagement
{
    /// <summary>
    /// Manages transition between scenes and keeps track of node add and removes.
    /// </summary>
    [RegisteredType(nameof(SceneManager), "res://addons/FracturalSceneManagement/Assets/scenemanager.svg")]
    [Tool]
    public class SceneManager : Node
    {
        /// <summary>
        /// Emitted when a scene is loaded, but the transition hasn't finished yet.
        /// </summary>
        /// <param name="loadedScene"></param>
        [Signal]
        public delegate void SceneLoaded(Node loadedScene);
        /// <summary>
        /// Emitted when the scene is loaded, and the transition has also finished.
        /// </summary>
        /// <param name="readiedScene"></param>
        [Signal]
        public delegate void SceneReadied(Node readiedScene);
        /// <summary>
        /// Emitted when a node is add to the scene.
        /// If SceneManager is self-contained then only nodes that are descendants of the SceneManager itself can trigger this signal.
        /// </summary>
        /// <param name="addedNode"></param>
        [Signal]
        public delegate void NodeAdded(Node addedNode);
        /// <summary>
        /// Emitted when a node is removed from the scene.
        /// If SceneManager is self-contained then only nodes that are descendants of the SceneManager itself can trigger this signal.
        /// </summary>
        /// <param name="addedNode"></param>
        [Signal]
        public delegate void NodeRemoved(Node removedNode);

        /// <summary>
        /// If SceneManager is self-contained, it loads scenes as children of the SceneManager.
        /// If SceneManager is not self-contained, it manages the current tree's scene.
        /// </summary>
        [Export]
        public bool IsSelfContained { get; set; }
        private PackedScene _autoLoadScene;
        /// <summary>
        /// Optional initial scene to auto load.
        /// </summary>
        [Export]
        public PackedScene AutoLoadScene
        {
            get => _autoLoadScene;
            set
            {
                _autoLoadScene = value;
                PropertyListChangedNotify();
            }
        }
        /// <summary>
        /// Delay before loading initial scene.
        /// </summary>
        public float AutoLoadDelay { get; set; }
        /// <summary>
        /// Optional transition to use when loading the initial scene.
        /// </summary>
        public PackedScene AutoLoadTransition { get; set; }

        /// <summary>
        /// Default transition to use if <seealso cref="TransitionTo"/> is used without a transition argument.
        /// </summary>
        [Export]
        public PackedScene DefaultTransition { get; set; }
        /// <summary>
        /// The layer of the transition canvas. Normally set really high to make sure the transitions appear above all UI.
        /// </summary>
        [Export]
        public int TransitionCanvasLayer { get; set; } = 100;

        /// <summary>
        /// Current scene managed by the SceneManager
        /// </summary>
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
            if (AutoLoadScene != null)
                CallDeferred(nameof(RunAutoLoadScene));
        }

        public async void RunAutoLoadScene()
        {
            await ToSignal(GetTree(), "idle_frame");
            await ToSignal(GetTree().CreateTimer(AutoLoadDelay), "timeout");
            if (AutoLoadScene != null)
                TransitionToScene(AutoLoadScene, AutoLoadTransition);
        }

        /// <summary>
        /// Loads a scene immediately without any transitions.
        /// </summary>
        /// <param name="scene"></param>
        public void GotoScene(PackedScene scene)
        {
            CurrentScene?.QueueFree();

            Node instance = scene.Instance();

            EmitSignal(nameof(SceneLoaded), instance);

            Root.AddChild(instance);
            CurrentScene = instance;

            EmitSignal(nameof(SceneReadied), instance);
        }

        /// <summary>
        /// Loads a scene at a path immediately without any transitions
        /// </summary>
        /// <param name="scene_path"></param>
        public void GotoScene(string scene_path)
        {
            GotoScene(ResourceLoader.Load<PackedScene>(scene_path));
        }

        /// <summary>
        /// Loads a scene using a transition. If <paramref name="transition"/> is null
        /// the <seealso cref="DefaultTransition"/> is used.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="transition"></param>
        public async void TransitionToScene(PackedScene scene, PackedScene transition = null)
        {
            if (transition == null)
            {
                if (DefaultTransition == null)
                {
                    GD.PushError($"{nameof(SceneManager)}: Could not transition to scene because transition was null -- either pass in a transition or set a DefaultTransition for the SceneManager.");
                    return;
                }
                transition = DefaultTransition;
            }
            SceneTransition transitionInstance = transition.Instance<SceneTransition>();
            _transitionCanvasLayer.AddChild(transitionInstance);

            transitionInstance.TransitionIn();

            await ToSignal(transitionInstance, nameof(SceneTransition.OnTransitionedIn));

            CurrentScene?.QueueFree();
            Node instance = scene.Instance();

            EmitSignal(nameof(SceneLoaded), instance);

            Root.AddChild(instance);
            CurrentScene = instance;

            transitionInstance.TransitionOut();

            await ToSignal(transitionInstance, nameof(SceneTransition.OnTransitionedOut));
            transitionInstance.QueueFree();

            EmitSignal(nameof(SceneReadied), instance);
        }

        public override Array _GetPropertyList()
        {
            var builder = new PropertyListBuilder();
            builder.AddItem(
                name: nameof(AutoLoadTransition),
                type: Variant.Type.Object,
                hintString: nameof(PackedScene),
                usage: AutoLoadScene != null ? PropertyUsageFlags.Default : PropertyUsageFlags.Noeditor
            );
            builder.AddItem(
                name: nameof(AutoLoadDelay),
                type: Variant.Type.Real,
                usage: AutoLoadScene != null ? PropertyUsageFlags.Default : PropertyUsageFlags.Noeditor
            );
            return builder.Build();
        }

        private void ListenOnNodeAdded(Node addedNode)
        {
            if (IsSelfContained && !addedNode.HasParent(this))
                return;

            EmitSignal(nameof(NodeAdded), addedNode);
        }

        private void ListenOnNodeRemoved(Node removedNode)
        {
            if (IsSelfContained && !removedNode.HasParent(this))
                return;

            EmitSignal(nameof(NodeRemoved), removedNode);
        }
    }
}