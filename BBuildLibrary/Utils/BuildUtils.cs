using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bloodthirst.BBuild;

public static class BuildUtils
{
    /// <summary>
    /// Fixed name of the json file containing the build settings of a project
    /// </summary>
    public const string BuildFilename = "Build.json";

    /// <summary>
    /// Fixed name of the json file containing the build cache of a project
    /// </summary>
    public const string CacheFilename = "Cache.json";

    /// <summary>
    /// <para>Get the case sensitive version on the path passed , the path can point to either a file or a folder</para>
    /// <para>NOTE : this assumes the path is absolute</para>
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetCaseSensitivePath(string path)
    {
        Debug.Assert(Path.IsPathRooted(path), "the path passed needs to be an absolute path");

        StringBuilder sb = new StringBuilder(path.Length);
        List<DirectoryInfo> tempDirectories = new List<DirectoryInfo>();

        int i = 0;

        // choose your path separator
        char separatorToUse = Path.AltDirectorySeparatorChar;

        //get volume
        {
            int idx = path.IndexOf(Path.VolumeSeparatorChar);

            for (int j = 0; j < idx; ++j)
            {
                sb.Append(char.ToUpper(path[i]));
            }

            sb.Append(Path.VolumeSeparatorChar);
            i = idx + 1;

            // skip next separators
            {
                bool appendSeparatorAtTheEnd = false;
                while (i < path.Length && (path[i] == Path.DirectorySeparatorChar || path[i] == Path.AltDirectorySeparatorChar))
                {
                    appendSeparatorAtTheEnd = true;
                    i++;
                }

                if (appendSeparatorAtTheEnd)
                {
                    sb.Append(separatorToUse);
                }
            }
        }

        // get folders
        {
            DirectoryInfo currDir = new DirectoryInfo(sb.ToString());

            while (i < path.Length)
            {
                int startIdx = i;

                // skip until separator
                while (i < path.Length && path[i] != Path.DirectorySeparatorChar && path[i] != Path.AltDirectorySeparatorChar)
                {
                    i++;
                }

                int len = i - startIdx;
                ReadOnlySpan<char> pathSegment = path.AsSpan(startIdx, len);

                // if it's a filename , should be the last entry anyways
                // so we append it and return
                if (Path.HasExtension(pathSegment))
                {
                    foreach (FileInfo fileInfo in currDir.EnumerateFiles())
                    {
                        bool isSameFile = fileInfo.Name.AsSpan().Equals(pathSegment, StringComparison.OrdinalIgnoreCase);

                        if (!isSameFile)
                        {
                            continue;
                        }

                        // this already includes the extension
                        sb.Append($"{fileInfo.Name}");
                        return sb.ToString();
                    }
                }

                tempDirectories.Clear();
                tempDirectories.AddRange(currDir.EnumerateDirectories());

                DirectoryInfo? nextDir = null;

                foreach (DirectoryInfo dir in tempDirectories)
                {
                    bool isSameFolder = dir.Name.AsSpan().Equals(pathSegment, StringComparison.OrdinalIgnoreCase);

                    if (!isSameFolder)
                    {
                        continue;
                    }

                    nextDir = dir;
                    break;
                }

                Debug.Assert(nextDir != null, $"Couldn't find a folder with a lowercase name {pathSegment.ToString().ToLower()} at {sb.ToString()}");

                sb.Append(nextDir.Name);

                currDir = nextDir;

                // skip next separators (yes , C:///// is a valid path so we skip all the separators at once)
                {
                    bool appendSeparatorAtTheEnd = false;
                    while (i < path.Length && (path[i] == Path.DirectorySeparatorChar || path[i] == Path.AltDirectorySeparatorChar))
                    {
                        appendSeparatorAtTheEnd = true;
                        i++;
                    }

                    if (appendSeparatorAtTheEnd)
                    {
                        sb.Append(separatorToUse);
                    }
                }
            }
        }

        return sb.ToString();
    }

