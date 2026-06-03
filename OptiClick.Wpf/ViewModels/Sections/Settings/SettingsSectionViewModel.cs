using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels.Sections.Settings;

public sealed class SettingsSectionViewModel : ViewModelBase
{
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly Func<bool> _isKoreanUi;
    private readonly Action<string> _applySettingsLanguageOption;
    private readonly SettingsActionCoordinator _settingsActionCoordinator;
    private readonly Func<bool> _isInstallExecutionInProgress;
    private string _selectedSettingsLanguageOption;
    private string _settingsStatusText = "";

    public SettingsSectionViewModel(SettingsSectionViewModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _isKoreanUi = options.IsKoreanUi ?? throw new ArgumentNullException(nameof(options.IsKoreanUi));
        _applySettingsLanguageOption = options.ApplySettingsLanguageOption ?? throw new ArgumentNullException(nameof(options.ApplySettingsLanguageOption));
        _settingsActionCoordinator = options.SettingsActionCoordinator ?? throw new ArgumentNullException(nameof(options.SettingsActionCoordinator));
        _isInstallExecutionInProgress = options.IsInstallExecutionInProgress ?? throw new ArgumentNullException(nameof(options.IsInstallExecutionInProgress));
        SettingsLanguageOptions = options.SettingsLanguageOptions ?? throw new ArgumentNullException(nameof(options.SettingsLanguageOptions));
        _selectedSettingsLanguageOption = options.InitialSettingsLanguageOption ?? "Auto";

        OpenLogFolderCommand = options.OpenLogFolderCommand ?? throw new ArgumentNullException(nameof(options.OpenLogFolderCommand));
        OpenSupportRequestCommand = options.OpenSupportRequestCommand ?? throw new ArgumentNullException(nameof(options.OpenSupportRequestCommand));
        RefreshInstallFilesCommand = new AsyncRelayCommand(
            (_, cancellationToken) => RefreshInstallFilesAsync(cancellationToken),
            onException: options.OnRefreshInstallFilesException);
    }

    public AppStrings Strings => _stringsAccessor();

    public ObservableCollection<string> SettingsLanguageOptions { get; }

    public string SelectedSettingsLanguageOption
    {
        get => _selectedSettingsLanguageOption;
        set
        {
            var normalized = value ?? "Auto";
            if (!SetProperty(ref _selectedSettingsLanguageOption, normalized))
            {
                return;
            }

            _applySettingsLanguageOption(normalized);
        }
    }

    public string SettingsStatusText
    {
        get => _settingsStatusText;
        set => SetProperty(ref _settingsStatusText, value);
    }

    public string SettingsLogFolderLabel => _isKoreanUi() ? "Log \uD3F4\uB354" : "Log folder";

    public string SettingsRefreshInstallFilesLabel =>
        _isKoreanUi() ? "\uC571 \uCE90\uC2DC \uCD08\uAE30\uD654" : "Reset app cache";

    public string SettingsGitHubLabel =>
        _isKoreanUi() ? "\uBB38\uC758\uD558\uAE30 [GitHub]" : "Contact [GitHub]";

    public string SettingsActionOpenLabel => _isKoreanUi() ? "\uC5F4\uAE30" : "Open";

    public string SettingsActionRunLabel => _isKoreanUi() ? "\uC2E4\uD589" : "Run";

    public ICommand OpenLogFolderCommand { get; }

    public ICommand OpenSupportRequestCommand { get; }

    public AsyncRelayCommand RefreshInstallFilesCommand { get; }

    public void ApplyLoadedSettings(string selectedSettingsLanguageOption)
    {
        _selectedSettingsLanguageOption = selectedSettingsLanguageOption ?? "Auto";
        OnPropertyChanged(nameof(SelectedSettingsLanguageOption));
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(SettingsLogFolderLabel));
        OnPropertyChanged(nameof(SettingsRefreshInstallFilesLabel));
        OnPropertyChanged(nameof(SettingsGitHubLabel));
        OnPropertyChanged(nameof(SettingsActionOpenLabel));
        OnPropertyChanged(nameof(SettingsActionRunLabel));
    }

    private Task RefreshInstallFilesAsync(CancellationToken cancellationToken)
    {
        return _settingsActionCoordinator.RefreshInstallFilesAsync(
            _isKoreanUi(),
            _isInstallExecutionInProgress(),
            value => SettingsStatusText = value,
            cancellationToken);
    }
}

public sealed record SettingsSectionViewModelOptions
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required ObservableCollection<string> SettingsLanguageOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
    public required SettingsActionCoordinator SettingsActionCoordinator { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required ICommand OpenLogFolderCommand { get; init; }
    public required ICommand OpenSupportRequestCommand { get; init; }
    public Action<Exception>? OnRefreshInstallFilesException { get; init; }
}
