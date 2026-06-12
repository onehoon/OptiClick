using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class DeviceIdentityRulesFlowController
{
    private readonly IRemoteDeviceIdentityRulesLoader? _loader;

    public DeviceIdentityRulesFlowController(IRemoteDeviceIdentityRulesLoader? loader)
    {
        _loader = loader;
    }

    public async Task<DeviceIdentityRulesFlowResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_loader is null)
        {
            return new DeviceIdentityRulesFlowResult
            {
                DidRun = false,
                IsSuccess = false,
                ErrorCode = ""
            };
        }

        var logs = new List<RuntimeFlowLogEntry>();
        try
        {
            var loadResult = await _loader.LoadAsync(cancellationToken);
            if (!loadResult.IsSuccess)
            {
                var code = NormalizeStatusCode(loadResult.ErrorCode, "unknown");
                logs.Add(Warning("device-rules", $"device identity rules refresh skipped_or_failed code={code}"));
                return new DeviceIdentityRulesFlowResult
                {
                    DidRun = true,
                    IsSuccess = false,
                    ErrorCode = code,
                    Logs = logs
                };
            }

            logs.Add(Info("device-rules", "device identity rules refresh completed"));
            return new DeviceIdentityRulesFlowResult
            {
                DidRun = true,
                IsSuccess = true,
                ErrorCode = "",
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            logs.Add(Error("device-rules", "device identity rules refresh failed", ex));
            return new DeviceIdentityRulesFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = "unknown",
                Logs = logs
            };
        }
    }

    // Cache-only startup path: apply available local identity rules quickly and proceed.
    // A missing/invalid cache is not considered fatal because this dataset is optional UI data.
    public DeviceIdentityRulesFlowResult ApplyLocalCache()
    {
        if (_loader is null)
        {
            return new DeviceIdentityRulesFlowResult
            {
                DidRun = false,
                IsSuccess = false,
                ErrorCode = "loader_missing",
                Logs = []
            };
        }

        var logs = new List<RuntimeFlowLogEntry>();
        try
        {
            var cacheApplied = _loader.TryApplyLocalCache();
            if (!cacheApplied)
            {
                return new DeviceIdentityRulesFlowResult
                {
                    DidRun = true,
                    IsSuccess = false,
                    ErrorCode = "device_identity_rules_cache_not_available",
                    Logs = logs
                };
            }

            logs.Add(Info("device-rules", "device identity rules cache applied"));
            return new DeviceIdentityRulesFlowResult
            {
                DidRun = true,
                IsSuccess = true,
                ErrorCode = "",
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            logs.Add(Error("device-rules", "device identity rules cache apply failed", ex));
            return new DeviceIdentityRulesFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = "unknown",
                Logs = logs
            };
        }
    }

    private static RuntimeFlowLogEntry Info(string category, string message)
    {
        return new RuntimeFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static RuntimeFlowLogEntry Warning(string category, string message)
    {
        return new RuntimeFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message
        };
    }

    private static RuntimeFlowLogEntry Error(string category, string message, Exception exception)
    {
        return new RuntimeFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
