using System.Collections.ObjectModel;
using System.Windows.Input;
using OptiClick.Core.OptiScaler;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels.Sections.OptiScaler;

public sealed class OptiScalerSectionViewModel : ViewModelBase
{
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly IOptiScalerSectionSaveHandler _saveHandler;
    private readonly OptiScalerSectionStateController _stateController;
    private readonly OptiScalerSectionOptionController _optionController;
    private string _statusText = "";
    private string _currentGpuBundleKey = "";
    private bool _isRefreshingOptionText;

    internal OptiScalerSectionViewModel(OptiScalerSectionViewModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _saveHandler = options.SaveHandler ?? throw new ArgumentNullException(nameof(options.SaveHandler));
        _stateController = options.StateController ?? throw new ArgumentNullException(nameof(options.StateController));
        _optionController = options.OptionController ?? throw new ArgumentNullException(nameof(options.OptionController));
        OptiScalerVariantOptions = options.OptiScalerVariantOptions ?? throw new ArgumentNullException(nameof(options.OptiScalerVariantOptions));
        FpsDisplayOptions = new ObservableCollection<OptiScalerSettingOption>();
        SplashOptions = new ObservableCollection<OptiScalerSettingOption>();
        FpsOverlayTypeOptions = new ObservableCollection<OptiScalerSettingOption>();
        FpsOverlayPositionOptions = new ObservableCollection<OptiScalerSettingOption>();
        MenuScaleOptions = new ObservableCollection<OptiScalerSettingOption>();
        FpsScaleOptions = new ObservableCollection<OptiScalerSettingOption>();
        FramerateLimitOptions = new ObservableCollection<OptiScalerSettingOption>();
        Fsr411ModeOptions = new ObservableCollection<OptiScalerSettingOption>();
        _currentGpuBundleKey = options.InitialGpuBundleKey ?? "";
        RefreshOptionText();

        SaveCommand = new RelayCommand(_ => SaveChanges(), _ => IsSaveEnabled);
        ApplyGpuBundleKey(_currentGpuBundleKey, persistModeChanges: true);
    }

    public AppStrings Strings => _stringsAccessor();

    public ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> FpsDisplayOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> SplashOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> FpsOverlayTypeOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> FpsOverlayPositionOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> MenuScaleOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> FpsScaleOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> FramerateLimitOptions { get; }

    public ObservableCollection<OptiScalerSettingOption> Fsr411ModeOptions { get; }

