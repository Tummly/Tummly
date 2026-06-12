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

/*
 =========================================
 DATABASE MIGRATIONS + DEFAULT ADMIN
 =========================================
*/

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<ApplicationDbContext>();

    const int maxAttempts = 12;
    const int delayMs = 5000;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            {
                context.Database.Migrate();
            }

            if (!context.Admins.Any())
            {
                var admin = new TummlyBackend.Models.Admin
                {
                    FullName = "Tummly Admin",

                    Email = "admin@tummly.com",

                    PasswordHash =
                        BCrypt.Net.BCrypt.HashPassword(
                            "Admin@123"
                        ),

                    Role = "Admin",

                    IsActive = true
                };

                context.Admins.Add(admin);

                context.SaveChanges();
            }

            break;
        }
        catch (Exception) when (attempt < maxAttempts)
        {
            Thread.Sleep(delayMs);
        }
    }
}


/*
 =========================================
 MIDDLEWARE
 =========================================
*/

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

/*
 =========================================
 ENDPOINTS
 =========================================
*/

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();
