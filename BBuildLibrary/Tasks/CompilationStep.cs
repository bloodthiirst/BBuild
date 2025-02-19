using System;
using System.Diagnostics;
using System.Text;

namespace Bloodthirst.BBuild;
public sealed class CompilationStep
{
    private readonly BuildSettings settings;
    private readonly BuildContext context;

    public CompilationStep(BuildSettings settings, BuildContext context)
    {
        this.settings = settings;
        this.context = context;
    }

    public void GetCommand(BuildExports exports, out string filePath, out IReadOnlyList<string> arguments)
    {
        List<string> args = new List<string>();

        filePath = settings.CompilerResources.CompilerPath;
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
                sb.Append($"/MP{val}");
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
                    if (s == Sanitizers.AddressFuzzer)
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
                    case LanguageStandard.C11: { sb.Append("/std:c14"); break; }
                    case LanguageStandard.C17: { sb.Append("/std:c17"); break; }
                    case LanguageStandard.CLatest: { sb.Append("/std:clatest"); break; }
                    case LanguageStandard.Cpp11: { sb.Append("/std:c++11"); break; }
                    case LanguageStandard.Cpp14: { sb.Append("/std:c++14"); break; }
                    case LanguageStandard.Cpp17: { sb.Append("/std:c++17"); break; }
                    case LanguageStandard.Cpp20: { sb.Append("/std:c++20"); break; }
                    case LanguageStandard.Cpp23Preview: { sb.Append("/std:c++23preview"); break; }
                    case LanguageStandard.CppLatest: { sb.Append("/std:c++latest"); break; }
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

        foreach (string includeFolder in settings.HeaderIncludeFolders)
        {
            string path = BuildUtils.EnsurePathIsAbsolute(includeFolder, settings);
            args.Add("/I");
            args.Add(path);
        }

        args.Add("/FC"); // should full file path when printing errors and warnings
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
            path = BuildUtils.EnsurePathIsAbsolute(path, settings);

            args.Add(path);
        }
    }

    public async Task<int> CompileAsync(BuildExports exports)
    {
        GetCommand(exports, out string filePath, out IReadOnlyList<string> args);

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

        // stdStr format : multiline text giving the result of the compilation
        // if the file "math.cpp" has no errors , then we would just get one line with the value "math.cpp"
        // if the file "math.cpp" has error , then we would get a line with value "math.cpp" followed by multiple lines with each one representing an error
        string stdStr = await process.StandardOutput.ReadToEndAsync();

        CompilationMessage[] messages = ParseCompilationOutput(stdStr);

        exports.CompilationMessages = messages;
        
        process.Dispose();
        return compilationResult;
    }

    /// <summary>
    /// Parse the text produced by the compiler after compilation into a structured format
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private CompilationMessage[] ParseCompilationOutput(string str)
    {
        List<CompilationMessage> messages = new List<CompilationMessage>();
        List<MessagePerFile> messagesPerFile = new List<MessagePerFile>();

        int i = 0;
        while (i < str.Length)
        {
            CompilationMessage msg = new CompilationMessage();
            ReadOnlySpan<char> fileName;

            // get file name
            {
                int start = i;
                int end = str.IndexOf("\r\n", start);
                int len = end - 1 - start + 1;
                fileName = str.AsSpan(start, len);
                i += len + 2;
            }

            messagesPerFile.Clear();

            // iterate over the next lines to gather the warnings and errors
            while (true)
            {
                if(i >= str.Length)
                {
                    msg.Filename = fileName.ToString();
                    msg.Messages = messagesPerFile.ToArray();
                    messages.Add(msg);
                    break;
                }

                int start = i;
                int end = str.IndexOf("\r\n", start);
                int len = end - 1 - start + 1;
                i += len + 2;

                // if we reach the end of the text without any text
                if (end == -1)
                {
                    msg.Filename = fileName.ToString();
                    msg.Messages = messagesPerFile.ToArray();
                    messages.Add(msg);
                    break;
                }

                ReadOnlySpan<char> line = str.AsSpan(start, len);

                // if the next line is a filename , that means we're done with the current file , add and exit
                if (BuildUtils.IsFileName(line))
                {
                    msg.Filename = fileName.ToString();
                    msg.Messages = messagesPerFile.ToArray();
                    messages.Add(msg);
                    break;
                }

                int lineStart = line.IndexOf('(');
                int lineEnd = line.IndexOf(')');
                int lineVal;
                MessageType messageType = MessageType.Warning;
                ReadOnlySpan<char> filePath;
                ReadOnlySpan<char> messageTxt;

                // filepath
                {
                    filePath = line.Slice(0, lineStart);
                }

                // line number
                {
                    ReadOnlySpan<char> lineSpan = line.Slice(lineStart + 1, lineEnd - lineStart - 1);
                    lineVal = int.Parse(lineSpan);
                }

                // message type
                {
                    if(line.Slice(lineEnd + 3).StartsWith("error"))
                    {
                        messageType = MessageType.Error;
                    }
                    if (line.Slice(lineEnd + 3).StartsWith("warning"))
                    {
                        messageType = MessageType.Warning;
                    } 
                }

                // text
                {
                    messageTxt = line.Slice(lineEnd + 3);
                }

                MessagePerFile perLine = new MessagePerFile
                {
                    Filepath = filePath.ToString().Replace('\\' , '/'),
                    Text = messageTxt.ToString(),
                    LineNumber = lineVal,
                    Type = messageType
                };

                messagesPerFile.Add(perLine);
            }
        }

        return messages.ToArray();
    }
}
