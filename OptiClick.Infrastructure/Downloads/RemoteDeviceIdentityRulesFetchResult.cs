namespace OptiClick.Infrastructure.Downloads;

public sealed class RemoteDeviceIdentityRulesFetchResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string Content { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static RemoteDeviceIdentityRulesFetchResult Success(string content)
    {
        return new RemoteDeviceIdentityRulesFetchResult
        {
            IsSuccess = true,
            IsSkipped = false,
            Content = content ?? "",
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteDeviceIdentityRulesFetchResult Skipped()
    {
        return new RemoteDeviceIdentityRulesFetchResult
        {
            IsSuccess = false,
            IsSkipped = true,
            Content = "",
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteDeviceIdentityRulesFetchResult Failure(string errorCode, string? errorMessage = null)
    {
        return new RemoteDeviceIdentityRulesFetchResult
        {
            IsSuccess = false,
            IsSkipped = false,
            Content = "",
            ErrorCode = errorCode ?? "fetch_failed",
            ErrorMessage = errorMessage ?? errorCode ?? "fetch_failed"
        };
    }
}
