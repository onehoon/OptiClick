namespace OptiClick.Infrastructure.Downloads;

public sealed class RemoteRuntimeDataFetchResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string Content { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static RemoteRuntimeDataFetchResult Success(string content)
    {
        return new RemoteRuntimeDataFetchResult
        {
            IsSuccess = true,
            IsSkipped = false,
            Content = content ?? "",
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteRuntimeDataFetchResult Skipped()
    {
        return new RemoteRuntimeDataFetchResult
        {
            IsSuccess = false,
            IsSkipped = true,
            Content = "",
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteRuntimeDataFetchResult Failure(string errorCode, string? errorMessage = null)
    {
        return new RemoteRuntimeDataFetchResult
        {
            IsSuccess = false,
            IsSkipped = false,
            Content = "",
            ErrorCode = errorCode ?? "fetch_failed",
            ErrorMessage = errorMessage ?? errorCode ?? "fetch_failed"
        };
    }
}
