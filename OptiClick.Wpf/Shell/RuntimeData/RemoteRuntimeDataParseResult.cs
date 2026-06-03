namespace OptiClick.Wpf.Shell.RuntimeData;

public sealed class RemoteRuntimeDataParseResult
{
    public bool IsSuccess { get; init; }
    public RemoteRuntimeData RuntimeData { get; init; } = RemoteRuntimeData.Empty;
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static RemoteRuntimeDataParseResult Success(RemoteRuntimeData runtimeData)
    {
        return new RemoteRuntimeDataParseResult
        {
            IsSuccess = true,
            RuntimeData = runtimeData ?? RemoteRuntimeData.Empty,
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteRuntimeDataParseResult Failure(string errorCode, string? errorMessage = null)
    {
        return new RemoteRuntimeDataParseResult
        {
            IsSuccess = false,
            RuntimeData = RemoteRuntimeData.Empty,
            ErrorCode = errorCode ?? "parse_failed",
            ErrorMessage = errorMessage ?? errorCode ?? "parse_failed"
        };
    }
}
