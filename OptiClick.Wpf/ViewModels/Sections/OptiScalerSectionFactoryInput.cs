using System.Collections.ObjectModel;
using OptiClick.Core.OptiScaler;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.ViewModels.Sections.OptiScaler;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record OptiScalerSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required string InitialOptiScalerVariantOption { get; init; }
    public required OptiScalerCommonIniSettingsDocument InitialCommonIniSettings { get; init; }
    public string InitialGpuBundleKey { get; init; } = "";
    public required IOptiScalerSectionSaveHandler SaveHandler { get; init; }
}
