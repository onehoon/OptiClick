using System.Collections.ObjectModel;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.OptiScaler;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record OptiScalerSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required string InitialOptiScalerVariantOption { get; init; }
    public required OptiScalerCommonIniSettingsDocument InitialCommonIniSettings { get; init; }
    public required Action<string, OptiScalerCommonIniSettingsDocument> SaveSettings { get; init; }
}
