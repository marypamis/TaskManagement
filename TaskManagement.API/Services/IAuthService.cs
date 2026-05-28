using TaskManagement.API.Models.Auth;

namespace TaskManagement.API.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user with ASP.NET Identity password hashing via UserManager.CreateAsync.
    /// </summary>
    Task<RegisterResult> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);
}
