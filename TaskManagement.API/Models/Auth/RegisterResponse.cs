namespace TaskManagement.API.Models.Auth;

/// <summary>
/// Successful registration response (does not include password or hash).
/// </summary>
public class RegisterResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TenantId { get; set; }
}
