using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagement.API.Authorization;
using TaskManagement.API.Data;
using TaskManagement.API.Models;
using TaskManagement.API.Models.Auth;

namespace TaskManagement.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Registration flow (step-by-step):
    /// 1. Validate username, password, role, and tenantId
    /// 2. Ensure tenant exists in database
    /// 3. Check duplicate username within the same tenant
    /// 4. UserManager.CreateAsync(user, password) — Identity hashes password via IPasswordHasher (PBKDF2)
    /// 5. Persist user; PasswordHash column stores the hash, never the plain password
    ///
    /// Why UserManager.CreateAsync:
    /// - Official ASP.NET Identity API for user creation
    /// - Internally calls PasswordHasher.HashPassword before SaveChanges
    /// - Applies password validation rules and security stamps
    ///
    /// Why we never store plain passwords:
    /// - Plain text exposure is a critical security breach
    /// - Hashes are one-way; login verifies with CheckPasswordAsync / VerifyHashedPassword
    /// </summary>
    public async Task<RegisterResult> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return RegisterResult.Failure("Username and password are required.");
        }

        if (request.TenantId <= 0)
        {
            return RegisterResult.Failure("A valid TenantId is required.");
        }

        var role = request.Role?.Trim() ?? string.Empty;
        if (role != AppRoles.Admin && role != AppRoles.User)
        {
            return RegisterResult.Failure($"Role must be '{AppRoles.Admin}' or '{AppRoles.User}'.");
        }

        var tenantExists = await _dbContext.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
        {
            return RegisterResult.Failure($"Tenant {request.TenantId} does not exist.");
        }

        // Multi-tenant uniqueness: same username allowed in different tenants, not within one tenant.
        var duplicateInTenant = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(
                u => u.UserName == request.Username.Trim() && u.TenantId == request.TenantId,
                cancellationToken);

        if (duplicateInTenant)
        {
            _logger.LogWarning(
                "Registration failed. Duplicate username {Username} in tenant {TenantId}.",
                request.Username,
                request.TenantId);
            return RegisterResult.Failure("Username already exists for this tenant.");
        }

        var user = new AppUser
        {
            UserName = request.Username.Trim(),
            Role = role,
            TenantId = request.TenantId
        };

        // CreateAsync hashes the password and stores it in user.PasswordHash automatically.
        var identityResult = await _userManager.CreateAsync(user, request.Password);

        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for {Username}: {Errors}", request.Username, errors);
            return RegisterResult.Failure(errors);
        }

        _logger.LogInformation(
            "User registered. UserId: {UserId}, Username: {Username}, TenantId: {TenantId}, Role: {Role}",
            user.Id,
            user.UserName,
            user.TenantId,
            user.Role);

        return RegisterResult.Success(new RegisterResponse
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Role = user.Role,
            TenantId = user.TenantId
        });
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login failed. Username or password was not provided.");
            return null;
        }

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.UserName == request.Username, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login failed. User not found. Username: {Username}", request.Username);
            return null;
        }

        // CheckPasswordAsync uses the same Identity hasher that CreateAsync used at registration.
        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Login failed. Invalid password. Username: {Username}", request.Username);
            return null;
        }

        _logger.LogInformation(
            "Login successful. Username: {Username}, UserId: {UserId}, TenantId: {TenantId}, Role: {Role}",
            user.UserName,
            user.Id,
            user.TenantId,
            user.Role);

        return GenerateToken(user);
    }

    private LoginResponse GenerateToken(AppUser user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing in configuration.");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing in configuration.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing in configuration.");
        var durationValue = jwtSection["DurationInMinutes"] ?? jwtSection["ExpiresMinutes"];
        var expiresMinutes = int.TryParse(durationValue, out var minutes) ? minutes : 60;

        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);
        var username = user.UserName ?? string.Empty;

        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new("TenantId", user.TenantId.ToString()),
            new("Role", user.Role),
            new("Username", username),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, username)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt
        };
    }
}
