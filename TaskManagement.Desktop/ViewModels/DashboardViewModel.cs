using System.Collections.ObjectModel;
using TaskManagement.Desktop.Helpers;
using TaskManagement.Desktop.Models;
using TaskManagement.Desktop.Services;

namespace TaskManagement.Desktop.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly IAuthService _authService;
    private readonly Action _onLogout;
    private TaskItem? _selectedTask;
    private string _errorMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public DashboardViewModel(
        ITaskService taskService,
        IAuthService authService,
        Action onLogout)
    {
        _taskService = taskService;
        _authService = authService;
        _onLogout = onLogout;

        Tasks = new ObservableCollection<TaskItem>();
        CompleteTaskCommand = new AsyncRelayCommand(_ => CompleteSelectedTaskAsync(), _ => CanCompleteTask());
        LogoutCommand = new RelayCommand(_ => Logout());
        RefreshCommand = new AsyncRelayCommand(_ => LoadTasksAsync());

        _ = LoadTasksAsync();
    }

    public ObservableCollection<TaskItem> Tasks { get; }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                CompleteTaskCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public AsyncRelayCommand CompleteTaskCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand LogoutCommand { get; }

    private bool CanCompleteTask()
    {
        return SelectedTask is not null && !SelectedTask.IsCompleted && !IsBusy;
    }

    private async Task LoadTasksAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var result = await _taskService.GetTasksAsync();

        IsBusy = false;

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage ?? "Could not load tasks.";
            return;
        }

        Tasks.Clear();
        foreach (var task in result.Tasks)
        {
            Tasks.Add(task);
        }

        StatusMessage = $"{Tasks.Count} task(s) loaded.";
    }

    private async Task CompleteSelectedTaskAsync()
    {
        if (SelectedTask is null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var taskId = SelectedTask.Id;
        var result = await _taskService.CompleteTaskAsync(taskId);

        if (!result.Success)
        {
            IsBusy = false;
            ErrorMessage = result.ErrorMessage ?? "Could not complete task.";
            return;
        }

        // marypamis: Refreshing task list after update to keep UI in sync with backend state
        await LoadTasksAsync();
        IsBusy = false;
        StatusMessage = "Task marked as completed.";
    }

    private void Logout()
    {
        _authService.Logout();
        _onLogout();
    }
}
