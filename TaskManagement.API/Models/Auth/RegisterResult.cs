namespace TaskManagement.API.Models.Auth;

/// <summary>
/// Internal service result for registration (success or validation error).
/// </summary>
public class RegisterResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public RegisterResponse? User { get; init; }

    public static RegisterResult Success(RegisterResponse user) =>
        new() { Succeeded = true, User = user };

    public static RegisterResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
