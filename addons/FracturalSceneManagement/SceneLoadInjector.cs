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

        private SceneManager sceneManager;
        private DIContainer diContainer;

        public override void _Ready()
        {
            sceneManager = GetNode<SceneManager>(_sceneManagerPath);
            diContainer = GetNode<DIContainer>(_diContainerPath);

            sceneManager.Connect(nameof(SceneManager.SceneLoaded), this, nameof(OnSceneLoaded));
        }

        private void OnSceneLoaded(Node scene)
        {
            RunInjection(scene, 1);
        }

        private void RunInjection(Node node, int depth)
        {
            diContainer.ResolveNode(node);
            if (depth < InjectionDepth)
            {
                depth++;
                foreach (Node child in node.GetChildren())
                    RunInjection(child, depth);
            }
        }
    }
}