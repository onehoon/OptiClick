using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Core.OptiScaler;

namespace OptiClick.Infrastructure.OptiScaler;

public sealed class AppUserSettingsOptiScalerPreferenceWriter : IOptiScalerVariantPreferenceWriter
{
    private readonly IAppUserSettingsStore _userSettingsStore;

    public AppUserSettingsOptiScalerPreferenceWriter(IAppUserSettingsStore userSettingsStore)
    {
        _userSettingsStore = userSettingsStore ?? throw new ArgumentNullException(nameof(userSettingsStore));
    }

    public void WriteVariantPreference(string languagePreference, string variantPreference)
    {
        var current = _userSettingsStore.Load() ?? new AppUserSettings();
        _userSettingsStore.Save(AppUserSettingsUpdatePolicy.UpdatePreferences(
            current,
            languagePreference,
            variantPreference));
    }
}
