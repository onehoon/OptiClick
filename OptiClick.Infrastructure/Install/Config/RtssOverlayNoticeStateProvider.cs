using System.IO;
using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Config;

public sealed class RtssOverlayNoticeStateProvider : IRtssOverlayNoticeStateProvider
{
    public bool IsNoticeRequired()
    {
        var installPath = RtssInstallPathResolver.Resolve();
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }

        var rtssExePath = Path.Combine(installPath, "RTSS.exe");
        if (!File.Exists(rtssExePath))
        {
            return false;
        }

        var globalProfilePath = Path.Combine(installPath, "Profiles", "Global");
        return File.Exists(globalProfilePath);
    }
}
