using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels;

public sealed class DialogHostViewModel : ViewModelBase
{
    private readonly object _syncRoot = new();
    private readonly Queue<PendingDialog> _pendingQueue = new();
    private PendingDialog? _activeDialog;
    private bool _isOpen;
    private DialogRequestViewModel? _currentDialog;

    public DialogHostViewModel()
    {
        PrimaryCommand = new RelayCommand(_ => CompleteWithPrimary(), _ => CurrentDialog?.IsPrimaryEnabled == true);
        SecondaryCommand = new RelayCommand(_ => CompleteWithSecondary(), _ => CurrentDialog?.HasSecondaryButton == true);
        CloseCommand = new RelayCommand(_ => CompleteWithClose(), _ => CurrentDialog?.CanClose == true);
        OverlayClickCommand = new RelayCommand(_ => TryCloseFromOverlay(), _ => CurrentDialog?.CloseOnOverlayClick == true);
        AcknowledgeCommand = new RelayCommand(
            parameter =>
            {
                if (CurrentDialog is null)
                {
                    return;
                }

                var acknowledged = parameter as bool? ?? false;
                CurrentDialog.IsAcknowledged = acknowledged;
            },
            _ => CurrentDialog?.RequiresAcknowledgement == true);
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (SetProperty(ref _isOpen, value))
            {
                OnPropertyChanged(nameof(OverlayVisibility));
            }
        }
    }

    public Visibility OverlayVisibility => IsOpen ? Visibility.Visible : Visibility.Collapsed;

    public DialogRequestViewModel? CurrentDialog
    {
        get => _currentDialog;
        private set
        {
            if (ReferenceEquals(_currentDialog, value))
            {
                return;
            }

            if (_currentDialog is not null)
            {
                _currentDialog.PropertyChanged -= OnCurrentDialogPropertyChanged;
            }

            _currentDialog = value;
            OnPropertyChanged();

            if (_currentDialog is not null)
            {
                _currentDialog.PropertyChanged += OnCurrentDialogPropertyChanged;
            }

            RaiseCommandCanExecuteChanged();
        }
    }

    public RelayCommand PrimaryCommand { get; }

    public RelayCommand SecondaryCommand { get; }

    public RelayCommand CloseCommand { get; }

    public RelayCommand OverlayClickCommand { get; }

    public RelayCommand AcknowledgeCommand { get; }

    public Task<AppDialogResult> ShowAsync(
        AppDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        var pending = new PendingDialog(request);

        if (cancellationToken.CanBeCanceled)
        {
            pending.CancellationRegistration = cancellationToken.Register(
                static state =>
                {
                    var tuple = (Tuple<DialogHostViewModel, PendingDialog>)state!;
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

        if (CurrentDialog?.CanClose != true)
        {
            return true;
        }

        CompleteWithClose();
        return true;
    }

    private void CompleteWithPrimary()
    {
        var result = CurrentDialog?.PrimaryResult ?? AppDialogResult.None;
        CompleteActiveDialog(result);
    }

    private void CompleteWithSecondary()
    {
        var result = CurrentDialog?.SecondaryResult ?? AppDialogResult.Cancel;
        CompleteActiveDialog(result);
    }

    private void CompleteWithClose()
    {
        CompleteActiveDialog(AppDialogResult.Close);
    }

    private void TryCloseFromOverlay()
    {
        if (CurrentDialog?.CloseOnOverlayClick != true)
        {
            return;
        }

        CompleteActiveDialog(AppDialogResult.Close);
    }

    private void CompleteActiveDialog(AppDialogResult result)
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
            CompleteActiveDialog(AppDialogResult.Cancel);
            return;
        }

        pending.Dispose();
        pending.TrySetResult(AppDialogResult.Cancel);
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
        CurrentDialog = new DialogRequestViewModel(pending.Request);
        IsOpen = true;
    }

    private void OnCurrentDialogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DialogRequestViewModel.IsPrimaryEnabled)
            or nameof(DialogRequestViewModel.RequiresAcknowledgement)
            or nameof(DialogRequestViewModel.HasSecondaryButton)
            or nameof(DialogRequestViewModel.CanClose)
            or nameof(DialogRequestViewModel.CloseOnOverlayClick))
        {
            RaiseCommandCanExecuteChanged();
        }
    }

    private void RaiseCommandCanExecuteChanged()
    {
        PrimaryCommand.RaiseCanExecuteChanged();
        SecondaryCommand.RaiseCanExecuteChanged();
        CloseCommand.RaiseCanExecuteChanged();
        OverlayClickCommand.RaiseCanExecuteChanged();
        AcknowledgeCommand.RaiseCanExecuteChanged();
    }

    private sealed class PendingDialog : IDisposable
    {
        private readonly TaskCompletionSource<AppDialogResult> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingDialog(AppDialogRequest request)
        {
            Request = request;
        }

        public AppDialogRequest Request { get; }

        public CancellationTokenRegistration CancellationRegistration { get; set; }

        public Task<AppDialogResult> Task => _completionSource.Task;

        public void TrySetResult(AppDialogResult result)
        {
            _completionSource.TrySetResult(result);
        }

        public void Dispose()
        {
            CancellationRegistration.Dispose();
        }
    }
}
