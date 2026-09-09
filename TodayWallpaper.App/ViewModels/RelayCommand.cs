using System.Windows.Input;

namespace TodayWallpaper.App.ViewModels;

/// <summary>
/// A command whose sole purpose is to relay its functionality to other
/// objects by invoking delegates.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Func<Task>? _asyncExecute;
    private readonly Action? _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> asyncExecute, Func<bool>? canExecute = null)
    {
        _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting) return false;
        return _canExecute == null || _canExecute();
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        if (_asyncExecute != null)
        {
            try
            {
                _isExecuting = true;
                CommandManager.InvalidateRequerySuggested();
                await _asyncExecute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
        else
        {
            _execute?.Invoke();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A generic command that relays its functionality with a typed parameter.
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Func<T?, Task>? _asyncExecute;
    private readonly Action<T?>? _execute;
    private readonly Predicate<T?>? _canExecute;
    private bool _isExecuting;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Func<T?, Task> asyncExecute, Predicate<T?>? canExecute = null)
    {
        _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting) return false;
        if (parameter is null && typeof(T).IsValueType)
            return _canExecute == null || _canExecute(default);

        return _canExecute == null || _canExecute((T?)parameter);
    }

    public async void Execute(object? parameter)
    {
        var typedParam = parameter is T val ? val : default;
        if (!CanExecute(typedParam)) return;

        if (_asyncExecute != null)
        {
            try
            {
                _isExecuting = true;
                CommandManager.InvalidateRequerySuggested();
                await _asyncExecute(typedParam);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
        else
        {
            _execute?.Invoke(typedParam);
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
