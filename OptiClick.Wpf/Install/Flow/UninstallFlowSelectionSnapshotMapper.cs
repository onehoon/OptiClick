using System.IO;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.Install.Flow;

public static class UninstallFlowSelectionSnapshotMapper
{
    public static UninstallFlowSelectionSnapshot FromSelectionState(ShellInstallSelectionState? selectionState)
    {
        selectionState ??= new ShellInstallSelectionState();
        var precheck = selectionState.PrecheckSnapshot;
        return new UninstallFlowSelectionSnapshot
        {
            FinalProxyDllName = PickFileName(
                selectionState.SelectedInstallStatus?.DetectedFile,
                selectionState.PrecheckResolvedDllName,
                precheck.ResolvedDllName),
            Precheck = precheck
        };
    }

    private static string PickFileName(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var fileName = Path.GetFileName((candidate ?? "").Trim());
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
        }

        return "";
    }
}
