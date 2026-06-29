namespace OptiClick.Core.OptiScaler;

public sealed class OptiScalerCommonIniSettingsDraftState
{
    private const string StableVariant = "stable";
    private OptiScalerCommonIniSettingsDraft _savedDraft = new();
    private OptiScalerCommonIniSettingsDraft _selectedDraft = new();
    private string _selectedOptiScalerVariantOption = StableVariant;
    private string _savedOptiScalerVariantOption = StableVariant;

    public string SelectedOptiScalerVariantOption
    {
        get => _selectedOptiScalerVariantOption;
        set => _selectedOptiScalerVariantOption = NormalizeVariant(value);
    }

    public string SavedOptiScalerVariantOption
    {
        get => _savedOptiScalerVariantOption;
        set => _savedOptiScalerVariantOption = NormalizeVariant(value);
    }

    public string SelectedShowFpsMode
    {
        get => _selectedDraft.ShowFpsMode;
        set => _selectedDraft = _selectedDraft with { ShowFpsMode = value };
    }

    public string SavedShowFpsMode => _savedDraft.ShowFpsMode;
    public string SelectedMenuScale
    {
        get => _selectedDraft.MenuScale;
        set => _selectedDraft = _selectedDraft with { MenuScale = value };
    }

    public string SavedMenuScale => _savedDraft.MenuScale;
    public string SelectedFpsOverlayType
    {
        get => _selectedDraft.FpsOverlayType;
        set => _selectedDraft = _selectedDraft with { FpsOverlayType = value };
    }

    public string SavedFpsOverlayType => _savedDraft.FpsOverlayType;
    public string SelectedFpsOverlayPos
    {
        get => _selectedDraft.FpsOverlayPos;
        set => _selectedDraft = _selectedDraft with { FpsOverlayPos = value };
    }

    public string SavedFpsOverlayPos => _savedDraft.FpsOverlayPos;
    public string SelectedFpsScale
    {
        get => _selectedDraft.FpsScale;
        set => _selectedDraft = _selectedDraft with { FpsScale = value };
    }

    public string SavedFpsScale => _savedDraft.FpsScale;
    public string SelectedDisableSplashMode
    {
        get => _selectedDraft.DisableSplashMode;
        set => _selectedDraft = _selectedDraft with { DisableSplashMode = value };
    }

    public string SavedDisableSplashMode => _savedDraft.DisableSplashMode;
    public string SelectedFramerateLimit
    {
        get => _selectedDraft.FramerateLimit;
        set => _selectedDraft = _selectedDraft with { FramerateLimit = value };
    }

    public string SavedFramerateLimit => _savedDraft.FramerateLimit;
    public string SelectedFsr411Mode
    {
        get => _selectedDraft.Fsr411Mode;
        set => _selectedDraft = _selectedDraft with { Fsr411Mode = OptiScalerFsr411Policy.NormalizeMode(value) };
    }

    public string SavedFsr411Mode => _savedDraft.Fsr411Mode;

    public void ApplySavedState(string selectedVariant, OptiScalerCommonIniSettingsDocument? commonIniSettings)
    {
        _savedOptiScalerVariantOption = NormalizeVariant(selectedVariant);
        _selectedOptiScalerVariantOption = _savedOptiScalerVariantOption;
        _savedDraft = OptiScalerCommonIniSettingsMapper.NormalizeDraft(
            OptiScalerCommonIniSettingsMapper.FromDocument(commonIniSettings));
        _selectedDraft = _savedDraft;
    }

    public void DiscardChanges()
    {
        _selectedOptiScalerVariantOption = _savedOptiScalerVariantOption;
        _selectedDraft = _savedDraft;
    }

    public void ApplyCurrentSelectionAsSaved(OptiScalerCommonIniSettingsDraft normalizedDraft, string selectedVariant)
    {
        _savedOptiScalerVariantOption = NormalizeVariant(selectedVariant);
        _savedDraft = normalizedDraft;
        _selectedDraft = normalizedDraft;
    }

    public OptiScalerCommonIniSettingsDraft BuildPersistableDraft()
    {
        return OptiScalerCommonIniSettingsMapper.NormalizePersistedDraft(
            OptiScalerCommonIniSettingsMapper.NormalizeDraft(_selectedDraft));
    }

    public OptiScalerCommonIniSettingsDocument BuildPersistableDocument()
    {
        return OptiScalerCommonIniSettingsMapper.ToDocument(BuildPersistableDraft());
    }

    public bool HasUnsavedChanges()
    {
        var selected = OptiScalerCommonIniSettingsMapper.NormalizeDraft(_selectedDraft);
        var saved = OptiScalerCommonIniSettingsMapper.NormalizeDraft(_savedDraft);
        var compareOverlayDetails = OptiScalerCommonIniSettingsMapper.IsFpsOverlayEnabled(selected.ShowFpsMode)
                                   || OptiScalerCommonIniSettingsMapper.IsFpsOverlayEnabled(saved.ShowFpsMode);

        return !string.Equals(_selectedOptiScalerVariantOption, _savedOptiScalerVariantOption, StringComparison.Ordinal)
               || !string.Equals(selected.ShowFpsMode, saved.ShowFpsMode, StringComparison.OrdinalIgnoreCase)
               || !string.Equals(selected.Fsr411Mode, saved.Fsr411Mode, StringComparison.OrdinalIgnoreCase)
               || !string.Equals(selected.MenuScale, saved.MenuScale, StringComparison.OrdinalIgnoreCase)
               || (compareOverlayDetails
                   && (!string.Equals(selected.FpsOverlayType, saved.FpsOverlayType, StringComparison.OrdinalIgnoreCase)
                       || !string.Equals(selected.FpsOverlayPos, saved.FpsOverlayPos, StringComparison.OrdinalIgnoreCase)
                       || !string.Equals(selected.FpsScale, saved.FpsScale, StringComparison.OrdinalIgnoreCase)))
               || !string.Equals(selected.DisableSplashMode, saved.DisableSplashMode, StringComparison.OrdinalIgnoreCase)
               || !string.Equals(selected.FramerateLimit, saved.FramerateLimit, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeVariant(string? value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? StableVariant : normalized;
    }

    public bool NormalizeFsr411ForBundle(string? gpuBundleKey)
    {
        var saved = OptiScalerFsr411Policy.NormalizeModeForBundle(_savedDraft.Fsr411Mode, gpuBundleKey);
        var selected = OptiScalerFsr411Policy.NormalizeModeForBundle(_selectedDraft.Fsr411Mode, gpuBundleKey);
        var changed = !string.Equals(saved, _savedDraft.Fsr411Mode, StringComparison.Ordinal)
                      || !string.Equals(selected, _selectedDraft.Fsr411Mode, StringComparison.Ordinal);
        if (changed)
        {
            _savedDraft = _savedDraft with { Fsr411Mode = saved };
            _selectedDraft = _selectedDraft with { Fsr411Mode = selected };
        }

        return changed;
    }
}
