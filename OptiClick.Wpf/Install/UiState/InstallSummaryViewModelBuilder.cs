using OptiClick.Core.Install;
using OptiClick.Core.Install.Summary;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Install.UiState;

public sealed record InstallSummaryBuildInput
{
    public InstallGameDescriptor? GameDescriptor { get; init; }
    public InstallStatusSnapshot InstallStatus { get; init; } = new();
    public string InstallSummaryNote { get; init; } = "";
    public string Language { get; init; } = "en";
}

public sealed record InstallSummaryPresentation
{
    public string OptiScalerText { get; init; } = "";
    public string ComponentsText { get; init; } = "";
    public string NoteText { get; init; } = "";
}

public interface IInstallSummaryStringsResolver
{
    InstallSummaryStrings Resolve(string language);
}

public sealed class DefaultInstallSummaryStringsResolver : IInstallSummaryStringsResolver
{
    private readonly IAppStringsProvider _stringsProvider;

    public DefaultInstallSummaryStringsResolver(IAppStringsProvider stringsProvider)
    {
        ArgumentNullException.ThrowIfNull(stringsProvider);
        _stringsProvider = stringsProvider;
    }

    public InstallSummaryStrings Resolve(string language)
    {
        var appLanguage = (language ?? "").Trim().StartsWith("ko", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Korean
            : AppLanguage.English;
        var strings = _stringsProvider.Get(appLanguage);
        return new InstallSummaryStrings
        {
            ActionInstall = strings.InstallSummaryActionInstall,
            ActionUpdate = strings.InstallSummaryActionUpdate,
            ActionReinstall = strings.InstallSummaryActionReinstall,
            AutoConfigApplied = strings.InstallSummaryAutoConfigApplied,
            ComponentOptiPatcher = strings.InstallSummaryComponentOptiPatcher,
            ComponentUnreal5 = strings.InstallSummaryComponentUnreal5,
            ComponentReframework = strings.InstallSummaryComponentReFramework,
            ComponentUltimateAsiLoader = strings.InstallSummaryComponentUltimateAsiLoader,
            ComponentSpecialK = strings.InstallSummaryComponentSpecialK,
            ComponentRtssOverlay = strings.InstallSummaryComponentRtssOverlay
        };
    }
}

public interface IInstallSummaryViewModelBuilder
{
    InstallSummaryPresentation Build(InstallSummaryBuildInput input);
}

public sealed class InstallSummaryViewModelBuilder : IInstallSummaryViewModelBuilder
{
    private readonly IInstallSummaryStringsResolver _stringsResolver;

    public InstallSummaryViewModelBuilder(IInstallSummaryStringsResolver stringsResolver)
    {
        _stringsResolver = stringsResolver;
    }

    public InstallSummaryPresentation Build(InstallSummaryBuildInput input)
    {
        var descriptor = input.GameDescriptor ?? InstallGameDescriptor.Empty;
        var summaryInput = new InstallSummaryInput
        {
            InstallStatusCode = input.InstallStatus.Code,
            InstalledVersion = input.InstallStatus.InstalledVersion,
            CurrentVersion = input.InstallStatus.CurrentVersion,
            CurrentDisplayVersion = input.InstallStatus.CurrentDisplayVersion,
            OptiPatcher = descriptor.RequiresOptiPatcher,
            Unreal5 = descriptor.RequiresUnreal5,
            ReframeworkUrl = descriptor.ReFrameworkUrl,
            UltimateAsiLoader = descriptor.RequiresUltimateAsiLoader,
            SpecialK = descriptor.SpecialK,
            RtssOverlay = descriptor.RequiresRtssProfile,
            InstallSummaryNote = input.InstallSummaryNote
        };

        var summary = InstallSummaryBuilder.Build(summaryInput, _stringsResolver.Resolve(input.Language));
        return new InstallSummaryPresentation
        {
            OptiScalerText = summary.OptiScalerText,
            ComponentsText = summary.ComponentsText,
            NoteText = summary.NoteText
        };
    }
}
