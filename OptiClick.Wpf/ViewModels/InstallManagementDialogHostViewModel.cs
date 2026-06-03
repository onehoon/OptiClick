using System.Collections.Generic;
using System.Windows;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels;

public sealed class InstallManagementDialogViewModel : ViewModelBase
{
    public InstallManagementDialogViewModel(InstallManagementDialogRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public InstallManagementDialogRequest Request { get; }

    public string Title => Request.Title;
    public string Message => Request.Message;
    public string DestructiveText => Request.DestructiveText;
    public string PrimaryText => Request.PrimaryText;
    public string CancelText => Request.CancelText;
}

public sealed class InstallManagementDialogHostViewModel : ViewModelBase
{
    private readonly object _syncRoot = new();
    private readonly Queue<PendingDialog> _pendingQueue = new();
    private PendingDialog? _activeDialog;
    private bool _isOpen;
    private InstallManagementDialogViewModel? _currentDialog;

    public InstallManagementDialogHostViewModel()
    {
        PrimaryCommand = new RelayCommand(_ => CompleteActiveDialog(InstallManagementDialogResult.ContinueInstall), _ => IsOpen);
        DestructiveCommand = new RelayCommand(_ => CompleteActiveDialog(InstallManagementDialogResult.Uninstall), _ => IsOpen);
        CancelCommand = new RelayCommand(_ => CompleteActiveDialog(InstallManagementDialogResult.Cancel), _ => IsOpen);
        CloseCommand = new RelayCommand(_ => CompleteActiveDialog(InstallManagementDialogResult.Cancel), _ => IsOpen);
        OverlayClickCommand = new RelayCommand(_ => CompleteActiveDialog(InstallManagementDialogResult.Cancel), _ => IsOpen);
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (SetProperty(ref _isOpen, value))
            {
                OnPropertyChanged(nameof(OverlayVisibility));
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    public Visibility OverlayVisibility => IsOpen ? Visibility.Visible : Visibility.Collapsed;

    public InstallManagementDialogViewModel? CurrentDialog
    {
        get => _currentDialog;
        private set
        {
            if (SetProperty(ref _currentDialog, value))
            {
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    public RelayCommand PrimaryCommand { get; }

    public RelayCommand DestructiveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand CloseCommand { get; }

    public RelayCommand OverlayClickCommand { get; }

    public Task<InstallManagementDialogResult> ShowAsync(
        InstallManagementDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pending = new PendingDialog(request);

        if (cancellationToken.CanBeCanceled)
        {
            pending.CancellationRegistration = cancellationToken.Register(
                static state =>
                {
                    var tuple = (Tuple<InstallManagementDialogHostViewModel, PendingDialog>)state!;
                    tuple.Item1.CancelPendingDialog(tuple.Item2);
                },
                Tuple.Create(this, pending));
        }

        lock (_syncRoot)
        {
            _pendingQueue.Enqueue(pending);
            if (_activeDialog is null)
            {
                ActivateNextDialog();
            }
        }

        return pending.Task;
    }

    public bool HandleEscapeKey()
    {
        if (!IsOpen)
        {
            return false;
        }

        CompleteActiveDialog(InstallManagementDialogResult.Cancel);
        return true;
    }

    private void CompleteActiveDialog(InstallManagementDialogResult result)
    {
        PendingDialog? completedDialog;
        lock (_syncRoot)
        {
            completedDialog = _activeDialog;
            if (completedDialog is null)
            {
                return;
            }

            _activeDialog = null;
            ActivateNextDialog();
        }

        completedDialog.Dispose();
        completedDialog.TrySetResult(result);
    }

    private void CancelPendingDialog(PendingDialog pending)
    {
        bool wasActive;
        lock (_syncRoot)
        {
            wasActive = ReferenceEquals(_activeDialog, pending);
            if (!wasActive)
            {
                if (_pendingQueue.Count == 0)
                {
                    return;
                }

                var retained = new Queue<PendingDialog>(_pendingQueue.Count);
                while (_pendingQueue.Count > 0)
                {
                    var queued = _pendingQueue.Dequeue();
                    if (ReferenceEquals(queued, pending))
                    {
                        continue;
                    }

                    retained.Enqueue(queued);
                }

                while (retained.Count > 0)
                {
                    _pendingQueue.Enqueue(retained.Dequeue());
                }
            }
        }

        if (wasActive)
        {
            CompleteActiveDialog(InstallManagementDialogResult.Cancel);
            return;
        }

        pending.Dispose();
        pending.TrySetResult(InstallManagementDialogResult.Cancel);
    }

    private void ActivateNextDialog()
    {
        if (_pendingQueue.Count == 0)
        {
            CurrentDialog = null;
            IsOpen = false;
            return;
        }

        var pending = _pendingQueue.Dequeue();
        _activeDialog = pending;
        CurrentDialog = new InstallManagementDialogViewModel(pending.Request);
        IsOpen = true;
    }

    private void RaiseCommandCanExecuteChanged()
    {
        PrimaryCommand.RaiseCanExecuteChanged();
        DestructiveCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        CloseCommand.RaiseCanExecuteChanged();
        OverlayClickCommand.RaiseCanExecuteChanged();
    }

    private sealed class PendingDialog : IDisposable
    {
        private readonly TaskCompletionSource<InstallManagementDialogResult> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingDialog(InstallManagementDialogRequest request)
        {
            Request = request;
        }

        public InstallManagementDialogRequest Request { get; }

        public CancellationTokenRegistration CancellationRegistration { get; set; }

        public Task<InstallManagementDialogResult> Task => _completionSource.Task;

        public void TrySetResult(InstallManagementDialogResult result)
        {
            _completionSource.TrySetResult(result);
        }

        public void Dispose()
        {
            CancellationRegistration.Dispose();
        }
    }
}
