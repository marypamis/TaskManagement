using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TaskManagement.API.Authorization;
using TaskManagement.API.Data;
using TaskManagement.API.Middleware;
using TaskManagement.API.Models;
using TaskManagement.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Allow React dev server to call this API during local development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:3000",
                "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Identity Core: UserManager.CreateAsync hashes passwords via IPasswordHasher (PBKDF2 + salt).
builder.Services.AddIdentityCore<AppUser>(options =>
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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing in configuration.");

// Keep custom JWT claim names (TenantId, UserId, Role) readable on HttpContext.User.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");

                logger.LogWarning(
                    context.Exception,
                    "JWT authentication failed for {Path}",
                    context.Request.Path);

                return Task.CompletedTask;
            }
        };
    });

// RBAC policies mirror [Authorize(Roles = ...)] on controllers; kept for consistency/extension.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(AppRoles.Admin));

    options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.User));

    options.FallbackPolicy = null;
});

var app = builder.Build();

// Apply migrations and seed required reference data (tenants for registration).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var migrateLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
    try
    {
        db.Database.Migrate();
        migrateLogger.LogInformation("Database migrations applied successfully.");
        await DbSeeder.SeedTenantsAsync(db, migrateLogger);
    }
    catch (Exception ex)
    {
        migrateLogger.LogError(ex, "Failed to apply database migrations or seed data.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

// Skip HTTPS redirect in Development: the http profile listens on :5016 only.
// Redirecting to https://7035 drops the Authorization header on follow-up requests (401 on /api/Tasks).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ReactDev");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<UnauthorizedLoggingMiddleware>();

app.MapControllers();

app.Run();
