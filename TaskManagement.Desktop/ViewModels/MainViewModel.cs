using TaskManagement.Desktop.Helpers;
using TaskManagement.Desktop.Services;

namespace TaskManagement.Desktop.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ITaskService _taskService;
    private ViewModelBase _currentViewModel;

    public MainViewModel(IAuthService authService, ITaskService taskService)
    {
        _authService = authService;
        _taskService = taskService;
        _currentViewModel = CreateLoginViewModel();
    }

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    private LoginViewModel CreateLoginViewModel()
    {
        return new LoginViewModel(_authService, ShowDashboard);
    }

    private DashboardViewModel CreateDashboardViewModel()
    {
        return new DashboardViewModel(_taskService, _authService, ShowLogin);
    }

    private void ShowDashboard()
    {
        CurrentViewModel = CreateDashboardViewModel();
    }

    private void ShowLogin()
    {
        CurrentViewModel = CreateLoginViewModel();
    }
}
