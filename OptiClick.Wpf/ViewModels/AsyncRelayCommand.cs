using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OptiClick.Wpf.ViewModels;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, CancellationToken, Task> _executeAsync;
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<Exception>? _onException;
    private readonly bool _allowConcurrentExecutions;
    private int _isExecuting;
    private CancellationTokenSource? _executionCts;

    public AsyncRelayCommand(
        Func<object?, CancellationToken, Task> executeAsync,
        Predicate<object?>? canExecute = null,
        Action<Exception>? onException = null,
        bool allowConcurrentExecutions = false)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onException = onException;
        _allowConcurrentExecutions = allowConcurrentExecutions;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (!_allowConcurrentExecutions && Volatile.Read(ref _isExecuting) == 1)
        {
            return false;
        }

        return _canExecute?.Invoke(parameter) ?? true;
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter, CancellationToken cancellationToken = default)
    {
        if (!_allowConcurrentExecutions
            && Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
        {
            return;
        }

        if (!(_canExecute?.Invoke(parameter) ?? true))
        {
            if (!_allowConcurrentExecutions)
            {
                Interlocked.Exchange(ref _isExecuting, 0);
            }

            return;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executionCts = linkedCts;
        RaiseCanExecuteChanged();

        try
        {
            await _executeAsync(parameter, linkedCts.Token);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            // Ignore command cancellation.
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }
        finally
        {
            _executionCts = null;
            if (!_allowConcurrentExecutions)
            {
                Interlocked.Exchange(ref _isExecuting, 0);
            }

            RaiseCanExecuteChanged();
        }
    }

    public void Cancel()
    {
        _executionCts?.Cancel();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
