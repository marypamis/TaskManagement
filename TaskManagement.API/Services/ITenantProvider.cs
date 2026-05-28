namespace TaskManagement.API.Services;

public interface ITenantProvider
{
    int? TenantId { get; }
}
