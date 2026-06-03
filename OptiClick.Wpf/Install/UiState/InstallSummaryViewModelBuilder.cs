using OptiClick.Core.Install.Summary;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Install.UiState;

public sealed record InstallSummaryBuildInput
{
    public ShellGameCardModel? Game { get; init; }
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

    public DefaultInstallSummaryStringsResolver()
        : this(new AppStringsProvider())
    {
    }

    public DefaultInstallSummaryStringsResolver(IAppStringsProvider stringsProvider)
    {
        _stringsProvider = stringsProvider ?? new AppStringsProvider();
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
        var game = input.Game ?? new ShellGameCardModel();
        var summaryInput = new InstallSummaryInput
        {
            InstallStatusCode = input.InstallStatus.Code,
            InstalledVersion = input.InstallStatus.InstalledVersion,
            CurrentVersion = input.InstallStatus.CurrentVersion,
            CurrentDisplayVersion = input.InstallStatus.CurrentDisplayVersion,
            OptiPatcher = ShellGameInstallMetadataResolver.GetOptiPatcher(game),
            Unreal5 = ShellGameInstallMetadataResolver.GetUnreal5(game),
            ReframeworkUrl = ShellGameInstallMetadataResolver.GetReFrameworkUrl(game),
            UltimateAsiLoader = ShellGameInstallMetadataResolver.GetUltimateAsiLoader(game),
            SpecialK = ShellGameInstallMetadataResolver.GetSpecialK(game),
            RtssOverlay = ShellGameInstallMetadataResolver.GetRtssOverlay(game),
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
