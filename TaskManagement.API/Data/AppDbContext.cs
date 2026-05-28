using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaskManagement.API.Models;
using TaskManagement.API.Services;

namespace TaskManagement.API.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    private readonly ITenantProvider _tenantProvider;
    private int CurrentTenantId => _tenantProvider.TenantId ?? -1;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }

    public async Task<List<TaskItem>> GetTasksByTenantFromStoredProcedureAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        return await Tasks
            .FromSqlInterpolated($"EXEC GetTasksByTenant @TenantId = {tenantId}")
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map Identity user to existing Users table/column names (no schema rename required).
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.UserName).HasColumnName("Username");
            entity.HasQueryFilter(u => u.TenantId == CurrentTenantId);

            // Usernames are unique per tenant, not globally (multi-tenant support).
            entity.HasIndex(u => new { u.NormalizedUserName, u.TenantId })
                .IsUnique()
                .HasDatabaseName("IX_Users_NormalizedUserName_TenantId");
        });

        modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasQueryFilter(t => t.TenantId == CurrentTenantId && !t.IsDeleted);

            entity.Property(t => t.Title).IsRequired();
            entity.Property(t => t.Description).IsRequired();
            entity.Property(t => t.IsCompleted).IsRequired();
            entity.Property(t => t.IsDeleted).IsRequired();
        });
    }

    public override int SaveChanges()
    {
        ApplyTenantRules();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantRules()
    {
        if (CurrentTenantId <= 0)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not AppUser && entry.Entity is not TaskItem)
            {
                continue;
            }

            var entityTenantId = GetTenantId(entry);
            if (entry.State == EntityState.Added)
            {
                SetTenantId(entry, CurrentTenantId);
                continue;
            }

            if ((entry.State == EntityState.Modified || entry.State == EntityState.Deleted) && entityTenantId != CurrentTenantId)
            {
                throw new UnauthorizedAccessException("Cross-tenant data access is not allowed.");
            }
        }
    }

    private static int GetTenantId(EntityEntry entry)
    {
        return entry.Entity switch
        {
            AppUser user => user.TenantId,
            TaskItem task => task.TenantId,
            _ => 0
        };
    }

    private static void SetTenantId(EntityEntry entry, int tenantId)
    {
        switch (entry.Entity)
        {
            case AppUser user:
                user.TenantId = tenantId;
                break;
            case TaskItem task:
                task.TenantId = tenantId;
                break;
        }
    }
}
