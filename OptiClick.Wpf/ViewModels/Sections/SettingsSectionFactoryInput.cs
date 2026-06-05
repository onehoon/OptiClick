using System.Collections.ObjectModel;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Dialogs;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record SettingsSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required ObservableCollection<string> SettingsLanguageOptions { get; init; }
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
    public required string InitialOptiScalerVariantOption { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
    public required Action<string> ApplyOptiScalerVariantOption { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Action OpenLogFolder { get; init; }
    public required Action OpenSupportRequest { get; init; }
    public Action<Exception>? OnRefreshInstallFilesException { get; init; }
}
