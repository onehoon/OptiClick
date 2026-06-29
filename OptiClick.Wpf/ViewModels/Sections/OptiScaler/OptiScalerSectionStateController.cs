using OptiClick.Core.OptiScaler;

namespace OptiClick.Wpf.ViewModels.Sections.OptiScaler;

internal sealed class OptiScalerSectionStateController
{
    private readonly IOptiScalerSectionSettingsCodec _settingsCodec;
    private readonly OptiScalerCommonIniSettingsDraftState _draftState = new();

    public OptiScalerSectionStateController(
        IOptiScalerSectionSettingsCodec settingsCodec,
        string initialSelectedVariant,
        OptiScalerCommonIniSettingsDocument? initialCommonIniSettings,
        string initialGpuBundleKey = "")
    {
        _settingsCodec = settingsCodec ?? throw new ArgumentNullException(nameof(settingsCodec));
        ApplySavedSettings(initialSelectedVariant, initialCommonIniSettings);
        ApplyGpuBundleKey(initialGpuBundleKey);
    }

    public string SelectedOptiScalerVariantOption
    {
        get => _draftState.SelectedOptiScalerVariantOption;
        set => _draftState.SelectedOptiScalerVariantOption = NormalizeVariant(value);
    }

    public string SelectedShowFpsMode
    {
        get => _draftState.SelectedShowFpsMode;
        set => _draftState.SelectedShowFpsMode = value;
    }

    public string SelectedMenuScale
    {
        get => _draftState.SelectedMenuScale;
        set => _draftState.SelectedMenuScale = value;
    }

    public string SelectedFpsOverlayType
    {
        get => _draftState.SelectedFpsOverlayType;
        set => _draftState.SelectedFpsOverlayType = value;
    }

    public string SelectedFpsOverlayPos
    {
        get => _draftState.SelectedFpsOverlayPos;
        set => _draftState.SelectedFpsOverlayPos = value;
    }

    public string SelectedFpsScale
    {
        get => _draftState.SelectedFpsScale;
        set => _draftState.SelectedFpsScale = value;
    }

    public string SelectedDisableSplashMode
    {
        get => _draftState.SelectedDisableSplashMode;
        set => _draftState.SelectedDisableSplashMode = value;
    }

    public string SelectedFramerateLimit
    {
        get => _draftState.SelectedFramerateLimit;
        set => _draftState.SelectedFramerateLimit = value;
    }

    public string SelectedFsr411Mode
    {
        get => _draftState.SelectedFsr411Mode;
        set => _draftState.SelectedFsr411Mode = NormalizeFsr411Mode(value);
    }

    public bool HasUnsavedChanges => _draftState.HasUnsavedChanges();

    public bool IsFpsOverlayDetailsVisible => _settingsCodec.IsFpsOverlayEnabled(SelectedShowFpsMode);

    public bool IsFsr411Visible { get; private set; }

    public string NormalizeVariant(string? value)
    {
        return _settingsCodec.NormalizeVariant(value);
    }

    public string NormalizeShowFpsMode(string? value)
    {
        return _settingsCodec.NormalizeShowFpsMode(value);
    }

    public string NormalizeAutoValue(string? value)
    {
        return _settingsCodec.NormalizeAutoValue(value);
    }

    public string NormalizeFpsOverlayTypeSelection(string? value)
    {
        return _settingsCodec.NormalizeFpsOverlayTypeSelection(value);
    }

    public string NormalizeFpsOverlayPositionSelection(string? value)
    {
        return _settingsCodec.NormalizeFpsOverlayPositionSelection(value);
    }

    public string NormalizeFramerateLimitSelection(string? value)
    {
        return _settingsCodec.NormalizeFramerateLimitSelection(value);
    }

    public string NormalizeFsr411Mode(string? value)
    {
        return _settingsCodec.NormalizeFsr411Mode(value);
    }

    public OptiScalerFsr411BundleStateChange ApplyGpuBundleKey(string? gpuBundleKey)
    {
        var nextVisible = OptiScalerFsr411Policy.ShouldShowMenu(gpuBundleKey);
        var visibilityChanged = IsFsr411Visible != nextVisible;
        IsFsr411Visible = nextVisible;
        var modeChanged = _draftState.NormalizeFsr411ForBundle(gpuBundleKey);
        return new OptiScalerFsr411BundleStateChange(visibilityChanged, modeChanged);
    }

    public void ApplySavedSettings(
        string selectedVariant,
        OptiScalerCommonIniSettingsDocument? commonIniSettings)
    {
        _draftState.ApplySavedState(selectedVariant, commonIniSettings);
    }

    public void SetSavedVariant(string selectedVariant)
    {
        _draftState.SavedOptiScalerVariantOption = selectedVariant;
    }

    public void DiscardChanges()
    {
        _draftState.DiscardChanges();
    }

    public OptiScalerSectionSavePayload BuildSavePayload()
    {
        var normalizedVariant = NormalizeVariant(SelectedOptiScalerVariantOption);
        var persistedDraft = _draftState.BuildPersistableDraft();
        var document = _settingsCodec.ToDocument(persistedDraft);

        return new OptiScalerSectionSavePayload(
            normalizedVariant,
            persistedDraft,
            document);
    }

    public void ApplySavedPayload(OptiScalerSectionSavePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        _draftState.ApplyCurrentSelectionAsSaved(payload.PersistedDraft, payload.SelectedVariant);
        _draftState.SelectedFramerateLimit = payload.PersistedDraft.FramerateLimit;
    }
}

internal sealed record OptiScalerSectionSavePayload(
    string SelectedVariant,
    OptiScalerCommonIniSettingsDraft PersistedDraft,
    OptiScalerCommonIniSettingsDocument Document);

internal sealed record OptiScalerFsr411BundleStateChange(
    bool VisibilityChanged,
    bool ModeChanged);
