using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;

namespace Bloodthirst.BBuild;
public sealed class ExecutableOutput
{
    private readonly BuildDependencies dependencies;
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public ExecutableOutput(BuildDependencies dependencies, BuildSettings settings, BuildContext context)
    {
        this.dependencies = dependencies;
        this.settings = settings;
        this.context = context;
    }

    public void GetCommand(BuildOutputInfo output, BuildExports exports, out string filePath, out IReadOnlyList<string> arguments)
    {
        List<string> args = new List<string>();

        filePath = dependencies.LinkerPath;
        arguments = args;

        string absoluteExePath = $"{output.FolderPath}/{output.Filename}.exe";
        if (!Path.IsPathRooted(absoluteExePath))
        {
            absoluteExePath = Path.GetFullPath(absoluteExePath, settings.AbsolutePath).Replace("\\", "/");
        }

        args.Add($"/OUT:{absoluteExePath}");

        // args
        args.Add("/DEBUG:FULL");
        args.Add("/SUBSYSTEM:CONSOLE");
        args.Add("/CGTHREADS:4");

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

        foreach (string objectFilePath in exports.ObjectFiles)
        {
            string path = objectFilePath;

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path, settings.AbsolutePath).Replace("\\", "/");
            }

            args.Add(path);
        }

        // add the lib files
        foreach (string libFilePath in settings.LibraryFiles)
        {
            string path = libFilePath;

            path = BuildUtils.ResolveStringWithVariables(path, settings);

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path, settings.AbsolutePath).Replace("\\", "/");
            }

            args.Add(path);
        }
    }

    public async Task<int> ExecutableAsync(BuildOutputInfo output, BuildExports exports)
    {
        string exeFilePath = $"{output.FolderPath}/{output.Filename}.exe";

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
            exports.Executables.Add(exeFilePath);
        }

        return compilationResult;
    }
}
