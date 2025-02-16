﻿using System.Diagnostics;
using System.Text;

namespace Bloodthirst.BBuild;
public sealed class CompilationStep
{
    private readonly BuildDependencies dependencies;
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public CompilationStep(BuildSettings settings, BuildDependencies dependencies, BuildContext context)
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

        // Complilation settings
        {
            StringBuilder sb = new StringBuilder(50);

            // exceptions
            {
                sb.Clear();
                sb.Append("/EH");
                ExceptionHandling[] val = settings.CompilationSettings.ExceptionHandlingOptions;
                if (val.Contains(ExceptionHandling.EHa))
                {
                    sb.Append('a');
                }
                if (val.Contains(ExceptionHandling.EHc))
                {
                    sb.Append('c');
                }
                if (val.Contains(ExceptionHandling.EHr))
                {
                    sb.Append('r');
                }
                if (val.Contains(ExceptionHandling.EHs))
                {
                    sb.Append('s');
                }
                
                args.Add(sb.ToString()); // exception handling
            }

            // wanrining level
            {
                sb.Clear();
                sb.Append('/');
                WarningLevel val = settings.CompilationSettings.WarningLevel;
                sb.Append(val.ToString());
                args.Add(sb.ToString()); 
            }

            // debug information
            {
                DebugInformation val = settings.CompilationSettings.DebugInformation;

                // generate debug information , will result in PDB file generated
                if (val != DebugInformation.None)
                {
                    sb.Clear();
                    sb.Append('/');
                    sb.Append(val.ToString());
                    args.Add(sb.ToString());
                }
            }

            // multi processes
            {
                int val = settings.CompilationSettings.ProcessCount;
                sb.Clear();
                sb.Append($"/MP{val}" );
                args.Add(sb.ToString());
            }

            // sanitizer
            // address sanitizer , needs to have "generate debug info" to be enabled
            {
                foreach (Sanitizers s in settings.CompilationSettings.EnabledSanitizers)
                {
                    if (s == Sanitizers.AddressSanitizer)
                    {
                        args.Add("/fsanitize=address");
                    }
                    if(s == Sanitizers.AddressFuzzer)
                    {
                        args.Add("/fsanitize=fuzzer");
                    }
                }
            }

            // Place jump tables for switch case statements in .rdata section
            // only x64
            if (settings.CompilationSettings.UseJumpTableRData)
            {
                args.Add("/jumptablerdata");
            }

            // language
            // define cpp version
            {
                sb.Clear();

                LanguageStandard lang = settings.CompilationSettings.LanguageStandard;
                switch (lang)
                {
                    case LanguageStandard.C11:          { sb.Append("/std:c14"); break; }
                    case LanguageStandard.C17:          { sb.Append("/std:c17"); break; }
                    case LanguageStandard.CLatest:      { sb.Append("/std:clatest"); break; }
                    case LanguageStandard.Cpp11:        { sb.Append("/std:c++11"); break; }
                    case LanguageStandard.Cpp14:        { sb.Append("/std:c++14"); break; }
                    case LanguageStandard.Cpp17:        { sb.Append("/std:c++17"); break; }
                    case LanguageStandard.Cpp20:        { sb.Append("/std:c++20"); break; }
                    case LanguageStandard.Cpp23Preview: { sb.Append("/std:c++23preview"); break; }
                    case LanguageStandard.CppLatest:    { sb.Append("/std:c++latest"); break; }
                    default: { Debug.Fail($"Case {lang} not handled"); break; }
                }
                
                args.Add(sb.ToString());

            }

            // optimization
            {
                sb.Clear();
                sb.Append("/O");
                OptimizationLevel[] options = settings.CompilationSettings.OptimizationLevelOptions;
                foreach (OptimizationLevel optimizationOpt in options)
                {
                    switch (optimizationOpt)
                    {
                        case OptimizationLevel.O1: { sb.Append('1'); break; }
                        case OptimizationLevel.O2: { sb.Append('2'); break; }
                        case OptimizationLevel.Ob: { sb.Append('b'); break; }
                        case OptimizationLevel.Od: { sb.Append('d'); break; }
                        case OptimizationLevel.Oi: { sb.Append('i'); break; }
                        case OptimizationLevel.Os: { sb.Append('s'); break; }
                        case OptimizationLevel.Ot: { sb.Append('t'); break; }
                        case OptimizationLevel.Oy: { sb.Append('y'); break; }
                        default: { Debug.Fail($"Case {optimizationOpt} not handled"); break; }
                    }
                }

                args.Add(sb.ToString());
            }
        }

        args.Add("/c"); // generate OBJs only , no linking
        args.Add($"/FS"); // syncronous PDB writes , is set by default when /MP[n] is enabled
        args.Add("/TP"); // usually the target language is based on the .cpp extension , we force it using this arg here
        args.Add($"/Fd:{settings.AbsolutePath}/{settings.ObjectFilesPath}/{settings.PBDFilename}"); // specify the PDB filename
        args.Add($"/Fo{settings.AbsolutePath}/{settings.ObjectFilesPath}/"); // set the path to export .obj files in

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
