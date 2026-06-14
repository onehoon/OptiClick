using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.ViewModels;

public sealed class SelectedGameActionViewModel : ViewModelBase
{
    private readonly Func<AppStrings> _stringsAccessor;
    private string _selectedGameTitle = "";
    private bool _hasSelection;
    private string _installButtonText = "";
    private bool _isInstallButtonEnabled;
    private string _installButtonReasonCode = InstallButtonReasonCodes.NoGameSelected;
    private bool _isInstallButtonBusy;
    private bool _isInstallButtonLoading;
    private bool _popupConfirmed;
    private bool _precheckRunning;
    private bool _precheckOk;
    private string _precheckError = "";
    private string _precheckResolvedDllName = "";
    private bool _hasPendingPopupRequests;
    private int _pendingPopupRequestCount;
    private string _currentPopupRequestKind = "";
    private string _currentPopupRequestBody = "";
    private AppStrings _strings = new AppStrings();

    public SelectedGameActionViewModel(Func<AppStrings>? stringsAccessor = null)
    {
        _stringsAccessor = stringsAccessor ?? (() => new AppStrings());
    }

    public string SelectedGameTitle
    {
        get => _selectedGameTitle;
        private set => SetProperty(ref _selectedGameTitle, value);
    }

    public bool HasSelection
    {
        get => _hasSelection;
        private set => SetProperty(ref _hasSelection, value);
    }

    public string InstallButtonText
    {
        get => _installButtonText;
        private set => SetProperty(ref _installButtonText, value);
    }

    public bool IsInstallButtonEnabled
    {
        get => _isInstallButtonEnabled;
        private set => SetProperty(ref _isInstallButtonEnabled, value);
    }

    public string InstallButtonReasonCode
    {
        get => _installButtonReasonCode;
        private set => SetProperty(ref _installButtonReasonCode, value);
    }

    public bool IsInstallButtonBusy
    {
        get => _isInstallButtonBusy;
        private set => SetProperty(ref _isInstallButtonBusy, value);
    }

    public bool IsInstallButtonLoading
    {
        get => _isInstallButtonLoading;
        private set => SetProperty(ref _isInstallButtonLoading, value);
    }

    public bool PopupConfirmed
    {
        get => _popupConfirmed;
        private set => SetProperty(ref _popupConfirmed, value);
    }

    public bool PrecheckRunning
    {
        get => _precheckRunning;
        private set => SetProperty(ref _precheckRunning, value);
    }

    public bool PrecheckOk
    {
        get => _precheckOk;
        private set => SetProperty(ref _precheckOk, value);
    }

    public string PrecheckError
    {
        get => _precheckError;
        private set => SetProperty(ref _precheckError, value);
    }

    public string PrecheckResolvedDllName
    {
        get => _precheckResolvedDllName;
        private set => SetProperty(ref _precheckResolvedDllName, value);
    }

    public bool HasPendingPopupRequests
    {
        get => _hasPendingPopupRequests;
        private set => SetProperty(ref _hasPendingPopupRequests, value);
    }

    public int PendingPopupRequestCount
    {
        get => _pendingPopupRequestCount;
        private set => SetProperty(ref _pendingPopupRequestCount, value);
    }

    public string CurrentPopupRequestKind
    {
        get => _currentPopupRequestKind;
        private set => SetProperty(ref _currentPopupRequestKind, value);
    }

    public string CurrentPopupRequestBody
    {
        get => _currentPopupRequestBody;
        private set => SetProperty(ref _currentPopupRequestBody, value);
    }

    public bool HasSelectedGame => HasSelection;
    public bool CanInstall => IsInstallButtonEnabled;

    public void ApplyLocalization(AppStrings strings)
    {
        _strings = strings ?? _strings;
        if (!HasSelection)
        {
            SelectedGameTitle = _strings.HomeSelectGameHint;
            InstallButtonText = _strings.InstallButtonInstall;
        }
    }

    public void Update(GameCardViewModel? selectedGame)
    {
        if (selectedGame is null)
        {
            HasSelection = false;
            SelectedGameTitle = _strings.HomeSelectGameHint;
            InstallButtonText = _strings.InstallButtonInstall;
            IsInstallButtonEnabled = false;
            InstallButtonReasonCode = InstallButtonReasonCodes.NoGameSelected;
            IsInstallButtonBusy = false;
            IsInstallButtonLoading = false;
            PopupConfirmed = false;
            PrecheckRunning = false;
            PrecheckOk = false;
            PrecheckError = "";
            PrecheckResolvedDllName = "";
            HasPendingPopupRequests = false;
            PendingPopupRequestCount = 0;
            CurrentPopupRequestKind = "";
            CurrentPopupRequestBody = "";
            OnPropertyChanged(nameof(HasSelectedGame));
            OnPropertyChanged(nameof(CanInstall));
            return;
        }

        HasSelection = true;
        SelectedGameTitle = selectedGame.Title;
        if (!PrecheckRunning)
        {
            InstallButtonText = _strings.InstallButtonInstall;
            IsInstallButtonEnabled = true;
            InstallButtonReasonCode = "";
            IsInstallButtonBusy = false;
            IsInstallButtonLoading = false;
        }
        OnPropertyChanged(nameof(HasSelectedGame));
        OnPropertyChanged(nameof(CanInstall));
    }

    public void ApplyPrecheckRunningIntermediate()
    {
        PopupConfirmed = false;
        PrecheckRunning = true;
        PrecheckOk = false;
        PrecheckError = "";
        PrecheckResolvedDllName = "";
        HasPendingPopupRequests = false;
        PendingPopupRequestCount = 0;
        CurrentPopupRequestKind = "";
        CurrentPopupRequestBody = "";
        InstallButtonText = _strings.InstallButtonLoading;
        IsInstallButtonEnabled = false;
        InstallButtonReasonCode = InstallButtonReasonCodes.InstallPrecheckRunning;
        IsInstallButtonBusy = false;
        IsInstallButtonLoading = false;
        OnPropertyChanged(nameof(CanInstall));
    }

    public void ApplySelectionBridgeState(ShellInstallSelectionState state)
    {
        PopupConfirmed = state.PopupConfirmed;
        PrecheckRunning = state.PrecheckRunning;
        PrecheckOk = state.PrecheckOk;
        PrecheckError = state.PrecheckError;
        PrecheckResolvedDllName = state.PrecheckResolvedDllName;

        var requests = state.PendingPopupRequests ?? Array.Empty<ShellPopupRequest>();
        HasPendingPopupRequests = requests.Count > 0;
        PendingPopupRequestCount = requests.Count;
        if (requests.Count > 0)
        {
            CurrentPopupRequestKind = requests[0].Kind.ToString();
            CurrentPopupRequestBody = requests[0].Message;
        }
        else
        {
            CurrentPopupRequestKind = "";
            CurrentPopupRequestBody = "";
        }

        var presentation = state.InstallButtonPresentation;
        InstallButtonText = string.IsNullOrWhiteSpace(presentation.Text)
            ? _strings.InstallButtonLoading
            : presentation.Text;
        IsInstallButtonEnabled = presentation.IsEnabled;
        InstallButtonReasonCode = presentation.ReasonCode;
        IsInstallButtonBusy = presentation.ShowInstalling;
        IsInstallButtonLoading = presentation.IsLoadingBlinkReason;
        OnPropertyChanged(nameof(CanInstall));
    }
}
