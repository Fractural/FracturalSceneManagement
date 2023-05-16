using Fractural.DependencyInjection;
using Godot;

namespace Fractural.SceneManagement
{
    /// <summary>
    /// Injects dependencies into scenes after it gets loaded.
    /// </summary>
    public class SceneLoadInjector : Node
    {
        [Export]
        private NodePath _sceneManagerPath;
        [Export]
        private NodePath _diContainerPath;
        /// <summary>
        /// How many nodes deep should we try to inject dependencies into?
        /// Normal injection depth of 2 means the root node and it's children will get their dependencies automatically injected.
        /// </summary>
        [Export]
        public int InjectionDepth { get; set; } = 2;
        [Export]
        public bool InjectInitialScene { get; set; } = false;

        public SceneManager SceneManager { get; set; }
        public DIContainer DIContainer { get; set; }

        public override void _Ready()
        {
            if (SceneManager == null)
                SceneManager = GetNode<SceneManager>(_sceneManagerPath);
            if (DIContainer == null)
                DIContainer = GetNode<DIContainer>(_diContainerPath);

            SceneManager.Connect(nameof(SceneManager.SceneLoaded), this, nameof(OnSceneLoaded));
        }

        private void OnSceneLoaded(Node scene)
        {
            RunInjection(scene, 1);
        }

        private void RunInjection(Node node, int depth)
        {
            DIContainer.ResolveNode(node);
            if (depth < InjectionDepth)
            {
                depth++;
                foreach (Node child in node.GetChildren())
                    RunInjection(child, depth);
            }
        }
    }
}