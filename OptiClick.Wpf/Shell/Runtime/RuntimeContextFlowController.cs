using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeContextFlowController
{
    private readonly IRuntimeContextProvider _runtimeContextProvider;

    public RuntimeContextFlowController(IRuntimeContextProvider runtimeContextProvider)
    {
        _runtimeContextProvider = runtimeContextProvider
            ?? throw new ArgumentNullException(nameof(runtimeContextProvider));
    }

    public async Task<RuntimeContextFlowResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var logs = new List<RuntimeFlowLogEntry>();

        try
        {
            var context = await Task.Run(
                () => _runtimeContextProvider.GetRuntimeContext(),
                cancellationToken);
            logs.Add(Info("runtime", $"runtime context refreshed gpu_count={context.Gpus.Count}"));
            return new RuntimeContextFlowResult
            {
                IsSuccess = true,
                Context = context,
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            logs.Add(Error("runtime", "runtime context refresh failed", ex));
            return new RuntimeContextFlowResult
            {
                IsSuccess = false,
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
}
