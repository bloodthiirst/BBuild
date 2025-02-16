using System.Diagnostics;
using System.Text;

namespace Bloodthirst.BBuild;
public sealed class StaticLibraryOutput
{
    private readonly BuildDependencies dependencies;
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public StaticLibraryOutput(BuildDependencies dependencies, BuildSettings settings , BuildContext context)
    {
        this.dependencies = dependencies;
        this.settings = settings;
        this.context = context;
    }

    public void GetCommand(BuildOutputInfo output , BuildExports exports , out string filePath , out IReadOnlyList<string> arguments)
    {
        StringBuilder sb = new StringBuilder();

        List<string> args = new List<string>();

        arguments = args;
        filePath = dependencies.LibPath;

        args.Add($"/OUT:{output.FolderPath}/{output.Filename}.lib");

        // folders to search in
        foreach (string lib in settings.LibrariesFolderPaths)
        {
            args.Add($"/LIBPATH:{lib} ");
        }

        foreach (string objectFile in exports.ObjectFiles)
        {
            args.Add(objectFile);
        }
    }

    public async Task<int> StaticLibraryAsync(BuildOutputInfo output, BuildExports exports)
    {
        string libFilePath = $"{output.FolderPath}/{output.Filename}.lib";

        GetCommand(output, exports, out string filePath, out IReadOnlyList<string> args);

        ProcessStartInfo startInfo = new ProcessStartInfo(filePath, args);

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

        if (compilationResult == 0)
        {
            exports.Libraries.Add(libFilePath);
        }

        return compilationResult;
    }
}
