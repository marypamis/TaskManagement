using System.Windows;
using TaskManagement.Desktop.Services;
using TaskManagement.Desktop.ViewModels;

namespace TaskManagement.Desktop;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var tokenStore = new SessionTokenStore();
        var apiClient = new ApiClient(tokenStore);
        var authService = new AuthService(apiClient, tokenStore);
        var taskService = new TaskService(apiClient);

        var mainViewModel = new MainViewModel(authService, taskService);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
    }
}
