using TaskManagement.Desktop.Helpers;
using TaskManagement.Desktop.Services;

namespace TaskManagement.Desktop.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly Action _onLoginSuccess;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthService authService, Action onLoginSuccess)
    {
        _authService = authService;
        _onLoginSuccess = onLoginSuccess;
        LoginCommand = new AsyncRelayCommand(_ => LoginAsync(), _ => CanLogin());
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ClearError();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ClearError();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    private async Task LoginAsync()
    {
        var result = await _authService.LoginAsync(Username, Password);

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage ?? "Login failed.";
            return;
        }

        _onLoginSuccess();
    }

    private void ClearError()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ErrorMessage = string.Empty;
        }
    }
}
