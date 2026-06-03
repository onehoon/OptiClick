using System.Diagnostics;
using System.IO;

namespace OptiClick.Wpf.Shell.Support;

public sealed class WindowsLogFolderLauncher : ILogFolderLauncher
{
    public LogFolderLaunchResult Open(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });

            return new LogFolderLaunchResult
            {
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new LogFolderLaunchResult
            {
                IsSuccess = false,
                ErrorType = ex.GetType().Name
            };
        }
    }
}
