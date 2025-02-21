using System.Diagnostics;
using System.Text;

namespace Bloodthirst.BBuild;
public sealed class StaticLibraryOutput
{
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public StaticLibraryOutput(BuildSettings settings, BuildContext context)
    {
        this.settings = settings;
        this.context = context;
    }

    public void GetCommand(BuildOutputInfo output, BuildExports exports, out string filePath, out IReadOnlyList<string> arguments)
    {
        StringBuilder sb = new StringBuilder();

        List<string> args = new List<string>();

        arguments = args;

        filePath = BuildUtils.EnsurePathIsAbsolute(settings.CompilerResources.LibPath, settings);

        args.Add($"/OUT:{output.FolderPath}/{output.Filename}.lib");
        args.Add("/NOLOGO"); // supress the "copyright" text at the start

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
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

        Process process = new Process()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = false,
        };

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(process.ExitCode);
        };

        process.Start();

        int compilationResult = await tcs.Task;

        LinkingMessage[] messages = Array.Empty<LinkingMessage>();

        if (compilationResult == 0)
        {
            exports.Dlls.Add(libFilePath);
        }
        else
        {
            string stdStr = await process.StandardOutput.ReadToEndAsync();
            messages = BuildUtils.ParseLinkingOutput(stdStr);

            exports.LinkingMessages = messages;
        }

        process.Dispose();

        return compilationResult;
    }
}