    public static bool IsFileName(ReadOnlySpan<char> line)
    {
        int i = 0;

        bool foundDot = false;
        
        // read name
        while (i < line.Length)
        {
            char curr = line[i];
            i++;

            bool isSeparator = 
                curr == Path.DirectorySeparatorChar || 
                curr == Path.AltDirectorySeparatorChar || 
                curr == Path.VolumeSeparatorChar || 
                curr == Path.PathSeparator;
                
            if (isSeparator)
            {
                return false;
            }

            if (curr == '.')
            {
                foundDot = true;
                i++;
                break;
            }
        }

        if (!foundDot)
        {
            return false;
        }

        // read extension
        while (i < line.Length)
        {
            char curr = line[i];
            i++;

            if (!char.IsLetterOrDigit(curr))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parse the text produced by the compiler after compilation into a structured format
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static CompilationMessage[] ParseCompilationOutput(ReadOnlySpan<char> str)
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
                int end = str.Slice(start).IndexOf("\r\n");
                int len = end;
                fileName = str.Slice(start, len);
                i += len + 2;
            }

            messagesPerFile.Clear();

            // iterate over the next lines to gather the warnings and errors
            while (true)
            {
                if (i >= str.Length)
                {
                    msg.Filename = fileName.ToString();
                    msg.Messages = messagesPerFile.ToArray();
                    messages.Add(msg);
                    break;
                }

                int start = i;
                int end = str.Slice(start).IndexOf("\r\n");
                int len = end;
                i += len + 2;

                // if we reach the end of the text without any text
                if (end == -1)
                {
                    msg.Filename = fileName.ToString();
                    msg.Messages = messagesPerFile.ToArray();
                    messages.Add(msg);
                    break;
                }

                ReadOnlySpan<char> line = str.Slice(start, len);

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
                    if (line.Slice(lineEnd + 3).StartsWith("error"))
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
                    Filepath = filePath.ToString().Replace('\\', '/'),
                    Text = messageTxt.ToString(),
                    LineNumber = lineVal,
                    Type = messageType
                };

                messagesPerFile.Add(perLine);
            }
        }

        return messages.ToArray();
    }


    /// <summary>
    /// Parse the text produced by the linker after compilation into a structured format
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static LinkingMessage[] ParseLinkingOutput(ReadOnlySpan<char> str)
    {
        List<LinkingMessage> messages = new List<LinkingMessage>();

        int i = 0;
        while (i < str.Length)
        {
            int firstColon = -1;
            for (int j = i + 1; j < str.Length - 1; j++)
            {
                char prev = str[j - 1];
                char curr = str[j + 0];
                char next = str[j + 1];

                if (prev == ' ' && curr == ':' && next == ' ')
                {
                    firstColon = j;
                    break;
                }
            }

            Debug.Assert(firstColon != -1);
            firstColon -= i;

            int secondColon = str.Slice(i).Slice(firstColon + 1).IndexOf(':');
            int endOfLine = str.Slice(i).IndexOf("\r\n");

            ReadOnlySpan<char> filename;
            ReadOnlySpan<char> errorCode;
            ReadOnlySpan<char> messageTxt;

            // filepath
            {
                filename = str.Slice(i, firstColon - 1);
            }

            // error code
            {
                errorCode = str.Slice(i + firstColon + 2, secondColon - 1);
            }

            // text
            {
                int offset = firstColon + secondColon + 3;
                messageTxt = str.Slice(i).Slice(offset, endOfLine - offset);
            }

            LinkingMessage msg = new LinkingMessage()
            {
                Filename = filename.ToString(),
                ErrorCode = errorCode.ToString(),
                Text = messageTxt.ToString()
            };

            messages.Add(msg);

            i += endOfLine + 2;
        }

        return messages.ToArray();
    }

    /// <summary>
    /// Normalizes the input path an turns it into an absolute path , also fills in the custom variables used in the path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static string EnsurePathIsAbsolute(string path, BuildSettings settings)
    {
        path = ResolveStringWithVariables(path, settings);

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path, settings.AbsolutePath);
        }

        path = path.Replace("\\", "/");

        return path;
    }

    public static void SaveCacheToPath(string absoluteProjectPath, BuildCache cache)
    {
        string cachePath = $"{absoluteProjectPath}/{CacheFilename}";

        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.General);
        options.WriteIndented = true;

        using (FileStream fs = new FileStream(cachePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        using (MemoryStream sw = new MemoryStream())
        {
            string json = JsonSerializer.Serialize<BuildCache>(cache, options);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            fs.Position = 0;
            fs.SetLength(bytes.Length);
            fs.Write(bytes);
        }
    }

    /// <summary>
    /// Get the cache of the project located at <paramref name="absoluteProjectPath"/>
    /// </summary>
    /// <param name="absoluteProjectPath">Absolute path for the project folder</param>
    /// <returns>
    /// <para>Returns whether the method succeeded at getting the project information or not</para> 
    /// <para>Returns <see cref="true"/> if it's successfull and fills <paramref name="dependencies"/> and <paramref name="settings"/></para> 
    /// <para>Returns <see cref="false"/> if it fails and sets <paramref name="dependencies"/> and <paramref name="settings"/> to <see cref="null"/></para> 
    /// </returns>
    public static BuildCache? GetCacheFromPath(string absoluteProjectPath)
    {
        Debug.Assert(Path.IsPathRooted(absoluteProjectPath));

        DirectoryInfo? folderPath = new DirectoryInfo(absoluteProjectPath);
        List<FileInfo> files = new List<FileInfo>(folderPath.EnumerateFiles());

        List<FileInfo> buildFiles = new List<FileInfo>(files.Where(f => f.Name == CacheFilename));

        if (buildFiles.Count != 1)
        {
            return null;
        }

        FileInfo buildFile = buildFiles[0];

        JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        BuildCache? cache = null;
        using (FileStream buildFs = buildFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try
            {
                cache = JsonSerializer.Deserialize<BuildCache>(buildFs, jsonOptions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return null;
            }
        }

        return cache;
    }

    /// <summary>
    /// Get the settings and dependencies of the project located at <paramref name="absoluteProjectPath"/>
    /// </summary>
    /// <param name="absoluteProjectPath">Absolute path for the project folder</param>
    /// <param name="settings">Settings of the project located at <paramref name="absoluteProjectPath"/> , is set to null if the method fails</param>
    /// <returns>
    /// <para>Returns whether the method succeeded at getting the project information or not</para> 
    /// <para>Returns <see cref="true"/> if it's successfull and fills <paramref name="dependencies"/> and <paramref name="settings"/></para> 
    /// <para>Returns <see cref="false"/> if it fails and sets <paramref name="dependencies"/> and <paramref name="settings"/> to <see cref="null"/></para> 
    /// </returns>
    public static BuildSettings? GetSettingsFromPath(string absoluteProjectPath)
    {
        Debug.Assert(Path.IsPathRooted(absoluteProjectPath));

        DirectoryInfo? folderPath = new DirectoryInfo(absoluteProjectPath);
        List<FileInfo> files = new List<FileInfo>(folderPath.EnumerateFiles());

        List<FileInfo> buildFiles = new List<FileInfo>(files.Where(f => f.Name == BuildFilename));

        if (buildFiles.Count != 1)
        {
            string error = $"Build file was not found, make sure only one \"{BuildFilename}\" is present";
            return null;
        }

        FileInfo buildFile = buildFiles[0];

        JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        BuildSettings? settings = null;
        using (FileStream buildFs = buildFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try
            {
                settings = JsonSerializer.Deserialize<BuildSettings>(buildFs, jsonOptions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return null;
            }
        }

        if (settings != null)
        {
            settings.AbsolutePath = absoluteProjectPath;
        }

        return settings;
    }

    public static string ResolveStringWithVariables(string inputString, BuildSettings settings)
    {
        StringBuilder str = new StringBuilder(inputString);

        List<int> bracketPairs = new List<int>();
        FindBracketPairs(str, bracketPairs);

        while (bracketPairs.Count != 0)
        {
            Debug.Assert(bracketPairs.Count % 2 == 0);

            for (int i = bracketPairs.Count - 1; i >= 0; i -= 2)
            {
                int open = bracketPairs[i - 1];
                int close = bracketPairs[i];

                string key = str.ToString(open + 1, close - open - 1);
                if (!settings.CustomVariables.TryGetValue(key, out string? value))
                {
                    Debug.Fail($"Tried looking for the custom value {key} but didn't find it");
                    break;
                }

                str.Remove(open, close - open + 1);
                str.Insert(open, value);
            }

            bracketPairs.Clear();
            FindBracketPairs(str, bracketPairs);
        }

        return str.ToString();
    }

    private static void FindBracketPairs(StringBuilder inputString, List<int> bracketPairs)
    {
        int lastOpenIdx = -1;

        for (int i = 0; i < inputString.Length; ++i)
        {
            if (inputString[i] == '[')
            {
                Debug.Assert(lastOpenIdx == -1);
                lastOpenIdx = i;
                continue;
            }

            if (inputString[i] == ']')
            {
                Debug.Assert(lastOpenIdx != -1);
                bracketPairs.Add(lastOpenIdx);
                bracketPairs.Add(i);
                lastOpenIdx = -1;
            }
        }
    }

}
