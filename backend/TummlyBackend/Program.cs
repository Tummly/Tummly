using Microsoft.EntityFrameworkCore;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Hubs;
using TummlyBackend.Infrastructure;
using TummlyBackend.Interfaces;
using TummlyBackend.Middleware;
using TummlyBackend.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(
            new UtcDateTimeJsonConverter()
        );
        options.JsonSerializerOptions.Converters.Add(
            new UtcNullableDateTimeJsonConverter()
        );
    });

builder.Services
    .AddFluentValidationAutoValidation();

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddMemoryCache();

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

builder.Services.AddSingleton<DatabaseInitState>();

/*
 =========================================
 EMAIL SETTINGS
 =========================================
*/

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

builder.Services.Configure<TwilioSettings>(
    builder.Configuration.GetSection("TwilioSettings")
);

/*
 =========================================
 JWT SETTINGS
 =========================================
*/

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings")
);

builder.Services.Configure<IdealPostcodesSettings>(
    builder.Configuration.GetSection("IdealPostcodes")
);

builder.Services.AddQueryAttachmentStorage(builder.Configuration);

builder.Services.Configure<HelpCentreSettings>(
    builder.Configuration.GetSection("HelpCentre")
);

builder.Services.Configure<FeedbackClassificationSettings>(
    builder.Configuration.GetSection(FeedbackClassificationSettings.SectionName)
);

builder.Services.Configure<SpeechToTextSettings>(
    builder.Configuration.GetSection(SpeechToTextSettings.SectionName)
);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 55_000_000;
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

        // SignalR JS clients send JWT via accessTokenFactory → query access_token.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                OperatorSignalRHubs.TryAssignAccessTokenFromQuery(
                    context.Request,
                    token => context.Token = token
                );

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOperatorSignalR(builder.Configuration);

/*
 =========================================
 SERVICES
 =========================================
*/

builder.Services.AddScoped<ITrialService, TrialService>();

builder.Services.AddScoped<IProvisioningService, GuestLoopProvisioningService>();

builder.Services.AddScoped<ISmartGuestLinkService, SmartGuestLinkService>();

builder.Services.AddScoped<IOwnedLocationService, OwnedLocationService>();

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

builder.Services.AddHttpClient(
    "IdealPostcodes",
    client =>
    {
        var baseUrl = builder.Configuration["IdealPostcodes:BaseUrl"]
            ?? "https://api.ideal-postcodes.co.uk/v1/";

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }
);

builder.Services.AddScoped<IAddressLookupService, AddressLookupService>();

builder.Services.AddHttpClient(
    FeedbackClassificationStructuredOutput.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{FeedbackClassificationSettings.SectionName}:Endpoint"
        ];

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(
                endpoint.TrimEnd('/') + "/"
            );
        }

        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

var feedbackClassificationProvider =
    builder.Configuration[
        $"{FeedbackClassificationSettings.SectionName}:Provider"
    ]
    ?? "AzureOpenAI";

var useFakeFeedbackClassification =
    builder.Environment.IsEnvironment("Testing")
    || feedbackClassificationProvider.Equals(
        "Fake",
        StringComparison.OrdinalIgnoreCase
    );

if (useFakeFeedbackClassification)
{
    builder.Services.AddSingleton<FakeFeedbackClassificationProvider>();
    builder.Services.AddSingleton<IFeedbackClassificationProvider>(sp =>
        sp.GetRequiredService<FakeFeedbackClassificationProvider>()
    );
}
else
{
    builder.Services.AddSingleton<
        IFeedbackClassificationProvider,
        AzureOpenAIFeedbackClassificationProvider
    >();
}

builder.Services.AddHttpClient(
    AzureSpeechFastTranscription.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{SpeechToTextSettings.SectionName}:Endpoint"
        ];

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(
                endpoint.TrimEnd('/') + "/"
            );
        }

        // Guest clips are capped at 60s; leave headroom for Speech round-trip.
        client.Timeout = TimeSpan.FromSeconds(90);
    }
);

var speechToTextProvider =
    builder.Configuration[$"{SpeechToTextSettings.SectionName}:Provider"]
    ?? "AzureSpeech";

var useFakeSpeechToText =
    builder.Environment.IsEnvironment("Testing")
    || speechToTextProvider.Equals(
        "Fake",
        StringComparison.OrdinalIgnoreCase
    );

var useAzureSpeechToText = speechToTextProvider.Equals(
    "AzureSpeech",
    StringComparison.OrdinalIgnoreCase
);

if (useFakeSpeechToText)
{
    builder.Services.AddSingleton<FakeSpeechToTextProvider>();
    builder.Services.AddSingleton<ISpeechToTextProvider>(sp =>
        sp.GetRequiredService<FakeSpeechToTextProvider>()
    );
}
else if (useAzureSpeechToText)
{
    builder.Services.AddSingleton<
        ISpeechToTextProvider,
        AzureSpeechToTextProvider
    >();
}
else
{
    throw new InvalidOperationException(
        $"Unsupported SpeechToText:Provider '{speechToTextProvider}'. "
            + "Use AzureSpeech (default), Fake (tests/local), or wire OpenAI "
            + "transcriptions separately as the documented one-vendor alternative."
    );
}

