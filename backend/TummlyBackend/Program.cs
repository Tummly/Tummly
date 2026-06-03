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

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    );
});

var app = builder.Build();

/*
 =========================================
 CREATE DEFAULT ADMIN
 =========================================
*/

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<ApplicationDbContext>();

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
}


/*
 =========================================
 MIDDLEWARE
 =========================================
*/

// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

/*
 =========================================
 MAP CONTROLLERS
 =========================================
*/

app.MapControllers();

app.Run();