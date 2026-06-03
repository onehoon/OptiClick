namespace OptiClick.Wpf.Shell.RuntimeData;

public sealed class RemoteRuntimeDataLoadResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public RemoteRuntimeData RuntimeData { get; init; } = RemoteRuntimeData.Empty;
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static RemoteRuntimeDataLoadResult Success(RemoteRuntimeData runtimeData)
    {
        return new RemoteRuntimeDataLoadResult
        {
            IsSuccess = true,
            IsSkipped = false,
            RuntimeData = runtimeData ?? RemoteRuntimeData.Empty,
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteRuntimeDataLoadResult Skipped()
    {
        return new RemoteRuntimeDataLoadResult
        {
            IsSuccess = false,
            IsSkipped = true,
            RuntimeData = RemoteRuntimeData.Empty,
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteRuntimeDataLoadResult Failure(string errorCode, string? errorMessage = null)
    {
        return new RemoteRuntimeDataLoadResult
        {
            IsSuccess = false,
            IsSkipped = false,
            RuntimeData = RemoteRuntimeData.Empty,
            ErrorCode = errorCode ?? "load_failed",
            ErrorMessage = errorMessage ?? errorCode ?? "load_failed"
        };
    }
}
