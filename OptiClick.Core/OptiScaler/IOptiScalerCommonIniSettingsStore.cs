namespace OptiClick.Core.OptiScaler;

public interface IOptiScalerCommonIniSettingsStore
{
    OptiScalerCommonIniSettingsDocument Load();
    OptiScalerSettingsPersistenceResult Save(OptiScalerCommonIniSettingsDocument settings);
}
