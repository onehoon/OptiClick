using System;
using System.IO;
using Velopack;

namespace OptiClickShell;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => DeleteLegacyDataFolder())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    // Pre-Velopack single-EXE builds stored data under %LocalAppData%\OptiClick, which is
    // outside Velopack's own install root (%LocalAppData%\{packId}) and never removed by it.
    private static void DeleteLegacyDataFolder()
    {
        var legacyDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OptiClick");

        if (!Directory.Exists(legacyDir))
            return;

        try
        {
            ClearReadOnlyRecursive(new DirectoryInfo(legacyDir));
            Directory.Delete(legacyDir, recursive: true);
        }
        catch
        {
            // Uninstall hook has a hard timeout; swallow failures rather than block removal.
        }
    }

    private static void ClearReadOnlyRecursive(DirectoryInfo dir)
    {
        dir.Attributes &= ~FileAttributes.ReadOnly;

        foreach (var file in dir.GetFiles())
            file.Attributes &= ~FileAttributes.ReadOnly;

        foreach (var subDir in dir.GetDirectories())
            ClearReadOnlyRecursive(subDir);
    }
}
