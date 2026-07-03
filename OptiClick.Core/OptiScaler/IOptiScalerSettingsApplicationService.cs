using System.Collections.Generic;

namespace OptiClick.Core.OptiScaler;

public interface IOptiScalerSettingsApplicationService
{
    OptiScalerCommonIniSettingsDocument LoadCommonIniSettings();

    OptiScalerSettingsPersistenceResult SaveCommonIniSettings(OptiScalerCommonIniSettingsDocument settings);

    OptiScalerSettingsApplyResult ApplySettings(OptiScalerSettingsApplyRequest request);

    OptiScalerIniApplyContext CreateIniApplyContext(
        IReadOnlyDictionary<string, string>? gameOptiScalerIniSettings = null);
}
