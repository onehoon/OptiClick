namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record ShellSectionsFactoryInput
{
    public required HomeSectionFactoryInput Home { get; init; }
    public required ScanSectionFactoryInput Scan { get; init; }
    public required SupportedGamesSectionFactoryInput SupportedGames { get; init; }
    public required OptiScalerSectionFactoryInput OptiScaler { get; init; }
    public required SettingsSectionFactoryInput Settings { get; init; }
}
