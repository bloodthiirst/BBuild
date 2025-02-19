using System.Data;
using System.Diagnostics;

namespace Bloodthirst.BBuild;

public sealed class DependencyTreeBuilder
{
    /// <summary>
    /// Given a path to a root project, returns a root node to a tree structure describing the dependencies between projects 
    /// </summary>
    /// <param name="absoluteRootProjectPath"></param>
    /// <returns>The root node to the tree, or <see cref="null"/> in case of an error</returns>
    public static DependencyNode? BuildDependencies(string absoluteRootProjectPath)
    {
        BuildSettings? settings = BuildUtils.GetFromPath(absoluteRootProjectPath);
        if (settings == null)
        {
            Console.Error.WriteLine($"Couldn't get project info at : {absoluteRootProjectPath}");
            return null;
        }

        DependencyNode node = new DependencyNode()
        {
            Parent = null,
            BuildSettings = settings,
            Dependencies = new List<DependencyNode>()
        };

        Dictionary<string,int> dependencyCountLookup = new Dictionary<string,int>();
        foreach(DependencySettings d in settings.DependencyPaths)
        {
            if(dependencyCountLookup.TryGetValue(d.Name, out int val))
            {
                dependencyCountLookup[d.Name] = val + 1;
            }
            else
            {
                dependencyCountLookup.Add(d.Name, 1);
            }
        }

        if (dependencyCountLookup.Count != settings.DependencyPaths.Count)
        {
            Console.Error.WriteLine($"Found duplicates in the dependency list for the project : {settings.Name}");
            foreach (KeyValuePair<string, int> kv in dependencyCountLookup)
            {
                Debug.WriteLineIf(kv.Value != 1, $"{kv.Key} has been duplicated {kv.Value} time(s)");
            }
            return null;
        }
        foreach (DependencySettings dep in settings.DependencyPaths)
        {
            string dependencyFullPath = dep.Path;
            dependencyFullPath = BuildUtils.EnsurePathIsAbsolute(dependencyFullPath, settings);

            DependencyNode? subNode = BuildDependencies(dependencyFullPath);

            if (subNode == null)
            {
                continue;
            }

            subNode.Parent = node;
            node.Dependencies.Add(subNode);

            // check for recursive loop
            DependencyNode? p = subNode.Parent;
            while(p != null)
            {
                if( p == subNode)
                {
                    Debug.Fail($"Found a recursive include with the project {p.BuildSettings.Name} , make sure to fix it to get a correct build");
                    return null;
                }

                p = p.Parent;
            }
        }

        return node;
    }

    public static DependencyNode[][] FlattenDependencyNodes(DependencyNode root)
    {
        List<List<DependencyNode>> flatList = new List<List<DependencyNode>>();
        Dictionary<string, (int, int)> nodeToIndexLookup = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);
        FlattenRecursive(root, flatList, 0);

        DependencyNode[][] result = new DependencyNode[flatList.Count][];

        for (int layerIdx = 0; layerIdx < flatList.Count; layerIdx++)
        {
            List<DependencyNode> currLayer = flatList[layerIdx];

            // we reverse the order so that , dependencies come before their dependents
            int reversedLayerIdx = flatList.Count - layerIdx - 1;

            for (int depIdx = 0; depIdx < currLayer.Count; depIdx++)
            {
                DependencyNode d = currLayer[depIdx];
                string dependencyPath = d.BuildSettings.AbsolutePath;
                if (nodeToIndexLookup.TryGetValue(dependencyPath, out (int layer, int index) existingIdx))
                {
                    Debug.Fail($"Dependency {d.BuildSettings.Name} already exists at [ layer : {existingIdx.layer}, index : {existingIdx.index} ]");
                }
                else
                {
                    nodeToIndexLookup.Add(dependencyPath, (reversedLayerIdx, depIdx));
                }
            }

            result[reversedLayerIdx] = currLayer.ToArray();
        }

        return result;
    }

    private static void FlattenRecursive(DependencyNode node, List<List<DependencyNode>> flat, int depthIndex)
    {
        while (flat.Count < depthIndex + 1)
        {
            flat.Add(new List<DependencyNode>());
        }

        List<DependencyNode> lst = flat[depthIndex];
        lst.Add(node);

        foreach (DependencyNode s in node.Dependencies)
        {
            FlattenRecursive(s, flat, depthIndex + 1);
        }
    }
}
