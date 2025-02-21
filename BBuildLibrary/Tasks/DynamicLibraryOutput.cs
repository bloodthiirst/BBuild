using System.Diagnostics;
using System.Text;

namespace Bloodthirst.BBuild;
public sealed class DynamicLibraryOutput
{
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public DynamicLibraryOutput(BuildSettings settings, BuildContext context)
    {
        this.settings = settings;
        this.context = context;
    }
    public void GetCommand(BuildOutputInfo output, BuildExports exports, out string filePath, out IReadOnlyList<string> arguments)
    {
        StringBuilder sb = new StringBuilder();

        List<string> args = new List<string>();

        filePath = BuildUtils.EnsurePathIsAbsolute(settings.CompilerResources.LinkerPath, settings);
        arguments = args;

        string absoluteDllPath = $"{output.FolderPath}/{output.Filename}.dll";
        absoluteDllPath = BuildUtils.EnsurePathIsAbsolute(absoluteDllPath, settings);

        args.Add("/DLL");
        args.Add("/DEBUG:FULL");
        args.Add("/SUBSYSTEM:CONSOLE");
        args.Add("/CGTHREADS:4");
        args.Add("/NOLOGO"); // supress the "copyright" text at the start

        // platform
        {
            TargetPlatform platform = settings.CompilationSettings.Platform;
            switch (platform)
            {
                case TargetPlatform.Arm: { args.Add("/MACHINE:ARM"); break; }
                case TargetPlatform.Arm64: { args.Add("/MACHINE:ARM64"); break; }
                case TargetPlatform.Arm64EC: { args.Add("/MACHINE:ARM64EC"); break; }
                case TargetPlatform.EBC: { args.Add("/MACHINE:EBC"); break; }
                case TargetPlatform.x64: { args.Add("/MACHINE:X64"); break; }
                case TargetPlatform.x86: { args.Add("/MACHINE:X86"); break; }
                default: { Debug.Fail($"Case {platform} not handled"); break; }
            }
        }

        // folders to search in
        foreach (string lib in settings.LibrariesFolderPaths)
        {
            args.Add($"/LIBPATH:{lib}");
        }

        // out path
        args.Add($"/OUT:{absoluteDllPath}");

        foreach (string objectFilePath in exports.ObjectFiles)
        {
            string path = objectFilePath;
            path = BuildUtils.EnsurePathIsAbsolute(path, settings);

            args.Add(path);
        }

    }
    public async Task<int> DynamicLibraryAsync(BuildOutputInfo output, BuildExports exports)
    {
        string dllFilePath = $"{output.FolderPath}/{output.Filename}.dll";

        GetCommand(output, exports, out string filePath, out IReadOnlyList<string> args);

        ProcessStartInfo startInfo = new ProcessStartInfo(filePath, args);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

        Process process = new Process()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
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
            exports.Dlls.Add(dllFilePath);
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
