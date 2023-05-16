using Fractural.DependencyInjection;
using Godot;

namespace Tests
{
    [Inject]
    public class SceneLoadInjectorTestNode : Node
    {
        [Inject]
        public CustomTypeA PropertyCustomTypeA { get; set; }
        [Inject]
        public CustomTypeB PropertyCustomTypeB { get; set; }
        [Export]
        private NodePath _customADependecyPath;
        public Dependency CustomADependency => GetNode<Dependency>(_customADependecyPath);
    }
}