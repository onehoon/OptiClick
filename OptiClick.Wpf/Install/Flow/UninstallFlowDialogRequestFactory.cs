using System.IO;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using InfrastructureUninstall = OptiClick.Infrastructure.Install.Uninstall;

namespace OptiClick.Wpf.Install.Flow;

internal sealed class UninstallFlowDialogRequestFactory
{
    public AppDialogRequest CreateNoRemovableItems(UninstallFlowText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new AppDialogRequest
        {
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info,
            Title = text.InstallManagementUninstallButton,
            Summary = text.UninstallNoRemovableItemsSummary,
            PrimaryButtonText = text.DialogButtonOk,
            PrimaryResult = AppDialogResult.Ok
        };
    }

    public AppDialogRequest CreateValidationFailed(UninstallFlowText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = text.UninstallValidationFailedTitle,
            Summary = text.UninstallValidationFailedSummary,
            PrimaryButtonText = text.DialogButtonOk,
            PrimaryResult = AppDialogResult.Ok
        };
    }

    public AppDialogRequest CreateConfirmation(
        InfrastructureUninstall.UninstallPlan plan,
        UninstallFlowText text)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(text);

        var lines = BuildUninstallCandidateLines(plan);
        var details = string.Join("\n", lines);
        var summary = string.IsNullOrWhiteSpace(details)
            ? text.UninstallConfirmationSummary
            : $"{text.UninstallConfirmationSummary}\n\n{details}";

        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = text.UninstallConfirmationTitle,
            Summary = summary,
            PrimaryButtonText = text.InstallManagementUninstallButton,
            SecondaryButtonText = text.DialogButtonCancel,
            PrimaryButtonRole = DialogButtonRole.Destructive,
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel
        };
    }

    public AppDialogRequest CreateCompletion(
        InfrastructureUninstall.UninstallExecutionResult result,
        UninstallFlowText text)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(text);

        return result.Status switch
        {
            InfrastructureUninstall.UninstallExecutionStatus.Success => new AppDialogRequest
            {
                Kind = AppDialogKind.Success,
                Severity = DialogSeverity.Success,
                Title = text.UninstallCompletedTitle,
                Summary = text.UninstallCompletedSummary,
                PrimaryButtonText = text.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok,
                PrimaryButtonRole = DialogButtonRole.Success
            },
            InfrastructureUninstall.UninstallExecutionStatus.PartialSuccess => new AppDialogRequest
            {
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                Title = text.UninstallPartialFailedTitle,
                Summary = text.UninstallPartialFailedSummary,
                PrimaryButtonText = text.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok
            },
            InfrastructureUninstall.UninstallExecutionStatus.Failed => new AppDialogRequest
            {
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                Title = text.UninstallFailedTitle,
                Summary = text.UninstallFailedSummary,
                PrimaryButtonText = text.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok
            },
            _ => CreateNoRemovableItems(text)
        };
    }

    private static IReadOnlyList<string> BuildUninstallCandidateLines(InfrastructureUninstall.UninstallPlan plan)
    {
        var lines = new List<string>();
        AppendCandidateLine(lines, plan.Candidates, plan.DirectoryCandidates, InfrastructureUninstall.UninstallCandidateKind.OptiScaler, "OptiScaler");
        AppendCandidateLine(lines, plan.Candidates, plan.DirectoryCandidates, InfrastructureUninstall.UninstallCandidateKind.ReFramework, "REFramework");
        AppendCandidateLine(lines, plan.Candidates, plan.DirectoryCandidates, InfrastructureUninstall.UninstallCandidateKind.UltimateAsiLoader, "Ultimate ASI Loader");
        AppendCandidateLine(lines, plan.Candidates, plan.DirectoryCandidates, InfrastructureUninstall.UninstallCandidateKind.SpecialK, "Special K");
        return lines;
    }

    private static void AppendCandidateLine(
        ICollection<string> target,
        IReadOnlyList<InfrastructureUninstall.UninstallCandidate> candidates,
        IReadOnlyList<InfrastructureUninstall.UninstallDirectoryCandidate> directories,
        InfrastructureUninstall.UninstallCandidateKind kind,
        string displayName)
    {
        var fileNames = candidates
            .Where(candidate => candidate.Kind == kind)
            .Select(static candidate => ResolveUninstallDisplayFileName(candidate.RelativePath, candidate.FullPath))
            .Concat(directories
                .Where(candidate => candidate.Kind == kind)
                .Select(static candidate => ResolveUninstallDisplayDirectoryName(candidate.RelativePath, candidate.FullPath)))
            .Where(static fileName => !string.IsNullOrWhiteSpace(fileName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (fileNames.Length == 0)
        {
            return;
        }

        target.Add($"{displayName}: {string.Join(", ", fileNames)}");
    }

    private static string ResolveUninstallDisplayFileName(string relativePath, string fullPath)
    {
        var fromRelative = Path.GetFileName((relativePath ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(fromRelative))
        {
            return fromRelative;
        }

        var fromFullPath = Path.GetFileName((fullPath ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(fromFullPath))
        {
            return fromFullPath;
        }

        return (relativePath ?? fullPath ?? "").Trim();
    }

    private static string ResolveUninstallDisplayDirectoryName(string relativePath, string fullPath)
    {
        var fromRelative = (relativePath ?? "").Trim().Replace('\\', '/').Trim('/');
        if (!string.IsNullOrWhiteSpace(fromRelative))
        {
            return $"{fromRelative}/";
        }

        var fromFullPath = Path.GetFileName((fullPath ?? "").Trim());
        return string.IsNullOrWhiteSpace(fromFullPath) ? "" : $"{fromFullPath}/";
    }
}
