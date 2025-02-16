using System.Text.Json.Serialization;

namespace Bloodthirst.BBuild;

public sealed class DependencySettings
{
    public string Name { get; set; }
    public string Path { get; set; }
    public BuildOutput Output { get; set; }
}

public sealed class BuildCallback
{
    public string DllPath { get; set; }
    public string MethodAssemblyName { get; set; }
    public object[] Params { get; set; }
}

public sealed class BuildSettings
{
    [JsonIgnore]
    public string AbsolutePath { get; set; }
    public string Name { get; set; }

    public string Description { get; set; }

    public List<DependencySettings> DependencyPaths { get; set; } = new List<DependencySettings>();

    public Dictionary<string,string> CustomVariables { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Action from a Dll to call before the build
    /// </summary>
    public BuildCallback[] PrebuildAction { get; set; } = [];

    /// <summary>
    /// Action from a Dll to call after the build
    /// </summary>
    public BuildCallback[] PostbuildAction { get; set; } = [];

    public CompilationSettings CompilationSettings { get; set; } = new CompilationSettings();

    /// <summary>
    /// Relative paths to the source files
    /// </summary>
    public List<string> SourceFiles { get; set; } = new List<string>();

    /// <summary>
    /// Paths to libraries to link with
    /// </summary>
    public List<string> LibraryFiles { get; set; } = new List<string>();

    /// <summary>
    /// Absolute paths to the folder used by the linker to look for libraries
    /// </summary>
    public List<string> LibrariesFolderPaths { get; set; } = new List<string>();

    /// <summary>
    /// Absolute paths to the header include folders
    /// </summary>
    public List<string> HeaderIncludeFolders { get; set; } = new List<string>();

    /// <summary>
    /// Compiler flags to add
    /// </summary>
    public List<string> CompilerFlags { get; set; } = new List<string>();

    /// <summary>
    /// List of the needed outputs from this project
    /// </summary>
    public List<BuildOutputInfo> BuildOutputs { get; set; } = new List<BuildOutputInfo>();
    public string PBDFilename { get; set; }

    /// <summary>
    /// Absolute path to the folder containing the objects files
    /// </summary>
    public string ObjectFilesPath { get; set; }

}
