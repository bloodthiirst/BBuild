namespace Bloodthirst.BBuild;

public sealed class DependencyNode
{
    public DependencyNode? Parent { get; set; } 
    public BuildSettings BuildSettings { get; set; }
    public List<DependencyNode> Dependencies { get; set; } = new List<DependencyNode>();
}