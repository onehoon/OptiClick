using OptiClick.Core.OptiScaler;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels.Sections.OptiScaler;

public sealed class OptiScalerSettingOptionProvider
{
    public static OptiScalerSettingOptionProvider Instance { get; } = new();

    public OptiScalerSettingOptionSet Create(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        return new OptiScalerSettingOptionSet(
            FpsDisplayOptions:
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, strings.OptiScalerOff),
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMapper.TrueValue, strings.OptiScalerOn)
            ],
            SplashOptions:
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, strings.OptiScalerSplashShow),
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMapper.TrueValue, strings.OptiScalerSplashHide)
            ],
            FpsOverlayTypeOptions:
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, strings.OptiScalerOverlayTypeJustFps),
                new OptiScalerSettingOption("1", strings.OptiScalerOverlayTypeSimple),
                new OptiScalerSettingOption("2", strings.OptiScalerOverlayTypeDetailed),
                new OptiScalerSettingOption("3", strings.OptiScalerOverlayTypeDetailedGraph),
                new OptiScalerSettingOption("4", strings.OptiScalerOverlayTypeFull),
                new OptiScalerSettingOption("5", strings.OptiScalerOverlayTypeFullGraph),
                new OptiScalerSettingOption("6", strings.OptiScalerOverlayTypeReflex)
            ],
            FpsOverlayPositionOptions:
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, strings.OptiScalerTopLeft),
                new OptiScalerSettingOption("1", strings.OptiScalerTopRight),
                new OptiScalerSettingOption("2", strings.OptiScalerBottomLeft),
                new OptiScalerSettingOption("3", strings.OptiScalerBottomRight)
            ],
            MenuScaleOptions:
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, strings.OptiScalerAuto),
                new OptiScalerSettingOption("0.9", "0.9"),
                new OptiScalerSettingOption("1.0", "1.0"),
                new OptiScalerSettingOption("1.1", "1.1"),
                new OptiScalerSettingOption("1.2", "1.2"),
                new OptiScalerSettingOption("1.3", "1.3"),
                new OptiScalerSettingOption("1.4", "1.4"),
                new OptiScalerSettingOption("1.5", "1.5")
            ],
            FpsScaleOptions:
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, strings.OptiScalerAuto),
                new OptiScalerSettingOption("1.0", "1.0"),
                new OptiScalerSettingOption("1.1", "1.1"),
                new OptiScalerSettingOption("1.2", "1.2"),
                new OptiScalerSettingOption("1.3", "1.3"),
                new OptiScalerSettingOption("1.4", "1.4"),
                new OptiScalerSettingOption("1.5", "1.5"),
                new OptiScalerSettingOption("1.6", "1.6"),
                new OptiScalerSettingOption("1.7", "1.7"),
                new OptiScalerSettingOption("1.8", "1.8"),
                new OptiScalerSettingOption("1.9", "1.9"),
                new OptiScalerSettingOption("2.0", "2.0")
            ],
            FramerateLimitOptions:
            [
                new OptiScalerSettingOption(
                    OptiScalerCommonIniSettingsMaterializer.AutoValue,
                    strings.OptiScalerUnlimited),
                new OptiScalerSettingOption(
                    OptiScalerCommonIniSettingsMapper.FramerateLimit120Hz,
                    OptiScalerCommonIniSettingsMapper.FramerateLimit120Hz),
                new OptiScalerSettingOption(
                    OptiScalerCommonIniSettingsMapper.FramerateLimit144Hz,
                    OptiScalerCommonIniSettingsMapper.FramerateLimit144Hz),
                new OptiScalerSettingOption(
                    OptiScalerCommonIniSettingsMapper.FramerateLimit165Hz,
                    OptiScalerCommonIniSettingsMapper.FramerateLimit165Hz)
            ]);
    }
}

public sealed record OptiScalerSettingOptionSet(
    IReadOnlyList<OptiScalerSettingOption> FpsDisplayOptions,
    IReadOnlyList<OptiScalerSettingOption> SplashOptions,
    IReadOnlyList<OptiScalerSettingOption> FpsOverlayTypeOptions,
    IReadOnlyList<OptiScalerSettingOption> FpsOverlayPositionOptions,
    IReadOnlyList<OptiScalerSettingOption> MenuScaleOptions,
    IReadOnlyList<OptiScalerSettingOption> FpsScaleOptions,
    IReadOnlyList<OptiScalerSettingOption> FramerateLimitOptions);
