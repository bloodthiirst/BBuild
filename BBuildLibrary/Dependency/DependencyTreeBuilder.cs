using System.Diagnostics;

namespace Bloodthirst.BBuild;

public sealed class DependencyTreeBuilder
{
    public static DependencyNode? BuildDependencies(string absoluteRootProjectPath)
    {
        if (!BuildUtils.GetFromPath(absoluteRootProjectPath, out BuildSettings? settings, out BuildDependencies? dependencies))
        {
            Console.Error.WriteLine($"Couldn't get project info at : {absoluteRootProjectPath}");
            return null;
        }

        Debug.Assert(settings != null);
        Debug.Assert(dependencies != null);

        DependencyNode node = new DependencyNode()
        {
            BuildSettings = settings,
            BuildDependencies = dependencies,
            Dependencies = new List<DependencyNode>()
        };

        foreach (DependencySettings dep in settings.DependencyPaths)
        {
            string dependencyFullPath = dep.Path;

            if (!Path.IsPathRooted(dependencyFullPath))
            {
                dependencyFullPath = Path.GetFullPath(dep.Path, absoluteRootProjectPath).Replace("\\" , "/");
            }

            DependencyNode? subNode = BuildDependencies(dependencyFullPath);

            if (subNode != null)
            {
                node.Dependencies.Add(subNode);
            }
        }

        return node;
    }

    public static DependencyNode[][] FlattenDependencyNodes(DependencyNode root)
    {
        List<List<DependencyNode>> flatList = new List<List<DependencyNode>>();

        FlattenRecursive(root, flatList, 0);

        DependencyNode[][] result = new DependencyNode[flatList.Count][];

        // we reverse the order so that , dependencies come before their dependents
        for (int i = 0; i < flatList.Count; i++)
        {
            List<DependencyNode> currLayer = flatList[i];

            result[i] = currLayer.ToArray();
        }

        return result;
    }

    private static void FlattenRecursive(DependencyNode node , List<List<DependencyNode>> flat , int depthIndex)
    {
        while (flat.Count < depthIndex + 1)
        {
            flat.Add(new List<DependencyNode>());
        }

        List<DependencyNode> lst = flat[depthIndex];
        lst.Add(node);

        foreach (DependencyNode s in node.Dependencies)
        {
            FlattenRecursive(s , flat , depthIndex + 1);
        }
    }
}
