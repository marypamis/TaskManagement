namespace TaskManagement.API.Services;

/// <summary>
/// Reads identity and role from the current JWT for tenant isolation and RBAC.
/// </summary>
public interface ICurrentUserContext
{
    int? TenantId { get; }
    int? UserId { get; }
    string? Role { get; }
    bool IsAdmin { get; }
    bool IsUser { get; }
    bool HasValidRole { get; }

    /// <summary>Admin or User may read tasks within their tenant.</summary>
    void RequireCanReadTasks();

    /// <summary>Only Admin may create, update, or delete tasks.</summary>
    void RequireCanModifyTasks();

    bool TryGetTenantAndUser(out int tenantId, out int userId);
}
