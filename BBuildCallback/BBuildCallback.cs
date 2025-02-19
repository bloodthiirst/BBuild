using Bloodthirst.BBuild;
using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Class containing examples of build callbacks that can be used for BBuild
/// </summary>
public static class BBuildCallback
{
    /// <summary>
    /// Prebuild method that deletes all the files from the "object files" folder of calling project
    /// </summary>
    public static void CleanupObjectFilesFolder(BuildSettings settings, BuildContext context, JsonElement[] parameters)
    {
        string objectFilesPath = settings.ObjectFilesPath;

        if (!Path.IsPathRooted(objectFilesPath))
        {
            objectFilesPath = Path.GetFullPath(objectFilesPath, settings.AbsolutePath);
        }

        Console.WriteLine($"> Cleanup of the object files for project : {settings.Name}");
        foreach (string file in Directory.EnumerateFiles(objectFilesPath))
        {
            Console.WriteLine($"\t> {file} deleted");
            File.Delete(file);
        }
    }

    /// <summary>
    /// Prebuild method that deletes all the files from the "Build Output" folder of calling project , the type of "Build Output" is passed through the parameters as a string
    /// </summary>
    public static void CleanupOutputFolder(BuildSettings settings, BuildContext context, JsonElement[] parameters)
    {
        Debug.Assert(parameters != null);
        Debug.Assert(parameters.Length == 1);

        JsonElement param = parameters[0];

        Debug.Assert(param.ValueKind == JsonValueKind.String);
        string outputType = param.GetString()!;

        if (!Enum.TryParse(typeof(BuildOutput), outputType, out object? result))
        {
            Console.Error.WriteLine($"Error while parsing {nameof(BuildOutput)} , {outputType} doesn't seem to be part of the enum entries");
            return;
        }

        BuildOutput outputToCleanup = (BuildOutput)result;
        BuildOutputInfo? output = settings.BuildOutputs.FirstOrDefault(o => o.OutputType == outputToCleanup);

        if (output == null)
        {
            Console.Error.WriteLine($"The project {settings.Name} doesn't seem to have a {nameof(BuildOutput)} of type {outputToCleanup}");
            return;
        }

        string outputPath = output.FolderPath;
        if (!Path.IsPathRooted(outputPath))
        {
            outputPath = Path.GetFullPath(outputPath, settings.AbsolutePath);
        }

        Console.WriteLine($"> Cleanup of the output of type {outputType} for project : {settings.Name}");

        foreach (string file in Directory.EnumerateFiles(outputPath))
        {
            Console.WriteLine($"\t> {file} deleted");
            File.Delete(file);
        }
    }

    /// <summary>
    /// Postbuild method used to copy a Dll produced from a dependency and paste it next to the executable
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="context"></param>
    /// <param name="parameters"></param>
    public static void CopyOutputDllNextToExe(BuildSettings settings, BuildContext context, BuildExports export, JsonElement[] parameters)
    {
        // get the dependency name from the parameters
        string dependencyName = string.Empty;
        {
            Debug.Assert(parameters != null);
            Debug.Assert(parameters.Length == 1);

            JsonElement param = parameters[0];
            Debug.Assert(param.ValueKind == JsonValueKind.String);

            dependencyName = param.GetString()!;
        }

        // get the build settings of the dependency
        BuildSettings? depSettings = null;
        {
            DependencySettings? dependencyInfo = settings.DependencyPaths.FirstOrDefault(d => d.Name == dependencyName);
            Debug.Assert(dependencyInfo != null);

            string dependencyPath = dependencyInfo.Path;
            dependencyPath = BuildUtils.EnsurePathIsAbsolute(dependencyPath, settings);

            depSettings = BuildUtils.GetFromPath(dependencyPath);
        }

        Debug.Assert(depSettings != null);

        // get the Dll name and the absolute path to the source Dll
        string DllName = string.Empty;
        string absolutePathToDll = string.Empty;
        {
            BuildOutputInfo? dllOutput = depSettings.BuildOutputs.FirstOrDefault(o => o.OutputType == BuildOutput.Dll);
            Debug.Assert(dllOutput != null);

            string absolutePathDllFolder = Path.GetFullPath(dllOutput.FolderPath, depSettings.AbsolutePath);

            DllName = dllOutput.Filename;
            absolutePathToDll = Path.GetFullPath(DllName + ".dll", absolutePathDllFolder);
        }

        // get the absolute path of the destination Dll
        string absolutePathToDestination = string.Empty;
        {
            BuildOutputInfo? exeOutput = settings.BuildOutputs.FirstOrDefault(o => o.OutputType == BuildOutput.Executable);
            Debug.Assert(exeOutput != null);

            string absolutePathToExeFolder = Path.GetFullPath(exeOutput.FolderPath, settings.AbsolutePath);
            absolutePathToDestination = Path.GetFullPath(DllName + ".dll", absolutePathToExeFolder);
        }

        // check that Dll exits in the dependency output
        Debug.Assert(File.Exists(absolutePathToDll));

        // finally we perform the Dll copy
        File.Copy(absolutePathToDll, absolutePathToDestination, true);

        Console.WriteLine($"> Copied Dll from : {absolutePathToDll} to {absolutePathToDestination}");
    }

    /// <summary>
    /// Prebuild method that outputs a simple log message
    /// </summary>
    public static void PrebuildAction(BuildSettings settings, BuildContext context, JsonElement[] parameters)
    {
        Console.WriteLine($"> Prebuild called from project : {settings.Name} with {parameters.Length} params passed");
    }

    /// <summary>
    /// Postbuild method that outputs a simple log message
    /// </summary>
    public static void PostbuildAction(BuildSettings settings, BuildContext context, BuildExports export, JsonElement[] parameters)
    {
        Console.WriteLine($"> Postbuild called from project : {settings.Name} with {parameters.Length} params passed");
    }
}
