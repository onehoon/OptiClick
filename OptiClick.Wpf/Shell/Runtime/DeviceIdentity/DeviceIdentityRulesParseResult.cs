using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Runtime.DeviceIdentity;

public sealed class DeviceIdentityRulesParseResult
{
    public bool IsSuccess { get; init; }
    public DeviceIdentityRules Rules { get; init; } = DeviceIdentityRules.Empty;
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static DeviceIdentityRulesParseResult Success(DeviceIdentityRules rules)
    {
        return new DeviceIdentityRulesParseResult
        {
            IsSuccess = true,
            Rules = rules ?? DeviceIdentityRules.Empty,
            ErrorCode = "",
            ErrorMessage = ""
        };
    }

    public static DeviceIdentityRulesParseResult Failure(string errorCode, string? errorMessage = null)
    {
        return new DeviceIdentityRulesParseResult
        {
            IsSuccess = false,
            Rules = DeviceIdentityRules.Empty,
            ErrorCode = errorCode ?? "parse_failed",
            ErrorMessage = errorMessage ?? errorCode ?? "parse_failed"
        };
    }
}
