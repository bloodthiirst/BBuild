namespace Bloodthirst.BBuild;

public sealed class BuildCache
{
    /// <summary>
    /// Lookup table to the write timestap per file (using their absolute path) since the last build
    /// </summary>
    public Dictionary<string, long> FileLastWriteLookup { get; set; } = [];
}
