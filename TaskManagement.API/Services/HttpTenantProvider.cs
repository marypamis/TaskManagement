namespace TaskManagement.API.Services;

public class HttpTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? TenantId
    {
        get
        {
            // TenantId comes from the JWT issued at login; never from the request body.
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;

            if (!int.TryParse(tenantClaim, out var tenantId) || tenantId <= 0)
            {
                return null;
            }

            return tenantId;
        }
    }
}