    public string SelectedOptiScalerVariantOption
    {
        get => _stateController.SelectedOptiScalerVariantOption;
        set
        {
            var normalized = _stateController.NormalizeVariant(value);
            if (!string.Equals(_stateController.SelectedOptiScalerVariantOption, normalized, StringComparison.Ordinal))
            {
                _stateController.SelectedOptiScalerVariantOption = normalized;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }
    }

    public string SelectedShowFpsMode
    {
        get => _stateController.SelectedShowFpsMode;
        set
        {
            if (SetDraftValue(
                () => _stateController.SelectedShowFpsMode,
                value => _stateController.SelectedShowFpsMode = value,
                _stateController.NormalizeShowFpsMode(value),
                nameof(SelectedShowFpsMode)))
            {
                OnPropertyChanged(nameof(IsFpsOverlayDetailsVisible));
            }
        }
    }

    public string SelectedMenuScale
    {
        get => _stateController.SelectedMenuScale;
        set => SetDraftValue(
            () => _stateController.SelectedMenuScale,
            value => _stateController.SelectedMenuScale = value,
            _stateController.NormalizeAutoValue(value),
            nameof(SelectedMenuScale));
    }

    public string SelectedFpsOverlayType
    {
        get => _stateController.SelectedFpsOverlayType;
        set => SetDraftValue(
            () => _stateController.SelectedFpsOverlayType,
            value => _stateController.SelectedFpsOverlayType = value,
            _stateController.NormalizeFpsOverlayTypeSelection(value),
            nameof(SelectedFpsOverlayType));
    }

    public string SelectedFpsOverlayPos
    {
        get => _stateController.SelectedFpsOverlayPos;
        set => SetDraftValue(
            () => _stateController.SelectedFpsOverlayPos,
            value => _stateController.SelectedFpsOverlayPos = value,
            _stateController.NormalizeFpsOverlayPositionSelection(value),
            nameof(SelectedFpsOverlayPos));
    }

    public string SelectedFpsScale
    {
        get => _stateController.SelectedFpsScale;
        set => SetDraftValue(
            () => _stateController.SelectedFpsScale,
            value => _stateController.SelectedFpsScale = value,
            _stateController.NormalizeAutoValue(value),
            nameof(SelectedFpsScale));
    }

    public string SelectedDisableSplashMode
    {
        get => _stateController.SelectedDisableSplashMode;
        set => SetDraftValue(
            () => _stateController.SelectedDisableSplashMode,
            value => _stateController.SelectedDisableSplashMode = value,
            _stateController.NormalizeAutoValue(value),
            nameof(SelectedDisableSplashMode));
    }

    public string SelectedFramerateLimit
    {
        get => _stateController.SelectedFramerateLimit;
        set => SetDraftValue(
            () => _stateController.SelectedFramerateLimit,
            value => _stateController.SelectedFramerateLimit = value,
            _stateController.NormalizeFramerateLimitSelection(value),
            nameof(SelectedFramerateLimit));
    }

    public string SelectedFsr411Mode
    {
        get => _stateController.SelectedFsr411Mode;
        set => SetDraftValue(
            () => _stateController.SelectedFsr411Mode,
            value => _stateController.SelectedFsr411Mode = value,
            _stateController.NormalizeFsr411Mode(value),
            nameof(SelectedFsr411Mode));
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool HasUnsavedChanges => _stateController.HasUnsavedChanges;

    public bool IsSaveEnabled => HasUnsavedChanges;

    public bool IsFpsOverlayDetailsVisible => _stateController.IsFpsOverlayDetailsVisible;

    public bool IsFsr411Visible => _stateController.IsFsr411Visible;

    public ICommand SaveCommand { get; }

    public void ApplySavedSettings(
        string selectedVariant,
        OptiScalerCommonIniSettingsDocument? commonIniSettings)
    {
        _stateController.ApplySavedSettings(selectedVariant, commonIniSettings);
        ApplyGpuBundleKey(_currentGpuBundleKey, persistModeChanges: true);
        DiscardChanges();
    }

    public void ApplyGpuBundleKey(string? gpuBundleKey, bool persistModeChanges = false)
    {
        _currentGpuBundleKey = (gpuBundleKey ?? "").Trim();
        var change = _stateController.ApplyGpuBundleKey(_currentGpuBundleKey);
        if (change.VisibilityChanged)
        {
            OnPropertyChanged(nameof(IsFsr411Visible));
        }

        if (change.ModeChanged)
        {
            OnPropertyChanged(nameof(SelectedFsr411Mode));
            if (persistModeChanges)
            {
                SaveFsr411Normalization();
            }
        }

        UpdateDirtyState();
    }

    public void ApplyOptiScalerVariantOptions(
        IEnumerable<OptiScalerVariantSelectionOption> options,
        string selectedVariant)
    {
        var wasDirty = HasUnsavedChanges;
        var normalizedSelectedVariant = _stateController.NormalizeVariant(selectedVariant);
        var optionsChanged = _optionController.ApplyVariantOptions(OptiScalerVariantOptions, options ?? []);
        if (optionsChanged)
        {
            OnPropertyChanged(nameof(OptiScalerVariantOptions));
        }

        _stateController.SetSavedVariant(normalizedSelectedVariant);
        if (!wasDirty)
        {
            _stateController.SelectedOptiScalerVariantOption = normalizedSelectedVariant;
            OnPropertyChanged(nameof(SelectedOptiScalerVariantOption));
        }

        UpdateDirtyState();
    }

    public void DiscardChanges()
    {
        _stateController.DiscardChanges();
        StatusText = "";
        OnPropertyChanged(nameof(SelectedOptiScalerVariantOption));
        OnPropertyChanged(nameof(SelectedShowFpsMode));
        OnPropertyChanged(nameof(IsFpsOverlayDetailsVisible));
        OnPropertyChanged(nameof(SelectedMenuScale));
        OnPropertyChanged(nameof(SelectedFpsOverlayType));
        OnPropertyChanged(nameof(SelectedFpsOverlayPos));
        OnPropertyChanged(nameof(SelectedFpsScale));
        OnPropertyChanged(nameof(SelectedDisableSplashMode));
        OnPropertyChanged(nameof(SelectedFramerateLimit));
        OnPropertyChanged(nameof(SelectedFsr411Mode));
        UpdateDirtyState();
    }

    public void RefreshLocalization()
    {
        _isRefreshingOptionText = true;
        try
        {
            RefreshOptionText();
        }
        finally
        {
            _isRefreshingOptionText = false;
        }

        OnPropertyChanged(nameof(Strings));
        RefreshSelectedOptionBindings();
    }

    public void SaveChanges()
    {
        var savePayload = _stateController.BuildSavePayload();
        var saveResult = _saveHandler.Save(new OptiScalerSectionSaveRequest(
            savePayload.SelectedVariant,
            savePayload.Document));

        _stateController.ApplySavedSettings(
            saveResult.SelectedVariant,
            saveResult.CommonIniSettings);
        ApplyGpuBundleKey(_currentGpuBundleKey, persistModeChanges: true);
        OnPropertyChanged(nameof(SelectedOptiScalerVariantOption));
        RefreshSelectedOptionBindings();
        StatusText = Strings.OptiScalerSavedStatus;
        UpdateDirtyState();
    }

    private void RefreshOptionText()
    {
        _optionController.RefreshOptionText(
            Strings,
            FpsDisplayOptions,
            SplashOptions,
            FpsOverlayTypeOptions,
            FpsOverlayPositionOptions,
            MenuScaleOptions,
            FpsScaleOptions,
            FramerateLimitOptions,
            Fsr411ModeOptions);
    }

    private bool SetDraftValue(
        Func<string> getValue,
        Action<string> setValue,
        string value,
        string propertyName)
    {
        if (_isRefreshingOptionText)
        {
            return false;
        }

        var currentValue = getValue();
        if (string.Equals(currentValue, value, StringComparison.Ordinal))
        {
            return false;
        }

        setValue(value);
        OnPropertyChanged(propertyName);
        UpdateDirtyState();
        return true;
    }

    private void RefreshSelectedOptionBindings()
    {
        OnPropertyChanged(nameof(SelectedShowFpsMode));
        OnPropertyChanged(nameof(IsFpsOverlayDetailsVisible));
        OnPropertyChanged(nameof(SelectedMenuScale));
        OnPropertyChanged(nameof(SelectedFpsOverlayType));
        OnPropertyChanged(nameof(SelectedFpsOverlayPos));
        OnPropertyChanged(nameof(SelectedFpsScale));
        OnPropertyChanged(nameof(SelectedDisableSplashMode));
        OnPropertyChanged(nameof(SelectedFramerateLimit));
        OnPropertyChanged(nameof(SelectedFsr411Mode));
    }

    private void SaveFsr411Normalization()
    {
        var savePayload = _stateController.BuildSavePayload();
        var saveResult = _saveHandler.Save(new OptiScalerSectionSaveRequest(
            savePayload.SelectedVariant,
            savePayload.Document));
        _stateController.ApplySavedSettings(
            saveResult.SelectedVariant,
            saveResult.CommonIniSettings);
    }

    private void UpdateDirtyState()
    {
        if (HasUnsavedChanges && !string.IsNullOrWhiteSpace(StatusText))
        {
            StatusText = "";
        }

        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsSaveEnabled));
        if (SaveCommand is RelayCommand saveCommand)
        {
            saveCommand.RaiseCanExecuteChanged();
        }
    }

}

internal sealed record OptiScalerSectionViewModelOptions
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required IOptiScalerSectionSaveHandler SaveHandler { get; init; }
    public required OptiScalerSectionStateController StateController { get; init; }
    public required OptiScalerSectionOptionController OptionController { get; init; }
    public string InitialGpuBundleKey { get; init; } = "";
}

public sealed record OptiScalerSettingOption(string Value, string DisplayText);
