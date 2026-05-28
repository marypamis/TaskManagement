using Microsoft.AspNetCore.Identity;

namespace TaskManagement.API.Models;

/// <summary>
/// Application user entity backed by ASP.NET Identity.
/// UserName maps to the existing "Username" database column.
/// Role and TenantId are custom fields for JWT and multi-tenancy.
/// </summary>
public class AppUser : IdentityUser<int>
{
    public string Role { get; set; } = string.Empty;
    public int TenantId { get; set; }
}
