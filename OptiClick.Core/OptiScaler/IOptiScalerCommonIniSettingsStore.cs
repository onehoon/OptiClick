namespace OptiClick.Core.OptiScaler;

public interface IOptiScalerCommonIniSettingsStore
{
    OptiScalerCommonIniSettingsDocument Load();
    void Save(OptiScalerCommonIniSettingsDocument settings);
}
