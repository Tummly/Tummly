using Microsoft.EntityFrameworkCore;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

/*
 =========================================
 CONTROLLERS
 =========================================
*/

builder.Services.AddControllers();

builder.Services
    .AddFluentValidationAutoValidation();

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>();

/*
 =========================================
 DATABASE
 =========================================
*/

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

/*
 =========================================
 EMAIL SETTINGS
 =========================================
*/

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

/*
 =========================================
 JWT SETTINGS
 =========================================
*/

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings")
);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

/*
 =========================================
 JWT AUTHENTICATION
 =========================================
*/

var jwtSettings =
    builder.Configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>()
    ?? throw new Exception(
        "JWT settings are missing."
    );

if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    throw new Exception("JWT secret is missing.");
}

var key = Encoding.UTF8.GetBytes(
    jwtSettings.Secret
);

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )

    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    jwtSettings.Issuer,

                ValidAudience =
                    jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(key),

                ClockSkew =
                    TimeSpan.Zero
            };
            });
                   builder.Services.AddAuthorization();

/*
 =========================================
 SERVICES
 =========================================
*/

builder.Services.AddScoped<ITrialService, TrialService>();

builder.Services.AddHttpClient(
    "Resend",
    client =>
    {
        client.BaseAddress =
            new Uri("https://api.resend.com/");

        client.Timeout =
            TimeSpan.FromSeconds(30);
    }
);

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IAdminService, AdminService>();

/*
 =========================================
 CORS
 =========================================
*/

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray();

if (allowedOrigins is null or { Length: 0 })
{
    allowedOrigins = ["http://localhost:5173"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    );
});

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/health/ready", async (ApplicationDbContext db) =>
{
    try
    {
        if (await db.Database.CanConnectAsync())
        {
            return Results.Ok(new { status = "ready" });
        }
    }
    catch
    {
        // fall through to 503
    }

    return Results.Json(
        new { status = "not_ready", message = "Database connection failed" },
        statusCode: StatusCodes.Status503ServiceUnavailable
    );
});

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = InitializeDatabaseAsync(app.Services, builder.Configuration);
});

app.Run();

static async Task InitializeDatabaseAsync(
    IServiceProvider services,
    IConfiguration configuration
)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseInit");
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        logger.LogError(
            "ConnectionStrings__DefaultConnection is missing. Set it in Railway variables."
        );
        return;
    }

    const int maxAttempts = 30;
    const int delayMs = 5000;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            {
                await context.Database.MigrateAsync();
            }

            if (!await context.Admins.AnyAsync())
            {
                var admin = new TummlyBackend.Models.Admin
                {
                    FullName = "Tummly Admin",
                    Email = "admin@tummly.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin",
                    IsActive = true
                };

                context.Admins.Add(admin);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Database initialized successfully.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Database init attempt {Attempt}/{MaxAttempts} failed",
                attempt,
                maxAttempts
            );

            if (attempt >= maxAttempts)
            {
                logger.LogError(
                    "Database initialization failed after all retries. Check TummlyDb is running and ConnectionStrings__DefaultConnection is correct."
                );
                return;
            }

            await Task.Delay(delayMs);
        }
    }
}
