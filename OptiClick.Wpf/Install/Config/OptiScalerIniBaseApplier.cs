using OptiClick.Core.Install;

namespace OptiClick.Wpf.Install.Config;

public sealed class OptiScalerIniBaseApplier : IOptiScalerIniBaseApplier
{
    private readonly IniProfileEditor _iniProfileEditor;

    public OptiScalerIniBaseApplier(IniProfileEditor iniProfileEditor)
    {
        _iniProfileEditor = iniProfileEditor ?? throw new ArgumentNullException(nameof(iniProfileEditor));
    }

    public ConfigProfileApplySummary ApplyBase(
        string targetFolder,
        string fileName,
        IReadOnlyDictionary<string, string> settings,
        string profileName = "optiscaler_ini_base")
    {
        return _iniProfileEditor.ApplyOptiScalerIniBase(targetFolder, fileName, settings, profileName);
    }
}
