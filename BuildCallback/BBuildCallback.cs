using Bloodthirst.BBuild;
using System.Diagnostics;
using System.Text.Json;

public static class BBuildCallback
{
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

    public static void CopyOutputDllNextToExe(BuildSettings settings, BuildContext context, JsonElement[] parameters)
    {
        Debug.Assert(parameters != null);
        Debug.Assert(parameters.Length == 1);

        JsonElement param = parameters[0];

        Debug.Assert(param.ValueKind == JsonValueKind.String);
        string dependencyName = param.GetString()!;

        DependencySettings? dependencyInfo = settings.DependencyPaths.FirstOrDefault(d => d.Name == dependencyName);
        Debug.Assert(dependencyInfo != null);

        string dependencyPath = dependencyInfo.Path;

        if (!Path.IsPathRooted(dependencyPath))
        {
            dependencyPath = Path.GetFullPath(dependencyPath, settings.AbsolutePath);
        }

        BuildSettings? depSettings = BuildUtils.GetFromPath(dependencyPath);
        if (depSettings == null)
        {
            return;
        }

        Debug.Assert(depSettings != null);

        BuildOutputInfo? dllOutput = depSettings.BuildOutputs.FirstOrDefault(o => o.OutputType == BuildOutput.Dll);
        Debug.Assert(dllOutput != null);

        string absPathFromDllFolder = Path.GetFullPath(dllOutput.FolderPath, depSettings.AbsolutePath);
        string absPathFromDll = Path.GetFullPath(dllOutput.Filename + ".dll", absPathFromDllFolder);

        BuildOutputInfo? exeOutput = settings.BuildOutputs.FirstOrDefault(o => o.OutputType == BuildOutput.Executable);
        Debug.Assert(exeOutput != null);

        string absPathToExeFolder = Path.GetFullPath(exeOutput.FolderPath, settings.AbsolutePath);
        string absPathToDll = Path.GetFullPath(dllOutput.Filename + ".dll", absPathToExeFolder);

        Debug.Assert(File.Exists(absPathFromDll));

        // Delete old Dll
        if (File.Exists(absPathToDll))
        {
            File.Delete(absPathToDll);
            Console.WriteLine($"> Deleted old Dll at : {absPathToDll}");
        }

        File.Copy(absPathFromDll, absPathToDll, true);
        Console.WriteLine($"> Copied Dll from : {absPathFromDll} to {absPathToDll}");
    }

    public static void PrebuildAction(BuildSettings settings, BuildContext context, JsonElement[] parameters)
    {
        Console.WriteLine($"> Prebuild called from project : {settings.Name} with {parameters.Length} params passed");
    }

    public static void PostbuildAction(BuildSettings settings, BuildContext context, JsonElement[] parameters)
    {
        Console.WriteLine($"> Postbuild called from project : {settings.Name} with {parameters.Length} params passed");
    }
}
