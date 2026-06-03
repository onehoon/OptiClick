using System.Globalization;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Localization;

public sealed class LocalizationStateController
{
    public LocalizationStateUpdate BuildInitialState(
        AppLanguage language,
        AppStrings strings,
        string currentSettingsStatusText,
        string currentScanStatusText,
        string currentDeviceText,
        string currentGpuText)
    {
        ArgumentNullException.ThrowIfNull(strings);

        return new LocalizationStateUpdate
        {
            SelectedLanguageDisplayName = GetLanguageDisplayName(language, strings),
            SettingsStatusText = string.IsNullOrWhiteSpace(currentSettingsStatusText)
                ? strings.RuntimeLoadingRemoteCatalog
                : "",
            ScanStatusText = string.IsNullOrWhiteSpace(currentScanStatusText)
                ? strings.RuntimeGameListLoading
                : "",
            DeviceText = string.IsNullOrWhiteSpace(currentDeviceText)
                ? strings.RuntimeUnknownDevice
                : "",
            GpuText = string.IsNullOrWhiteSpace(currentGpuText)
                ? strings.RuntimeDetectingGpu
                : "",
            ShouldRelocalizeScanFolders = true
        };
    }

    public LocalizationStateUpdate BuildRefreshState(AppLanguage language, AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        var displayName = GetLanguageDisplayName(language, strings);
        return new LocalizationStateUpdate
        {
            SelectedLanguageDisplayName = displayName,
            SettingsStatusText = string.Format(
                CultureInfo.CurrentCulture,
                strings.SettingsLanguageChangedTo ?? "",
                displayName),
            ShouldRelocalizeScanFolders = true
        };
    }

    public string GetLanguageDisplayName(AppLanguage language, AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return language == AppLanguage.Korean ? strings.LanguageKorean : strings.LanguageEnglish;
    }
}
