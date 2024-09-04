namespace FanScript.Cli
{
    internal enum ErrorCode
    {
        None,
        UnknownError,
        CompilationErrors = 10,
        FileNotFound = 20,
        InvalidBuildPos,
    }
}
