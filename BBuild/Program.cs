using System.Diagnostics;

namespace Bloodthirst.BBuild.Front;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        string rootProjectPath = Environment.CurrentDirectory;
        
        if (args.Length == 2 && args[0] == "/Path")
        {
            rootProjectPath = args[1];    
        }

        DirectoryInfo? rootFolderPath = new DirectoryInfo(rootProjectPath);

        BuildSettings? settings = BuildUtils.GetFromPath(rootProjectPath);
        if (settings == null)
        {
            return -1;
        }

        BuildContext context = new BuildContext()
        {
            ExecutablePath = Environment.ProcessPath!.Replace("\\", "/"),
            RootFolderPath = rootFolderPath.FullName.Replace("\\", "/")
        };

        DependencyNode? rootNode = DependencyTreeBuilder.BuildDependencies(rootProjectPath);
        Debug.Assert(rootNode != null);

        DependencyNode[][] parallelNodes = DependencyTreeBuilder.FlattenDependencyNodes(rootNode);

        foreach (DependencyNode[] parallelLayer in parallelNodes.Reverse())
        {
            foreach (DependencyNode dep in parallelLayer)
            {
                BuildSettings currSettings = dep.BuildSettings;
                Builder builder = new Builder(currSettings, context);
                BuildExports result = await builder.Build();

                if (result.Result != BuildExports.BuildResult.Success)
                {
                    return -1;
                }
            }
        }

        return 0;
        
    }

}
