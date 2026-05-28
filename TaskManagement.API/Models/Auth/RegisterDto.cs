namespace TaskManagement.API.Models.Auth;

/// <summary>
/// Registration request payload for POST /api/Auth/register.
/// Password is sent once over HTTPS and hashed server-side — never stored as plain text.
/// </summary>
public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TenantId { get; set; }
}
