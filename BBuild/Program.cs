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

        if (!BuildUtils.GetFromPath(rootProjectPath, out BuildSettings? settings, out BuildDependencies? dependencies))
        {
            return -1;
        }

        BuildContext context = new BuildContext()
        {
            ExecutablePath = Environment.ProcessPath!.Replace("\\", "/"),
            RootFolderPath = rootFolderPath.FullName.Replace("\\", "/")
        };

        DependencyNode? rootNode = DependencyTreeBuilder.BuildDependencies(rootProjectPath);

        DependencyNode[][] parallelNodes = DependencyTreeBuilder.FlattenDependencyNodes(rootNode);

        foreach (var parallelLayer in parallelNodes.Reverse())
        {
            foreach(var dep in parallelLayer)
            {
                BuildDependencies currDependencies = dep.BuildDependencies;
                BuildSettings currSettings = dep.BuildSettings;
                Builder builder = new Builder(currDependencies, currSettings, context);
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
