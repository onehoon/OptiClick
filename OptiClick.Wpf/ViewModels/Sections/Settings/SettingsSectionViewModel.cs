using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels.Sections.Settings;

public sealed class SettingsSectionViewModel : ViewModelBase
{
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly Func<bool> _isKoreanUi;
    private readonly Action<string> _applySettingsLanguageOption;
    private readonly Action<string> _applyOptiScalerVariantOption;
    private readonly SettingsActionCoordinator _settingsActionCoordinator;
    private readonly Func<bool> _isInstallExecutionInProgress;
    private string _selectedSettingsLanguageOption;
    private string _selectedOptiScalerVariantOption;
    private string _settingsStatusText = "";
    private bool _suppressOptiScalerVariantApply;

    public SettingsSectionViewModel(SettingsSectionViewModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _isKoreanUi = options.IsKoreanUi ?? throw new ArgumentNullException(nameof(options.IsKoreanUi));
        _applySettingsLanguageOption = options.ApplySettingsLanguageOption ?? throw new ArgumentNullException(nameof(options.ApplySettingsLanguageOption));
        _applyOptiScalerVariantOption = options.ApplyOptiScalerVariantOption ?? throw new ArgumentNullException(nameof(options.ApplyOptiScalerVariantOption));
        _settingsActionCoordinator = options.SettingsActionCoordinator ?? throw new ArgumentNullException(nameof(options.SettingsActionCoordinator));
        _isInstallExecutionInProgress = options.IsInstallExecutionInProgress ?? throw new ArgumentNullException(nameof(options.IsInstallExecutionInProgress));
        SettingsLanguageOptions = options.SettingsLanguageOptions ?? throw new ArgumentNullException(nameof(options.SettingsLanguageOptions));
        OptiScalerVariantOptions = options.OptiScalerVariantOptions ?? throw new ArgumentNullException(nameof(options.OptiScalerVariantOptions));
        _selectedSettingsLanguageOption = options.InitialSettingsLanguageOption ?? "Auto";
        _selectedOptiScalerVariantOption = options.InitialOptiScalerVariantOption ?? "stable";

        OpenLogFolderCommand = options.OpenLogFolderCommand ?? throw new ArgumentNullException(nameof(options.OpenLogFolderCommand));
        OpenSupportRequestCommand = options.OpenSupportRequestCommand ?? throw new ArgumentNullException(nameof(options.OpenSupportRequestCommand));
        RefreshInstallFilesCommand = new AsyncRelayCommand(
            (_, cancellationToken) => RefreshInstallFilesAsync(cancellationToken),
            onException: options.OnRefreshInstallFilesException);
    }

    public AppStrings Strings => _stringsAccessor();

    public ObservableCollection<string> SettingsLanguageOptions { get; }

    public ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; }

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

    public string SelectedOptiScalerVariantOption
    {
        get => _selectedOptiScalerVariantOption;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "stable" : value;
            if (!SetProperty(ref _selectedOptiScalerVariantOption, normalized))
            {
                return;
            }

            if (_suppressOptiScalerVariantApply)
            {
                return;
            }

            _applyOptiScalerVariantOption(normalized);
        }
    }

    public string SettingsStatusText
    {
        get => _settingsStatusText;
        set => SetProperty(ref _settingsStatusText, value);
    }

    public ICommand OpenLogFolderCommand { get; }

    public ICommand OpenSupportRequestCommand { get; }

    public AsyncRelayCommand RefreshInstallFilesCommand { get; }

    public void ApplyLoadedSettings(string selectedSettingsLanguageOption)
    {
        _selectedSettingsLanguageOption = selectedSettingsLanguageOption ?? "Auto";
        OnPropertyChanged(nameof(SelectedSettingsLanguageOption));
    }

    public void ApplyOptiScalerVariantOptions(
        IEnumerable<OptiScalerVariantSelectionOption> options,
        string selectedVariant)
    {
        _suppressOptiScalerVariantApply = true;
        try
        {
            OptiScalerVariantOptions.Clear();
            foreach (var option in options ?? [])
            {
                OptiScalerVariantOptions.Add(option);
            }

            _selectedOptiScalerVariantOption = string.IsNullOrWhiteSpace(selectedVariant) ? "stable" : selectedVariant;
            OnPropertyChanged(nameof(SelectedOptiScalerVariantOption));
        }
        finally
        {
            _suppressOptiScalerVariantApply = false;
        }
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(Strings));
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
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
    public required string InitialOptiScalerVariantOption { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
    public required Action<string> ApplyOptiScalerVariantOption { get; init; }
    public required SettingsActionCoordinator SettingsActionCoordinator { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required ICommand OpenLogFolderCommand { get; init; }
    public required ICommand OpenSupportRequestCommand { get; init; }
    public Action<Exception>? OnRefreshInstallFilesException { get; init; }
}
