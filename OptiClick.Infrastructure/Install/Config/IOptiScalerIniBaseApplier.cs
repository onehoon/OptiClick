namespace OptiClick.Infrastructure.Install.Config;

public interface IOptiScalerIniBaseApplier
{
    ConfigProfileApplySummary ApplyBase(
        string targetFolder,
        string fileName,
        IReadOnlyDictionary<string, string> settings,
        string profileName = "optiscaler_ini_base");
}
