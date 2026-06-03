namespace OptiClick.Wpf.Shell.Runtime.DeviceIdentity;

public sealed class RemoteDeviceIdentityRulesLoadResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static RemoteDeviceIdentityRulesLoadResult Success()
    {
        return new RemoteDeviceIdentityRulesLoadResult
        {
            IsSuccess = true,
            IsSkipped = false,
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteDeviceIdentityRulesLoadResult Skipped()
    {
        return new RemoteDeviceIdentityRulesLoadResult
        {
            IsSuccess = false,
            IsSkipped = true,
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static RemoteDeviceIdentityRulesLoadResult Failure(string errorCode, string? errorMessage = null)
    {
        return new RemoteDeviceIdentityRulesLoadResult
        {
            IsSuccess = false,
            IsSkipped = false,
            ErrorCode = errorCode ?? "load_failed",
            ErrorMessage = errorMessage ?? errorCode ?? "load_failed"
        };
    }
}