builder.Services.AddSingleton<
    IFeedbackClassificationWork,
    FeedbackClassificationWork
>();
builder.Services.AddHostedService<FeedbackClassificationBackgroundService>();

builder.Services.AddHttpClient(
    SignInMetadataResolverHttpClient.Name,
    SignInMetadataResolverHttpClient.Configure
);

builder.Services.AddScoped<ISignInMetadataResolver, SignInMetadataResolver>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<ISmsService, TwilioVerifySmsService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<IHelpCentreService, HelpCentreService>();

builder.Services.AddScoped<ISupportService, SupportService>();

builder.Services.AddScoped<IOperatorNotificationsService, OperatorNotificationsService>();

builder.Services.AddSingleton<
    INotificationRealtimePublisher,
    SignalRNotificationRealtimePublisher
>();

builder.Services.AddSingleton<
    IFeedbackHomeRealtimePublisher,
    SignalRFeedbackHomeRealtimePublisher
>();

builder.Services.AddScoped<
    IActivationNotificationProducer,
    ActivationNotificationProducer
>();

builder.Services.AddScoped<ITrialReviewTransition, TrialReviewTransition>();

builder.Services.AddScoped<IActivationGate, ActivationGate>();

builder.Services.AddHostedService<
    OperatorSetupInvitationReminderBackgroundService
>();

builder.Services.AddHostedService<ActivationNotificationBackgroundService>();

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

app.UseForwardedHeaders();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseMiddleware<ActivationGateMiddleware>();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/health/ready", async (
    ApplicationDbContext db,
    DatabaseInitState initState
) =>
{
    var initStatus = initState.Status;
    if (initStatus != DatabaseInitStatus.Succeeded)
    {
        var message = initStatus switch
        {
            DatabaseInitStatus.Failed => "Database initialization failed",
            DatabaseInitStatus.InProgress => "Database initialization in progress",
            _ => "Database initialization not started"
        };

        return Results.Json(
            new { status = "not_ready", message },
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }

    try
    {
        if (!await db.Database.CanConnectAsync())
        {
            return Results.Json(
                new { status = "not_ready", message = "Database connection failed" },
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }

        // In-memory test hosts have no migration history; skip pending check.
        if (db.Database.IsRelational())
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                return Results.Json(
                    new
                    {
                        status = "not_ready",
                        message = "Pending EF migrations"
                    },
                    statusCode: StatusCodes.Status503ServiceUnavailable
                );
            }
        }

        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.Json(
            new { status = "not_ready", message = "Database readiness check failed" },
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
});

app.MapControllers();

app.MapOperatorHub<NotificationsHub>(
    OperatorSignalRHubs.NotificationsPath
);
app.MapOperatorHub<FeedbackHomeHub>(
    OperatorSignalRHubs.FeedbackHomePath
);

app.Lifetime.ApplicationStarted.Register(() =>
{
    var initState = app.Services.GetRequiredService<DatabaseInitState>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        // Integration tests use in-memory DB; skip migrate and mark ready.
        initState.MarkSucceeded();
        return;
    }

    _ = InitializeDatabaseAsync(
        app.Services,
        builder.Configuration,
        initState
    );
});

app.Run();

static async Task InitializeDatabaseAsync(
    IServiceProvider services,
    IConfiguration configuration,
    DatabaseInitState initState
)
{
    initState.MarkInProgress();

    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseInit");
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        logger.LogError(
            "ConnectionStrings__DefaultConnection is missing. Set it before starting the API."
        );
        initState.MarkFailed();
        Environment.Exit(1);
        return;
    }

    const int maxAttempts = 30;
    const int delayMs = 5000;
    var applyMigrations = configuration.GetValue<bool>(
        "Database:ApplyMigrationsOnStartup"
    );

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            if (applyMigrations)
            {
                await context.Database.MigrateAsync();
            }
            else if (!await context.Database.CanConnectAsync())
            {
                throw new InvalidOperationException(
                    "Cannot connect to the database."
                );
            }

            break;
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
                    "Database initialization failed after all retries. Check SQL is reachable and ConnectionStrings__DefaultConnection is correct."
                );
                initState.MarkFailed();
                Environment.Exit(1);
                return;
            }

            await Task.Delay(delayMs);
        }
    }

    // Seed Admin/Support is best-effort — does not gate /health/ready (ADR-0015).
    try
    {
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

        if (
            !await context.Admins.AnyAsync(a =>
                a.Role == "Support"
            )
        )
        {
            var support = new TummlyBackend.Models.Admin
            {
                FullName = "Tummly Support",
                Email = "support@tummly.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Support@123"),
                Role = "Support",
                IsActive = true
            };

            context.Admins.Add(support);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(
            ex,
            "Admin/Support seed failed; continuing without blocking readiness."
        );
    }

    initState.MarkSucceeded();
    logger.LogInformation("Database initialized successfully.");
}

public partial class Program;
