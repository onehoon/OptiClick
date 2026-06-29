namespace OptiClick.Core.OptiScaler;

public sealed record OptiScalerCommonIniSettingsDraft
{
    public string Fsr411Mode { get; init; } = OptiScalerFsr411Policy.ModeAuto;
    public string ShowFpsMode { get; init; } = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    public string MenuScale { get; init; } = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    public string FpsOverlayType { get; init; } = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    public string FpsOverlayPos { get; init; } = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    public string FpsScale { get; init; } = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    public string DisableSplashMode { get; init; } = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    public string FramerateLimit { get; init; } = OptiScalerCommonIniSettingsMaterializer.AutoValue;
}
