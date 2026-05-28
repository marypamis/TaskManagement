using System.Security.Claims;
using TaskManagement.API.Authorization;

namespace TaskManagement.API.Services;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<HttpCurrentUserContext> _logger;

    public HttpCurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        ITenantProvider tenantProvider,
        ILogger<HttpCurrentUserContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public int? TenantId
    {
        get
        {
            var fromProvider = _tenantProvider.TenantId;
            if (fromProvider is > 0)
            {
                return fromProvider;
            }

            // Fallback: read TenantId directly from JWT (never from request body).
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            return int.TryParse(claim, out var tenantId) && tenantId > 0 ? tenantId : null;
        }
    }

    public int? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
            return int.TryParse(claim, out var userId) && userId > 0 ? userId : null;
        }
    }

    public string? Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return null;
            }

            // AuthService emits both "Role" and ClaimTypes.Role; check both for consistency.
            return user.FindFirst("Role")?.Value
                ?? user.FindFirst(ClaimTypes.Role)?.Value;
        }
    }

    public bool IsAdmin =>
        string.Equals(Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

    public bool IsUser =>
        string.Equals(Role, AppRoles.User, StringComparison.OrdinalIgnoreCase);

    public bool HasValidRole => IsAdmin || IsUser;

    public void RequireCanReadTasks()
    {
        if (!TryGetTenantAndUser(out _, out _))
        {
            _logger.LogWarning("Read denied. Missing or invalid TenantId/UserId claims.");
            throw new UnauthorizedAccessException("Valid TenantId and UserId claims are required.");
        }

        // Admin and User may view tasks, but only within their own tenant (enforced by queries/filters).
        if (!HasValidRole)
        {
            _logger.LogWarning("Read denied. Invalid role: {Role}", Role ?? "(none)");
            throw new UnauthorizedAccessException("Admin or User role is required to view tasks.");
        }
    }

    public void RequireCanModifyTasks()
    {
        if (!TryGetTenantAndUser(out var tenantId, out var userId))
        {
            _logger.LogWarning("Write denied. Missing or invalid TenantId/UserId claims.");
            throw new UnauthorizedAccessException("Valid TenantId and UserId claims are required.");
        }

        // User role is read-only; only Admin may change task data.
        if (!IsAdmin)
        {
            _logger.LogWarning(
                "Write denied. User {UserId} with role {Role} attempted a restricted operation in tenant {TenantId}.",
                userId,
                Role ?? "(none)",
                tenantId);
            throw new UnauthorizedAccessException("Admin role is required to create, update, or delete tasks.");
        }
    }

    public bool TryGetTenantAndUser(out int tenantId, out int userId)
    {
        tenantId = TenantId ?? 0;
        userId = UserId ?? 0;
        return tenantId > 0 && userId > 0;
    }
}
