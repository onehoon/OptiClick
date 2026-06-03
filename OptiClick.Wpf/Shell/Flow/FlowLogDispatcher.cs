using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Flow;

public sealed class FlowLogDispatcher
{
    private readonly IAppLogger _logger;

    public FlowLogDispatcher(IAppLogger? logger)
    {
        _logger = logger ?? NullAppLogger.Instance;
    }

    public void Dispatch(IEnumerable<IFlowLogEntry>? logs, string fallbackCategory)
    {
        if (logs is null)
        {
            return;
        }

        foreach (var log in logs)
        {
            Dispatch(log, fallbackCategory);
        }
    }

    public void Dispatch(IFlowLogEntry? log, string fallbackCategory)
    {
        if (log is null)
        {
            return;
        }

        var category = string.IsNullOrWhiteSpace(log.Category) ? fallbackCategory : log.Category;
        var message = log.Message ?? "";
        var level = (log.Level ?? "").Trim();

        if (string.Equals(level, "error", StringComparison.OrdinalIgnoreCase))
        {
            if (log.Exception is null)
            {
                _logger.Error(category, message);
            }
            else
            {
                _logger.Error(category, message, log.Exception);
            }

            return;
        }

        if (string.Equals(level, "warning", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning(category, message);
            return;
        }

        _logger.Info(category, message);
    }
}
