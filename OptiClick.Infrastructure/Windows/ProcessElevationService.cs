using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

public interface IProcessElevationService
{
    bool IsCurrentProcessElevated();
    bool TryRelaunchAsAdministrator(string[] args);
}

public sealed class ProcessElevationService : IProcessElevationService
{
    public const string ElevatedRelaunchArgument = "--elevated-relaunch";

    private readonly IAppLogger _logger;
    private readonly Func<string?> _processPathProvider;
    private readonly Func<bool>? _isElevatedOverride;
    private readonly Func<ProcessStartInfo, bool> _processStarter;

    public ProcessElevationService(
        IAppLogger? logger = null,
        Func<string?>? processPathProvider = null,
        Func<bool>? isElevatedOverride = null,
        Func<ProcessStartInfo, bool>? processStarter = null)
    {
        _logger = logger ?? NullAppLogger.Instance;
        _processPathProvider = processPathProvider ?? ResolveCurrentProcessPath;
        _isElevatedOverride = isElevatedOverride;
        _processStarter = processStarter ?? StartProcess;
    }

    public bool IsCurrentProcessElevated()
    {
        if (_isElevatedOverride is not null)
        {
            return _isElevatedOverride();
        }

        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex)
        {
            _logger.Warning("Elevation", $"elevation state read failed type={ex.GetType().Name}");
            return false;
        }
    }

    public bool TryRelaunchAsAdministrator(string[] args)
    {
        if (IsCurrentProcessElevated())
        {
            return false;
        }

        var safeArgs = args ?? [];
        if (safeArgs.Any(arg => string.Equals(arg, ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var processPath = (_processPathProvider() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(processPath))
        {
            _logger.Warning("Elevation", "elevated relaunch skipped reason=process_path_missing");
            return false;
        }

        try
        {
            var info = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = BuildElevatedArguments(safeArgs)
            };
            return _processStarter(info);
        }
        catch (Win32Exception ex)
        {
            _logger.Warning("Elevation", $"elevated relaunch canceled_or_failed code={ex.NativeErrorCode}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Warning("Elevation", $"elevated relaunch failed type={ex.GetType().Name}");
            return false;
        }
    }

    private static string ResolveCurrentProcessPath()
    {
        var fromEnvironment = (Environment.ProcessPath ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        try
        {
            return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static bool StartProcess(ProcessStartInfo info)
    {
        var process = System.Diagnostics.Process.Start(info);
        return process is not null;
    }

    private static string BuildElevatedArguments(string[] args)
    {
        var filtered = args
            .Where(arg => !string.Equals(arg, ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase))
            .Append(ElevatedRelaunchArgument);

        return string.Join(" ", filtered.Select(QuoteArgument));
    }

    private static string QuoteArgument(string value)
    {
        var text = value ?? "";
        if (text.Length == 0)
        {
            return "\"\"";
        }

        var needsQuoting = text.Any(char.IsWhiteSpace) || text.Contains('"');
        if (!needsQuoting)
        {
            return text;
        }

        var builder = new StringBuilder();
        builder.Append('"');

        var backslashCount = 0;
        foreach (var current in text)
        {
            if (current == '\\')
            {
                backslashCount++;
                continue;
            }

            if (current == '"')
            {
                builder.Append('\\', backslashCount * 2 + 1);
                builder.Append('"');
                backslashCount = 0;
                continue;
            }

            if (backslashCount > 0)
            {
                builder.Append('\\', backslashCount);
                backslashCount = 0;
            }

            builder.Append(current);
        }

        if (backslashCount > 0)
        {
            builder.Append('\\', backslashCount * 2);
        }

        builder.Append('"');
        return builder.ToString();
    }
}

