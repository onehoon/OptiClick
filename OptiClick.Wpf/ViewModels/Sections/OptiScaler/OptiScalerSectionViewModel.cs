using System.Collections.ObjectModel;
using System.Windows.Input;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.OptiScaler;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.ViewModels.Sections.OptiScaler;

public sealed class OptiScalerSectionViewModel : ViewModelBase
{
    private const string StableVariant = "stable";
    private const string FramerateLimit120Hz = "116";
    private const string FramerateLimit144Hz = "138";
    private const string FramerateLimit165Hz = "157";
    private const string TrueValue = "true";
    private const string FpsOverlayJustFpsValue = "0";
    private const string FpsOverlayTopLeftValue = "0";
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly Action<string, OptiScalerCommonIniSettingsDocument> _saveSettings;
    private string _selectedOptiScalerVariantOption;
    private string _selectedShowFpsMode = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _selectedMenuScale = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _selectedFpsOverlayType = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _selectedFpsOverlayPos = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _selectedFpsScale = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _selectedDisableSplashMode = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _selectedFramerateLimit = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _savedOptiScalerVariantOption;
    private string _savedShowFpsMode = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _savedMenuScale = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _savedFpsOverlayType = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _savedFpsOverlayPos = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _savedFpsScale = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _savedDisableSplashMode = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _savedFramerateLimit = OptiScalerCommonIniSettingsMaterializer.AutoValue;
    private string _statusText = "";

