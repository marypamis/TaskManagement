using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.API.Authorization;
using TaskManagement.API.Data;
using TaskManagement.API.Models;
using TaskManagement.API.Models.Auth;
using TaskManagement.API.Services;
using Xunit;

namespace TaskManagement.API.Tests;

/// <summary>
/// Tests for registration and login business logic.
///
/// Why test auth logic:
/// - Registration and password hashing are security-critical paths
/// - Bugs here expose users to credential theft or lockout
/// - Automated tests catch regressions when Identity or JWT config changes
/// </summary>
public class AuthRegistrationTests : IDisposable
{
    private readonly string _databaseName = $"AuthTests_{Guid.NewGuid()}";
    private readonly ServiceProvider _serviceProvider;

    public AuthRegistrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName));

        services.AddScoped<ITenantProvider>(_ => new NullTenantProvider());

        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 4;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddUserManager<UserManager<AppUser>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-minimum-32-characters-long",
                ["Jwt:Issuer"] = "TaskManagement.Test",
                ["Jwt:Audience"] = "TaskManagement.Test",
                ["Jwt:DurationInMinutes"] = "60"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IAuthService, AuthService>();

        _serviceProvider = services.BuildServiceProvider();
        SeedTenants();
    }

    private void SeedTenants()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        if (!db.Tenants.Any())
        {
            db.Tenants.Add(new Tenant { Id = 1, Name = "Tenant One" });
            db.Tenants.Add(new Tenant { Id = 2, Name = "Tenant Two" });
            db.SaveChanges();
        }
    }

    private IAuthService CreateAuthService() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IAuthService>();

    private AppDbContext CreateDbContext() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsSuccess()
    {
        var authService = CreateAuthService();

        var result = await authService.RegisterAsync(new RegisterDto
        {
            Username = "alice",
            Password = "1234",
            Role = AppRoles.Admin,
            TenantId = 1
        });

        Assert.True(result.Succeeded, result.ErrorMessage ?? "Registration failed.");
        Assert.NotNull(result.User);
        Assert.Equal("alice", result.User!.Username);
        Assert.Equal(AppRoles.Admin, result.User.Role);
        Assert.Equal(1, result.User.TenantId);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsernameSameTenant_ReturnsFailure()
    {
        var authService = CreateAuthService();

        var dto = new RegisterDto
        {
            Username = "bob",
            Password = "1234",
            Role = AppRoles.User,
            TenantId = 1
        };

        var first = await authService.RegisterAsync(dto);
        var second = await authService.RegisterAsync(dto);

        Assert.True(first.Succeeded, first.ErrorMessage);
        Assert.False(second.Succeeded);
        Assert.Contains("already exists", second.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashed_NotPlainText()
    {
        const string plainPassword = "MySecretPassword123";
        var authService = CreateAuthService();

        var result = await authService.RegisterAsync(new RegisterDto
        {
            Username = "carol",
            Password = plainPassword,
            Role = AppRoles.User,
            TenantId = 1
        });

        Assert.True(result.Succeeded, result.ErrorMessage);

        using var db = CreateDbContext();
        var storedUser = db.Users
            .IgnoreQueryFilters()
            .Single(u => u.UserName == "carol");

        // Identity PBKDF2 hashes start with "AQAAAA" — never equal to plain password.
        Assert.NotEqual(plainPassword, storedUser.PasswordHash);
        Assert.StartsWith("AQAAAA", storedUser.PasswordHash);
    }

    [Fact]
    public async Task LoginAsync_AfterRegistration_ReturnsJwtToken()
    {
        const string username = "dave";
        const string password = "5678";
        var authService = CreateAuthService();

        var registerResult = await authService.RegisterAsync(new RegisterDto
        {
            Username = username,
            Password = password,
            Role = AppRoles.User,
            TenantId = 1
        });

        Assert.True(registerResult.Succeeded, registerResult.ErrorMessage);

        var loginResponse = await authService.LoginAsync(new LoginRequest
        {
            Username = username,
            Password = password
        });

        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse!.Token));
    }

    [Fact]
    public async Task RegisterAsync_SameUsernameDifferentTenants_BothSucceed()
    {
        var authService = CreateAuthService();

        var tenant1 = await authService.RegisterAsync(new RegisterDto
        {
            Username = "shared",
            Password = "1234",
            Role = AppRoles.User,
            TenantId = 1
        });

        var tenant2 = await authService.RegisterAsync(new RegisterDto
        {
            Username = "shared",
            Password = "1234",
            Role = AppRoles.User,
            TenantId = 2
        });

        Assert.True(tenant1.Succeeded, tenant1.ErrorMessage);
        Assert.True(tenant2.Succeeded, tenant2.ErrorMessage);
        Assert.Equal(1, tenant1.User!.TenantId);
        Assert.Equal(2, tenant2.User!.TenantId);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private sealed class NullTenantProvider : ITenantProvider
    {
        public int? TenantId => null;
    }
}
