using System.IO;
using OptiClick.Wpf.Install.Precheck;
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
            Precheck = precheck,
            UalDetectedNames = ResolveUalDetectedNames(precheck)
        };
    }

    private static IReadOnlyList<string> ResolveUalDetectedNames(InstallPrecheckSnapshot precheck)
    {
        if (precheck.Findings.Count == 0)
        {
            return Array.Empty<string>();
        }

        var names = new List<string>();
        foreach (var finding in precheck.Findings)
        {
            if (!string.Equals((finding.Kind ?? "").Trim(), ModConflictKinds.UltimateAsiLoader, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var evidence in finding.Evidence)
            {
                var fileName = PickFileName(evidence);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    names.Add(fileName);
                }
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
