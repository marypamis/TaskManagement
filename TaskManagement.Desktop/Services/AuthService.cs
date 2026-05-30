using TaskManagement.Desktop.Models;

namespace TaskManagement.Desktop.Services;

public interface IAuthService
{
    Task<(bool Success, string? ErrorMessage)> LoginAsync(string username, string password);
    void Logout();
}

public class AuthService : IAuthService
{
    private readonly ApiClient _apiClient;
    private readonly SessionTokenStore _tokenStore;

    public AuthService(ApiClient apiClient, SessionTokenStore tokenStore)
    {
        _apiClient = apiClient;
        _tokenStore = tokenStore;
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username.Trim(),
            Password = password
        };

        var result = await _apiClient.PostAsync<LoginResponse>(
            "/api/Auth/login",
            request,
            requireAuth: false);

        if (!result.Success || result.Data is null || string.IsNullOrWhiteSpace(result.Data.Token))
        {
            return (false, result.ErrorMessage ?? "Invalid username or password.");
        }

        // marypamis: token stays in memory only for this desktop session
        _tokenStore.SetToken(result.Data.Token);
        return (true, null);
    }

    public void Logout()
    {
        _tokenStore.Clear();
    }
}
