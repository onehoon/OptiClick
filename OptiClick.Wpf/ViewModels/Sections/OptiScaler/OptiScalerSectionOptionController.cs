using System.Collections.ObjectModel;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels.Sections.OptiScaler;

internal sealed class OptiScalerSectionOptionController
{
    private readonly OptiScalerSettingOptionProvider _settingOptionProvider;

    public OptiScalerSectionOptionController(OptiScalerSettingOptionProvider settingOptionProvider)
    {
        _settingOptionProvider = settingOptionProvider
                                 ?? throw new ArgumentNullException(nameof(settingOptionProvider));
    }

    public bool ApplyVariantOptions(
        IList<OptiScalerVariantSelectionOption> current,
        IEnumerable<OptiScalerVariantSelectionOption> options)
    {
        ArgumentNullException.ThrowIfNull(current);

        var next = (options ?? []).ToArray();
        if (AreSameOptiScalerVariantOptions(current, next))
        {
            return false;
        }

        current.Clear();
        foreach (var option in next)
        {
            current.Add(option);
        }

        return true;
    }

    public void RefreshOptionText(
        AppStrings strings,
        ObservableCollection<OptiScalerSettingOption> fpsDisplayOptions,
        ObservableCollection<OptiScalerSettingOption> splashOptions,
        ObservableCollection<OptiScalerSettingOption> fpsOverlayTypeOptions,
        ObservableCollection<OptiScalerSettingOption> fpsOverlayPositionOptions,
        ObservableCollection<OptiScalerSettingOption> menuScaleOptions,
        ObservableCollection<OptiScalerSettingOption> fpsScaleOptions,
        ObservableCollection<OptiScalerSettingOption> framerateLimitOptions)
    {
        var optionSet = _settingOptionProvider.Create(strings);
        ReplaceOptions(fpsDisplayOptions, optionSet.FpsDisplayOptions);
        ReplaceOptions(splashOptions, optionSet.SplashOptions);
        ReplaceOptions(fpsOverlayTypeOptions, optionSet.FpsOverlayTypeOptions);
        ReplaceOptions(fpsOverlayPositionOptions, optionSet.FpsOverlayPositionOptions);
        ReplaceOptions(menuScaleOptions, optionSet.MenuScaleOptions);
        ReplaceOptions(fpsScaleOptions, optionSet.FpsScaleOptions);
        ReplaceOptions(framerateLimitOptions, optionSet.FramerateLimitOptions);
    }

    private static void ReplaceOptions(
        ObservableCollection<OptiScalerSettingOption> target,
        IReadOnlyList<OptiScalerSettingOption> options)
    {
        target.Clear();
        foreach (var option in options)
        {
            target.Add(option);
        }
    }

    private static bool AreSameOptiScalerVariantOptions(
        IList<OptiScalerVariantSelectionOption> current,
        IReadOnlyList<OptiScalerVariantSelectionOption> next)
    {
        if (current.Count != next.Count)
        {
            return false;
        }

        for (var index = 0; index < current.Count; index++)
        {
            var left = current[index];
            var right = next[index];
            if (!string.Equals(left.Variant, right.Variant, StringComparison.Ordinal)
                || !string.Equals(left.DisplayLabel, right.DisplayLabel, StringComparison.Ordinal)
                || !string.Equals(left.Version, right.Version, StringComparison.Ordinal)
                || !string.Equals(left.DisplayVersion, right.DisplayVersion, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
