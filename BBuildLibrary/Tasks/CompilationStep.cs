using System.Diagnostics;
using System.Text;

namespace Bloodthirst.BBuild;
public sealed class CompilationStep
{
    private readonly BuildDependencies dependencies;
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public CompilationStep(BuildDependencies dependencies, BuildSettings settings, BuildContext context)
    {
        this.dependencies = dependencies;
        this.settings = settings;
        this.context = context;
    }

    public void GetCommand(BuildExports exports, out string filePath, out IReadOnlyList<string> arguments)
    {
        List<string> args = new List<string>();

        filePath = dependencies.CompilerPath;
        arguments = args;

        // add the flags
        {
            args.Add("/EHsc"); // exception handling
            args.Add("/W4"); // wanrining level
            args.Add("/Zi"); // generate debug information , will result in PDB file generated
            args.Add("/TP"); // usually the target language is based on the .cpp extension , we force it using this arg here
            args.Add("/MP8"); // user multiple processes for compilation , in this example we will use 8 cores
            args.Add("/c"); // generate OBJs only , no linking
            args.Add("/fsanitize=address"); // address sanitizer , needs to have "generate debug info" to be enabled
            args.Add("/jumptablerdata"); // Place jump tables for switch case statements in .rdata section,  only x64
            args.Add("/std:c++17"); // define cpp version
            args.Add($"/FS"); // syncronous PDB writes , is set by default when /MP[n] is enabled
            args.Add($"/Fd:{settings.AbsolutePath}/{settings.ObjectFilesPath}/{settings.PBDFilename}"); // specify the PDB filename
            args.Add($"/Fo{settings.AbsolutePath}/{settings.ObjectFilesPath}/"); // set the path to export .obj files in
        }

        // from settings
        foreach (string f in settings.CompilerFlags)
        {
            args.Add($"/D {f}="); // add the define without a value , equivalent to #define f
        }

        // add the source files
        foreach (string sourceFilePath in settings.SourceFiles)
        {
            string path = sourceFilePath;

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path, settings.AbsolutePath).Replace("\\", "/");
            }

            args.Add(path);
        }
    }

    public async Task<int> CompileAsync(BuildExports exports)
    {
        GetCommand(exports, out string filePath, out IReadOnlyList<string> args);

        ProcessStartInfo startInfo = new ProcessStartInfo(filePath, args);

        // add include directories
        StringBuilder includeFolders = new StringBuilder();
        {
            foreach (string includeFolder in settings.HeaderIncludeFolders)
            {
                includeFolders.Append($"{includeFolder};");
            }
        }
        startInfo.EnvironmentVariables.Add("INCLUDE", includeFolders.ToString());

        TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

        Process process = new Process()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(process.ExitCode);
            process.Dispose();
        };

        process.Start();

        int compilationResult = await tcs.Task;

        return compilationResult;
    }
}
