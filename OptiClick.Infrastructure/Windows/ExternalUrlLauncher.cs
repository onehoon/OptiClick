using System.Diagnostics;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

public interface IExternalUrlLauncher
{
    bool OpenUrl(string url);
}

public sealed class ExternalUrlLauncher : IExternalUrlLauncher
{
    private readonly IAppLogger _logger;

    public ExternalUrlLauncher(IAppLogger? logger = null)
    {
        _logger = logger ?? NullAppLogger.Instance;
    }

    public bool OpenUrl(string url)
    {
        var target = (url ?? "").Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        if (target.Any(static ch => char.IsControl(ch)))
        {
            return false;
        }

        try
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning("Support", $"failed to open external url type={ex.GetType().Name}");
            return false;
        }
    }
}

