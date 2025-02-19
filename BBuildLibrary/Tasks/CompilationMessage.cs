namespace Bloodthirst.BBuild;

public enum MessageType
{
    Warning,
    Error
}

public struct MessagePerFile
{
    public string Filepath { get; set; }
    public int LineNumber { get; set; }
    public string Text { get; set; }
    public MessageType Type { get; set; }
}

public struct CompilationMessage
{
    public string Filename { get; set; }
    public MessagePerFile[] Messages { get; set; }
}

public struct LinkingMessage
{
    public string Filename { get; set; }
    public string ErrorCode { get; set; }
    public string Text { get; set; }
}
