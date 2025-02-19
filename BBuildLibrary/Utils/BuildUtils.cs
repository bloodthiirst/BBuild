using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bloodthirst.BBuild;

public sealed class BuildUtils
{
    /// <summary>
    /// Fixed name of the json file containing the build settings of a project
    /// </summary>
    public const string BuildFilename = "Build.json";

    public static bool IsFileName(ReadOnlySpan<char> line)
    {
        int i = 0;

        bool foundDot = false;
        // read name
        while(i < line.Length)
        {
            char curr = line[i];
            i++;

            if (curr == '\\' || curr == '/' || curr == ':')
            {
                return false;
            }
            if( curr == '.')
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

            if(!char.IsLetter(curr))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Normalizes the input path an turns it into an absolute path , also fills in the custom variables used in the path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static string EnsurePathIsAbsolute(string path , BuildSettings settings)
    {
        path = ResolveStringWithVariables(path , settings);

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path, settings.AbsolutePath).Replace("\\", "/");
        }

        return path;
    }
    /// <summary>
    /// Get the settings and dependencies of the project located at <paramref name="absoluteProjectPath"/>
    /// </summary>
    /// <param name="absoluteProjectPath">Absolute path for the project folder</param>
    /// <param name="settings">Settings of the project located at <paramref name="absoluteProjectPath"/> , is set to null if the method fails</param>
    /// <param name="dependencies">Dependencies of the project located at <paramref name="absoluteProjectPath"/>, is set to null if the method fails</param>
    /// <returns>
    /// <para>Returns whether the method succeeded at getting the project information or not</para> 
    /// <para>Returns <see cref="true"/> if it's successfull and fills <paramref name="dependencies"/> and <paramref name="settings"/></para> 
    /// <para>Returns <see cref="false"/> if it fails and sets <paramref name="dependencies"/> and <paramref name="settings"/> to <see cref="null"/></para> 
    /// </returns>
    public static BuildSettings? GetFromPath(string absoluteProjectPath)
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

    public static string ResolveStringWithVariables(string inputString , BuildSettings settings)
    {
        StringBuilder str = new StringBuilder(inputString);

        List<int> bracketPairs = new List<int>();
        FindBracketPairs(str, bracketPairs);

        while(bracketPairs.Count != 0)
        {
            Debug.Assert(bracketPairs.Count % 2 == 0);

            for (int i = bracketPairs.Count - 1; i >= 0; i -= 2)
            {
                int open = bracketPairs[i - 1];
                int close = bracketPairs[i];

                string key = str.ToString(open + 1 , close - open - 1);
                if(!settings.CustomVariables.TryGetValue(key, out string? value))
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

    private static void FindBracketPairs(StringBuilder inputString , List<int> bracketPairs)
    {
        int lastOpenIdx = -1;

        for(int i = 0; i < inputString.Length; ++i)
        {
            if(inputString[i] == '[')
            {
                Debug.Assert(lastOpenIdx == -1);
                lastOpenIdx = i;
                continue;
            }

            if(inputString[i] == ']')
            {
                Debug.Assert(lastOpenIdx != -1);
                bracketPairs.Add(lastOpenIdx);
                bracketPairs.Add(i);
                lastOpenIdx = -1;
            }
        }
    }

}
