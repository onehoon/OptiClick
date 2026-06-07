using OptiClick.Wpf.ViewModels.Sections.Home;
using OptiClick.Wpf.ViewModels.Sections.OptiScaler;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Sections.Settings;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record ShellSections(
    HomeSectionViewModel Home,
    ScanSectionViewModel Scan,
    SupportedGamesSectionViewModel SupportedGames,
    OptiScalerSectionViewModel OptiScaler,
    SettingsSectionViewModel Settings);
