using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Models;

namespace TaskManagement.API.Data;

/// <summary>
/// Seeds reference data required for registration (tenants must exist before users can register).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedTenantsAsync(AppDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Tenants.AddRange(
            new Tenant { Id = 1, Name = "Default Tenant" },
            new Tenant { Id = 2, Name = "Tenant Two" });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default tenants (Id 1 and 2).");
    }
}