    public OptiScalerSectionViewModel(OptiScalerSectionViewModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _saveSettings = options.SaveSettings ?? throw new ArgumentNullException(nameof(options.SaveSettings));
        OptiScalerVariantOptions = options.OptiScalerVariantOptions ?? throw new ArgumentNullException(nameof(options.OptiScalerVariantOptions));
        _selectedOptiScalerVariantOption = NormalizeVariant(options.InitialOptiScalerVariantOption);
        _savedOptiScalerVariantOption = _selectedOptiScalerVariantOption;

        FpsDisplayOptions = new ObservableCollection<OptiScalerSettingOption>();
        SplashOptions = new ObservableCollection<OptiScalerSettingOption>();
        FpsOverlayTypeOptions = new ObservableCollection<OptiScalerSettingOption>();
        FpsOverlayPositionOptions = new ObservableCollection<OptiScalerSettingOption>();
        MenuScaleOptions = new ObservableCollection<OptiScalerSettingOption>();
        FpsScaleOptions = new ObservableCollection<OptiScalerSettingOption>();
        FramerateLimitOptions = new ObservableCollection<OptiScalerSettingOption>();
        RefreshOptionText();
        ApplySavedSettings(_selectedOptiScalerVariantOption, options.InitialCommonIniSettings);

        SaveCommand = new RelayCommand(_ => SaveChanges(), _ => IsSaveEnabled);
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

    public string SelectedOptiScalerVariantOption
    {
        get => _selectedOptiScalerVariantOption;
        set
        {
            var normalized = NormalizeVariant(value);
            if (SetProperty(ref _selectedOptiScalerVariantOption, normalized))
            {
                UpdateDirtyState();
            }
        }
    }

    public string SelectedShowFpsMode
    {
        get => _selectedShowFpsMode;
        set
        {
            if (SetProperty(ref _selectedShowFpsMode, NormalizeShowFpsMode(value)))
            {
                UpdateDirtyState();
                OnPropertyChanged(nameof(IsFpsOverlayDetailsVisible));
            }
        }
    }

    public string SelectedMenuScale
    {
        get => _selectedMenuScale;
        set => SetDraftValue(ref _selectedMenuScale, NormalizeAutoValue(value));
    }

    public string SelectedFpsOverlayType
    {
        get => _selectedFpsOverlayType;
        set => SetDraftValue(ref _selectedFpsOverlayType, NormalizeFpsOverlayTypeSelection(value));
    }

    public string SelectedFpsOverlayPos
    {
        get => _selectedFpsOverlayPos;
        set => SetDraftValue(ref _selectedFpsOverlayPos, NormalizeFpsOverlayPositionSelection(value));
    }

    public string SelectedFpsScale
    {
        get => _selectedFpsScale;
        set => SetDraftValue(ref _selectedFpsScale, NormalizeAutoValue(value));
    }

    public string SelectedDisableSplashMode
    {
        get => _selectedDisableSplashMode;
        set => SetDraftValue(ref _selectedDisableSplashMode, NormalizeAutoValue(value));
    }

    public string SelectedFramerateLimit
    {
        get => _selectedFramerateLimit;
        set => SetDraftValue(ref _selectedFramerateLimit, NormalizeFramerateLimitSelection(value));
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool HasUnsavedChanges => IsDraftDifferentFromSaved();

    public bool IsSaveEnabled => HasUnsavedChanges;

    public bool IsFpsOverlayDetailsVisible => string.Equals(
        NormalizeShowFpsMode(SelectedShowFpsMode),
        TrueValue,
        StringComparison.OrdinalIgnoreCase);

    public ICommand SaveCommand { get; }

    public void ApplySavedSettings(
        string selectedVariant,
        OptiScalerCommonIniSettingsDocument? commonIniSettings)
    {
        _savedOptiScalerVariantOption = NormalizeVariant(selectedVariant);
        ApplyCommonIniSettingsToSavedState(commonIniSettings);
        DiscardChanges();
    }

    public void ApplyOptiScalerVariantOptions(
        IEnumerable<OptiScalerVariantSelectionOption> options,
        string selectedVariant)
    {
        var wasDirty = HasUnsavedChanges;
        var nextOptions = (options ?? []).ToArray();
        var normalizedSelectedVariant = NormalizeVariant(selectedVariant);
        var optionsChanged = !AreSameOptiScalerVariantOptions(OptiScalerVariantOptions, nextOptions);
        if (optionsChanged)
        {
            OptiScalerVariantOptions.Clear();
            foreach (var option in nextOptions)
            {
                OptiScalerVariantOptions.Add(option);
            }
        }

        _savedOptiScalerVariantOption = normalizedSelectedVariant;
        if (!wasDirty)
        {
            _selectedOptiScalerVariantOption = normalizedSelectedVariant;
            OnPropertyChanged(nameof(SelectedOptiScalerVariantOption));
        }

        UpdateDirtyState();
    }

    public void DiscardChanges()
    {
        _selectedOptiScalerVariantOption = _savedOptiScalerVariantOption;
        _selectedShowFpsMode = _savedShowFpsMode;
        _selectedMenuScale = _savedMenuScale;
        _selectedFpsOverlayType = _savedFpsOverlayType;
        _selectedFpsOverlayPos = _savedFpsOverlayPos;
        _selectedFpsScale = _savedFpsScale;
        _selectedDisableSplashMode = _savedDisableSplashMode;
        _selectedFramerateLimit = _savedFramerateLimit;
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
        UpdateDirtyState();
    }

    public void RefreshLocalization()
    {
        RefreshOptionText();
        OnPropertyChanged(nameof(Strings));
    }

    public void SaveChanges()
    {
        var normalizedVariant = NormalizeVariant(SelectedOptiScalerVariantOption);
        var normalizedFramerateLimit = NormalizeFramerateLimitSelection(SelectedFramerateLimit);
        var document = BuildCommonIniSettingsDocument(normalizedFramerateLimit);
        _saveSettings(normalizedVariant, document);

        _savedOptiScalerVariantOption = normalizedVariant;
        _savedShowFpsMode = NormalizeShowFpsMode(SelectedShowFpsMode);
        _savedMenuScale = NormalizeAutoValue(SelectedMenuScale);
        _savedFpsOverlayType = NormalizeEffectiveFpsOverlayTypeValue(SelectedFpsOverlayType);
        _savedFpsOverlayPos = NormalizeEffectiveFpsOverlayPositionValue(SelectedFpsOverlayPos);
        _savedFpsScale = NormalizeEffectiveFpsOverlayDetailValue(SelectedFpsScale);
        _savedDisableSplashMode = NormalizeAutoValue(SelectedDisableSplashMode);
        _savedFramerateLimit = normalizedFramerateLimit;
        _selectedFramerateLimit = normalizedFramerateLimit;
        OnPropertyChanged(nameof(SelectedFramerateLimit));
        StatusText = Strings.OptiScalerSavedStatus;
        UpdateDirtyState();
    }

    private OptiScalerCommonIniSettingsDocument BuildCommonIniSettingsDocument(string normalizedFramerateLimit)
    {
        var entries = new List<OptiScalerCommonIniEntry>();
        AddExplicitEntry(entries, "Menu", "ShowFps", SelectedShowFpsMode);
        AddExplicitEntry(entries, "Menu", "Scale", SelectedMenuScale);
        if (IsFpsOverlayDetailsVisible)
        {
            AddExplicitEntry(entries, "Menu", "FpsOverlayType", NormalizeFpsOverlayTypeSelection(SelectedFpsOverlayType));
            AddExplicitEntry(entries, "Menu", "FpsOverlayPos", NormalizeFpsOverlayPositionSelection(SelectedFpsOverlayPos));
            AddExplicitEntry(entries, "Menu", "FpsScale", SelectedFpsScale);
        }

        AddExplicitEntry(entries, "Menu", "DisableSplash", SelectedDisableSplashMode);
        AddExplicitEntry(entries, "Framerate", "FramerateLimit", normalizedFramerateLimit);
        return OptiScalerCommonIniSettingsMaterializer.NormalizeDocument(
            new OptiScalerCommonIniSettingsDocument
            {
                Version = 1,
                Entries = entries
            });
    }

    private void ApplyCommonIniSettingsToSavedState(OptiScalerCommonIniSettingsDocument? commonIniSettings)
    {
        var materialized = OptiScalerCommonIniSettingsMaterializer.Materialize(commonIniSettings);
        _savedShowFpsMode = NormalizeShowFpsMode(ReadSavedValue(materialized, OptiScalerCommonIniSettingsMaterializer.ShowFpsKey));
        _savedMenuScale = ReadSavedValue(materialized, OptiScalerCommonIniSettingsMaterializer.MenuScaleKey);
        _savedFpsOverlayType = NormalizeFpsOverlayTypeSelection(
            ReadSavedValue(materialized, OptiScalerCommonIniSettingsMaterializer.FpsOverlayTypeKey));
        _savedFpsOverlayPos = NormalizeFpsOverlayPositionSelection(
            ReadSavedValue(materialized, OptiScalerCommonIniSettingsMaterializer.FpsOverlayPosKey));
        _savedFpsScale = ReadSavedValue(materialized, OptiScalerCommonIniSettingsMaterializer.FpsScaleKey);
        _savedDisableSplashMode = ReadSavedValue(materialized, OptiScalerCommonIniSettingsMaterializer.DisableSplashKey);
        _savedFramerateLimit = NormalizeFramerateLimitSelection(
            ReadSavedValue(materialized, OptiScalerCommonIniSettingsMaterializer.FramerateLimitKey));
    }

    private void RefreshOptionText()
    {
        ReplaceOptions(
            FpsDisplayOptions,
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, Strings.OptiScalerOff),
                new OptiScalerSettingOption(TrueValue, Strings.OptiScalerOn)
            ]);
        ReplaceOptions(
            SplashOptions,
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, Strings.OptiScalerSplashShow),
                new OptiScalerSettingOption("true", Strings.OptiScalerSplashHide)
            ]);
        ReplaceOptions(
            FpsOverlayTypeOptions,
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, Strings.OptiScalerOverlayTypeJustFps),
                new OptiScalerSettingOption("1", Strings.OptiScalerOverlayTypeSimple),
                new OptiScalerSettingOption("2", Strings.OptiScalerOverlayTypeDetailed),
                new OptiScalerSettingOption("3", Strings.OptiScalerOverlayTypeDetailedGraph),
                new OptiScalerSettingOption("4", Strings.OptiScalerOverlayTypeFull),
                new OptiScalerSettingOption("5", Strings.OptiScalerOverlayTypeFullGraph),
                new OptiScalerSettingOption("6", Strings.OptiScalerOverlayTypeReflex)
            ]);
        ReplaceOptions(
            FpsOverlayPositionOptions,
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, Strings.OptiScalerTopLeft),
                new OptiScalerSettingOption("1", Strings.OptiScalerTopRight),
                new OptiScalerSettingOption("2", Strings.OptiScalerBottomLeft),
                new OptiScalerSettingOption("3", Strings.OptiScalerBottomRight)
            ]);
        ReplaceOptions(
            MenuScaleOptions,
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, Strings.OptiScalerAuto),
                new OptiScalerSettingOption("0.9", "0.9"),
                new OptiScalerSettingOption("1.0", "1.0"),
                new OptiScalerSettingOption("1.1", "1.1"),
                new OptiScalerSettingOption("1.2", "1.2"),
                new OptiScalerSettingOption("1.3", "1.3"),
                new OptiScalerSettingOption("1.4", "1.4"),
                new OptiScalerSettingOption("1.5", "1.5")
            ]);
        ReplaceOptions(
            FpsScaleOptions,
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, Strings.OptiScalerAuto),
                new OptiScalerSettingOption("1.0", "1.0"),
                new OptiScalerSettingOption("1.1", "1.1"),
                new OptiScalerSettingOption("1.2", "1.2"),
                new OptiScalerSettingOption("1.3", "1.3"),
                new OptiScalerSettingOption("1.4", "1.4"),
                new OptiScalerSettingOption("1.5", "1.5"),
                new OptiScalerSettingOption("1.6", "1.6"),
                new OptiScalerSettingOption("1.7", "1.7"),
                new OptiScalerSettingOption("1.8", "1.8"),
                new OptiScalerSettingOption("1.9", "1.9"),
                new OptiScalerSettingOption("2.0", "2.0")
            ]);
        ReplaceOptions(
            FramerateLimitOptions,
            [
                new OptiScalerSettingOption(OptiScalerCommonIniSettingsMaterializer.AutoValue, Strings.OptiScalerUnlimited),
                new OptiScalerSettingOption(FramerateLimit120Hz, FramerateLimit120Hz),
                new OptiScalerSettingOption(FramerateLimit144Hz, FramerateLimit144Hz),
                new OptiScalerSettingOption(FramerateLimit165Hz, FramerateLimit165Hz)
            ]);
    }

    private void SetDraftValue(ref string field, string value)
    {
        if (SetProperty(ref field, value))
        {
            UpdateDirtyState();
        }
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

    private bool IsDraftDifferentFromSaved()
    {
        var selectedShowFpsMode = NormalizeShowFpsMode(SelectedShowFpsMode);
        var savedShowFpsMode = NormalizeShowFpsMode(_savedShowFpsMode);
        var shouldCompareFpsOverlayDetails = IsFpsOverlayEnabled(selectedShowFpsMode) || IsFpsOverlayEnabled(savedShowFpsMode);
        return !string.Equals(NormalizeVariant(SelectedOptiScalerVariantOption), _savedOptiScalerVariantOption, StringComparison.Ordinal)
               || !string.Equals(selectedShowFpsMode, savedShowFpsMode, StringComparison.OrdinalIgnoreCase)
               || !string.Equals(NormalizeAutoValue(SelectedMenuScale), _savedMenuScale, StringComparison.OrdinalIgnoreCase)
               || (shouldCompareFpsOverlayDetails
                   && (!string.Equals(NormalizeFpsOverlayTypeSelection(SelectedFpsOverlayType), _savedFpsOverlayType, StringComparison.OrdinalIgnoreCase)
                       || !string.Equals(NormalizeFpsOverlayPositionSelection(SelectedFpsOverlayPos), _savedFpsOverlayPos, StringComparison.OrdinalIgnoreCase)
                       || !string.Equals(NormalizeAutoValue(SelectedFpsScale), _savedFpsScale, StringComparison.OrdinalIgnoreCase)))
               || !string.Equals(NormalizeAutoValue(SelectedDisableSplashMode), _savedDisableSplashMode, StringComparison.OrdinalIgnoreCase)
               || !string.Equals(NormalizeFramerateLimitSelection(SelectedFramerateLimit), _savedFramerateLimit, StringComparison.OrdinalIgnoreCase);
    }

    private string NormalizeEffectiveFpsOverlayDetailValue(string? value)
    {
        return IsFpsOverlayDetailsVisible
            ? NormalizeAutoValue(value)
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    private string NormalizeEffectiveFpsOverlayTypeValue(string? value)
    {
        return IsFpsOverlayDetailsVisible
            ? NormalizeFpsOverlayTypeSelection(value)
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    private static string NormalizeFpsOverlayTypeSelection(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, FpsOverlayJustFpsValue, StringComparison.OrdinalIgnoreCase)
            ? OptiScalerCommonIniSettingsMaterializer.AutoValue
            : normalized;
    }

    private string NormalizeEffectiveFpsOverlayPositionValue(string? value)
    {
        return IsFpsOverlayDetailsVisible
            ? NormalizeFpsOverlayPositionSelection(value)
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    private static string NormalizeFpsOverlayPositionSelection(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, FpsOverlayTopLeftValue, StringComparison.OrdinalIgnoreCase)
            ? OptiScalerCommonIniSettingsMaterializer.AutoValue
            : normalized;
    }

    private static bool IsFpsOverlayEnabled(string? value)
    {
        return string.Equals(NormalizeShowFpsMode(value), TrueValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeShowFpsMode(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, TrueValue, StringComparison.OrdinalIgnoreCase)
            ? TrueValue
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    private static void AddExplicitEntry(
        ICollection<OptiScalerCommonIniEntry> entries,
        string section,
        string key,
        string value)
    {
        var normalized = NormalizeAutoValue(value);
        if (string.Equals(normalized, OptiScalerCommonIniSettingsMaterializer.AutoValue, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        entries.Add(new OptiScalerCommonIniEntry
        {
            Section = section,
            Key = key,
            Value = normalized
        });
    }

    private static string NormalizeFramerateLimitSelection(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, FramerateLimit120Hz, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, FramerateLimit144Hz, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, FramerateLimit165Hz, StringComparison.OrdinalIgnoreCase)
            ? normalized
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    private static string NormalizeAutoValue(string? value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? OptiScalerCommonIniSettingsMaterializer.AutoValue
            : normalized;
    }

    private static string NormalizeVariant(string? value)
    {
        var normalized = OptiScalerVariantCatalogBuilder.NormalizeVariant(value);
        return string.IsNullOrWhiteSpace(normalized) ? StableVariant : normalized;
    }

    private static string ReadSavedValue(
        IReadOnlyDictionary<string, string> materialized,
        string key,
        string? fallback = null)
    {
        return materialized.TryGetValue(key, out var value)
            ? NormalizeAutoValue(value)
            : fallback ?? OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    private static void ReplaceOptions(
        ObservableCollection<OptiScalerSettingOption> target,
        IReadOnlyList<OptiScalerSettingOption> options)
    {
        target.Clear();
        foreach (var option in options)
        {
            target.Add(option);
        }
    }

    private static bool AreSameOptiScalerVariantOptions(
        IList<OptiScalerVariantSelectionOption> current,
        IReadOnlyList<OptiScalerVariantSelectionOption> next)
    {
        if (current.Count != next.Count)
        {
            return false;
        }

        for (var index = 0; index < current.Count; index++)
        {
            var left = current[index];
            var right = next[index];
            if (!string.Equals(left.Variant, right.Variant, StringComparison.Ordinal)
                || !string.Equals(left.DisplayLabel, right.DisplayLabel, StringComparison.Ordinal)
                || !string.Equals(left.Version, right.Version, StringComparison.Ordinal)
                || !string.Equals(left.DisplayVersion, right.DisplayVersion, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}

public sealed record OptiScalerSectionViewModelOptions
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required string InitialOptiScalerVariantOption { get; init; }
    public required OptiScalerCommonIniSettingsDocument InitialCommonIniSettings { get; init; }
    public required Action<string, OptiScalerCommonIniSettingsDocument> SaveSettings { get; init; }
}

public sealed record OptiScalerSettingOption(string Value, string DisplayText);
