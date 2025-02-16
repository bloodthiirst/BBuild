using System.Diagnostics;
using System.Text;

namespace Bloodthirst.BBuild;
public sealed class DynamicLibraryOutput
{
    private readonly BuildDependencies dependencies;
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public DynamicLibraryOutput(BuildDependencies dependencies, BuildSettings settings, BuildContext context)
    {
        this.dependencies = dependencies;
        this.settings = settings;
        this.context = context;
    }
    public void GetCommand(BuildOutputInfo output, BuildExports exports, out string filePath, out IReadOnlyList<string> arguments)
    {
        StringBuilder sb = new StringBuilder();

        List<string> args = new List<string>();

        filePath = dependencies.LinkerPath;
        arguments = args;

        string absoluteDllPath = $"{output.FolderPath}/{output.Filename}.dll";
        if (!Path.IsPathRooted(absoluteDllPath))
        {
            absoluteDllPath = Path.GetFullPath(absoluteDllPath, settings.AbsolutePath).Replace("\\", "/");
        }

        args.Add("/DLL");
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

        // out path
        args.Add($"/OUT:{absoluteDllPath}");

        foreach (string objectFilePath in exports.ObjectFiles)
        {
            string path = objectFilePath;

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path, settings.AbsolutePath).Replace("\\", "/");
            }

            args.Add(path);
        }

    }
    public async Task<int> DynamicLibraryAsync(BuildOutputInfo output, BuildExports exports)
    {
        string dllFilePath = $"{output.FolderPath}/{output.Filename}.dll";

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
            exports.Dlls.Add(dllFilePath);
        }

        return compilationResult;
    }
}
