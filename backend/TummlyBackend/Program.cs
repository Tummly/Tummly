using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing;
using TummlyBackend.Billing.Pricebook;
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

var redis = RedisConnection.TryResolve(builder.Configuration);
if (!string.IsNullOrWhiteSpace(redis))
{
    // Fail fast when Redis is configured but unreachable (same seam as SignalR).
    RedisConnection.EnsureReachable(redis);
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redis;
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

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

builder.Services.Configure<RevolutSettings>(
    builder.Configuration.GetSection(RevolutSettings.SectionName)
);

builder.Services.AddOptions<TummlySellerVatSettings>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.RegistrationNumber =
            configuration[TummlySellerVatSettings.RegistrationNumberKey]
            ?? string.Empty;
        options.EffectiveDate =
            configuration[TummlySellerVatSettings.EffectiveDateKey]
            ?? string.Empty;
        options.LegalName =
            configuration[TummlySellerVatSettings.LegalNameKey]
            ?? string.Empty;
        options.RegisteredAddress =
            configuration[TummlySellerVatSettings.RegisteredAddressKey]
            ?? string.Empty;
    });

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
builder.Services.Configure<GuestResponseEmailDeliverySettings>(
    builder.Configuration.GetSection(
        GuestResponseEmailDeliverySettings.SectionName
    )
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

builder.Services.AddScoped<IQrCodeProvisioningService, QrCodeProvisioningService>();

builder.Services.AddScoped<IGuestUpsertService, GuestUpsertService>();

builder.Services.AddScoped<GuestsListService>();
builder.Services.AddScoped<IGuestsListService>(sp =>
    sp.GetRequiredService<GuestsListService>()
);
builder.Services.AddScoped<IGuestsExportService>(sp =>
    sp.GetRequiredService<GuestsListService>()
);

builder.Services.AddScoped<CaptureWindowedEngagementAggregate>();
builder.Services.AddScoped<CaptureMultiLocationReadsService>();
builder.Services.AddScoped<ICaptureMultiLocationReadsService>(sp =>
    sp.GetRequiredService<CaptureMultiLocationReadsService>()
);
builder.Services.AddScoped<CaptureLocationSnapshotService>();
builder.Services.AddScoped<ICaptureLocationSnapshotService>(sp =>
    sp.GetRequiredService<CaptureLocationSnapshotService>()
);
builder.Services.AddScoped<CaptureThankYouOfferService>();
builder.Services.AddScoped<ICaptureThankYouOfferService>(sp =>
    sp.GetRequiredService<CaptureThankYouOfferService>()
);
builder.Services.AddScoped<CapturePreviewOptionsService>();
builder.Services.AddScoped<ICapturePreviewOptionsService>(sp =>
    sp.GetRequiredService<CapturePreviewOptionsService>()
);
builder.Services.AddScoped<CaptureArchiveListService>();
builder.Services.AddScoped<ICaptureArchiveListService>(sp =>
    sp.GetRequiredService<CaptureArchiveListService>()
);
builder.Services.AddScoped<CaptureQrLifecycleService>();
builder.Services.AddScoped<ICaptureQrLifecycleService>(sp =>
    sp.GetRequiredService<CaptureQrLifecycleService>()
);
builder.Services.AddScoped<IGuestsEffectiveLocationService, GuestsEffectiveLocationService>();

builder.Services.AddScoped<IGuestProfileService, GuestProfileService>();

builder.Services.AddScoped<IFeedbackGuestBackfillService, FeedbackGuestBackfillService>();

builder.Services.AddScoped<IGuestTaggingService, GuestTaggingService>();

builder.Services.AddScoped<IGuestTagBackfillService, GuestTagBackfillService>();

builder.Services.AddScoped<
    ILocationGuestActivityRecorder,
    LocationGuestActivityRecorder
>();
builder.Services.AddScoped<
    IGuestActivityListService,
    GuestActivityListService
>();
builder.Services.AddScoped<
    IGuestFeedbacksListService,
    GuestFeedbacksListService
>();
builder.Services.AddScoped<IGuestNotesService, GuestNotesService>();
builder.Services.AddScoped<
    IFeedbackInternalNotesService,
    FeedbackInternalNotesService
>();
builder.Services.AddScoped<
    IFeedbackClassificationCorrectionsService,
    FeedbackClassificationCorrectionsService
>();
builder.Services.AddScoped<
    IFeedbackDetectedTagsService,
    FeedbackDetectedTagsService
>();
builder.Services.AddScoped<
    IFeedbackWorkflowStatusChangesService,
    FeedbackWorkflowStatusChangesService
>();
builder.Services.AddScoped<
    IFeedbackCloseOutsService,
    FeedbackCloseOutsService
>();
builder.Services.AddScoped<
    IFeedbackGuestResponsesService,
    FeedbackGuestResponsesService
>();
builder.Services.AddScoped<
    IFeedbackGuestPreviewSendTestService,
    FeedbackGuestPreviewSendTestService
>();
builder.Services.AddScoped<
    IFeedbackInternalActionsService,
    FeedbackInternalActionsService
>();
builder.Services.AddScoped<
    IFeedbackRespondAndRecordService,
    FeedbackRespondAndRecordService
>();
builder.Services.AddScoped<
    IFeedbackRecoveryOffersService,
    FeedbackRecoveryOffersService
>();
builder.Services.AddScoped<
    IFeedbackRecoveryOfferAttachService,
    FeedbackRecoveryOfferAttachService
>();
builder.Services.AddScoped<
    IFeedbackRecoveryCompletionsService,
    FeedbackRecoveryCompletionsService
>();
builder.Services.AddScoped<
    IFeedbackRecoveryDraftsService,
    FeedbackRecoveryDraftsService
>();
builder.Services.AddScoped<
    IFeedbackInboxListService,
    FeedbackInboxListService
>();
builder.Services.AddScoped<
    ICampaignsListService,
    CampaignsListService
>();
builder.Services.AddScoped<
    ICampaignsSummaryService,
    CampaignsSummaryService
>();
builder.Services.AddScoped<
    ICampaignDraftService,
    CampaignDraftService
>();
builder.Services.AddScoped<
    IActiveOfferCapGate,
    ActiveOfferCapGate
>();
builder.Services.AddScoped<
    IOffersCatalogService,
    OffersCatalogService
>();
builder.Services.AddScoped<
    IOffersMetricsService,
    OffersMetricsService
>();
builder.Services.AddScoped<
    IOfferIssueService,
    OfferIssueService
>();
builder.Services.AddScoped<
    IOfferLifecycleService,
    OfferLifecycleService
>();
builder.Services.AddScoped<
    IOfferVoidRequestService,
    OfferVoidRequestService
>();
builder.Services.AddScoped<
    ICampaignRecommendationService,
    CampaignRecommendationService
>();
builder.Services.AddScoped<
    IHomeRecommendationService,
    HomeRecommendationService
>();
builder.Services.AddScoped<
    IOfferRecommendationService,
    OfferRecommendationService
>();
builder.Services.AddScoped<
    IWeeklyBriefGenerateService,
    WeeklyBriefGenerateService
>();
builder.Services.AddScoped<
    IWeeklyBriefMondayJob,
    WeeklyBriefMondayJob
>();
builder.Services.AddScoped<
    IWeeklyBriefReadyNotifier,
    WeeklyBriefReadyNotifier
>();
builder.Services.AddScoped<
    ICampaignMessageDraftService,
    CampaignMessageDraftService
>();
builder.Services.AddScoped<
    ICampaignSendTestService,
    CampaignSendTestService
>();
builder.Services.AddScoped<
    ICampaignEligibilityService,
    CampaignEligibilityService
>();
builder.Services.AddScoped<
    ICampaignBillingReserve,
    LiveCampaignBillingReserve
>();
builder.Services.AddSingleton<
    ICampaignProductAnalytics,
    LoggingCampaignProductAnalytics
>();
builder.Services.AddScoped<
    ICampaignSendStartGate,
    ClearCampaignSendStartGate
>();
builder.Services.AddScoped<
    ICampaignOutboundSender,
    CampaignOutboundEmailSender
>();
builder.Services.AddScoped<
    ICampaignFireService,
    CampaignFireService
>();
builder.Services.AddSingleton<ICampaignFireWork, CampaignFireWork>();
builder.Services.AddHostedService<CampaignFireBackgroundService>();
builder.Services.AddScoped<
    ICampaignScheduleCommitService,
    CampaignScheduleCommitService
>();
builder.Services.AddScoped<
    ICampaignLifecycleService,
    CampaignLifecycleService
>();
builder.Services.AddSingleton<
    ICampaignTemplateCatalogueService,
    CampaignTemplateCatalogueService
>();
builder.Services.AddScoped<
    IGuestIdentityUpdateService,
    GuestIdentityUpdateService
>();
builder.Services.AddScoped<
    IGuestMarketingPreferenceUpdateService,
    GuestMarketingPreferenceUpdateService
>();
builder.Services.AddScoped<
    ILocationGuestDeleteService,
    LocationGuestDeleteService
>();
builder.Services.AddScoped<
    ILocationGuestActivityBackfillService,
    LocationGuestActivityBackfillService
>();

builder.Services.AddScoped<IOwnedLocationService, OwnedLocationService>();
builder.Services.AddScoped<IOwnedLocationInsertService, OwnedLocationInsertService>();
builder.Services.AddScoped<ILocationsListService, LocationsListService>();
builder.Services.AddScoped<ILocationsActivityService, LocationsActivityService>();
builder.Services.AddScoped<ILocationsLifecycleWriteService, LocationsLifecycleWriteService>();
builder.Services.AddScoped<ILocationLifecycleService, LocationLifecycleService>();

builder.Services.AddScoped<IAccountWorkspaceService, AccountWorkspaceService>();
builder.Services.AddScoped<ITeamPermissionsService, TeamPermissionsService>();
builder.Services.AddScoped<ITeamMemberCapGate, TeamMemberCapGate>();
builder.Services.AddScoped<IPlanEntitlementsSnapshot, PlanEntitlementsSnapshotService>();
builder.Services.AddSingleton<IPricebookCatalog>(sp =>
    PricebookCatalog.CreateForHost(sp.GetRequiredService<IHostEnvironment>())
);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICreditLedger, CreditLedgerService>();
builder.Services.AddScoped<ICreditThresholdEvaluator, CreditThresholdEvaluator>();
builder.Services.AddScoped<ICreditBalanceSnapshot, CreditBalanceSnapshotService>();
builder.Services.AddScoped<IIncludedPeriodMintService, IncludedPeriodMintService>();
builder.Services.AddScoped<IIncludedPeriodJob, IncludedPeriodJob>();
builder.Services.AddScoped<IAssistantAiBilling, AssistantAiBillingService>();
builder.Services.AddScoped<IBilledAiActionCoordinator, BilledAiActionCoordinator>();
builder.Services.AddScoped<
    IRecoverySmsBillingReserve,
    LiveRecoverySmsBillingReserve
>();
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<
        IRecoveryGuestSmsDelivery,
        TestingRecoveryGuestSmsDelivery
    >();
}
else
{
    builder.Services.AddScoped<IRecoveryGuestSmsDelivery, TwilioRecoveryGuestSmsDelivery>();
}
builder.Services.AddScoped<IPlanChangeService, PlanChangeService>();
builder.Services.AddScoped<IRevolutMerchantCreateGate, RevolutMerchantCreateGate>();
builder.Services.AddScoped<IRevolutMerchantClient, RevolutMerchantClient>();
builder.Services.AddScoped<
    IRevolutCancelAtPeriodEndAdapter,
    RevolutCancelAtPeriodEndAdapter
>();
builder.Services.AddScoped<
    IFirstPaidConversionPaySession,
    FirstPaidConversionPaySessionService
>();
builder.Services.AddScoped<
    ISameCadenceUpgradePaySession,
    SameCadenceUpgradePaySessionService
>();
builder.Services.AddScoped<
    IPaymentMethodUpdatePaySession,
    PaymentMethodUpdatePaySessionService
>();
builder.Services.AddScoped<ICreditTopUpPaySession, CreditTopUpPaySessionService>();
builder.Services.AddScoped<ICycleEndPlanChange, CycleEndPlanChangeService>();
builder.Services.AddScoped<ICycleEndPlanCancel, CycleEndPlanCancelService>();
builder.Services.AddScoped<
    IRevolutOrderCompletedApplier,
    RevolutOrderCompletedApplier
>();
builder.Services.AddScoped<ITummlyVatInvoiceService, TummlyVatInvoiceService>();
builder.Services.AddScoped<IAdminPaymentRefundService, AdminPaymentRefundService>();
builder.Services.AddScoped<
    IRevolutPaymentRefundCompletedHandler,
    RevolutPaymentRefundCompletedHandler
>();
builder.Services.AddScoped<IRevolutWebhookService, RevolutWebhookService>();
builder.Services.AddScoped<IRevolutDunningPayAdapter, RevolutDunningPayAdapter>();
builder.Services.AddScoped<IBillingCreditsService, BillingCreditsService>();
builder.Services.AddScoped<IExtraGroupLocationService, ExtraGroupLocationService>();
builder.Services.AddScoped<IBillingAccountLifecycle, BillingAccountLifecycleService>();
builder.Services.AddScoped<
    IBillingAccountNoticeNotifier,
    BillingAccountNoticeNotifier
>();
builder.Services.AddScoped<ITeamInvitationAcceptService, TeamInvitationAcceptService>();
builder.Services.AddScoped<IGuestDataExportService, GuestDataExportService>();

builder.Services.AddHttpClient(
    RevolutMerchantClient.HttpClientName,
    (sp, client) =>
    {
        var settings = sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<RevolutSettings>
        >().Value;
        if (!string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
        {
            client.BaseAddress = new Uri(
                settings.ApiBaseUrl.TrimEnd('/') + "/"
            );
        }

        client.Timeout = TimeSpan.FromSeconds(30);
    }
);

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

builder.Services.AddHttpClient(
    FeedbackRecoveryDraftStructuredOutput.HttpClientName,
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

if (useFakeFeedbackClassification)
{
    builder.Services.AddSingleton<FakeFeedbackRecoveryDraftProvider>();
    builder.Services.AddSingleton<IFeedbackRecoveryDraftProvider>(sp =>
        sp.GetRequiredService<FakeFeedbackRecoveryDraftProvider>()
    );
    builder.Services.AddSingleton<FakeCampaignRecommendationProvider>();
    builder.Services.AddSingleton<ICampaignRecommendationProvider>(sp =>
        sp.GetRequiredService<FakeCampaignRecommendationProvider>()
    );
    builder.Services.AddSingleton<FakeHomeRecommendationProvider>();
    builder.Services.AddSingleton<IHomeRecommendationProvider>(sp =>
        sp.GetRequiredService<FakeHomeRecommendationProvider>()
    );
    builder.Services.AddSingleton<FakeOfferRecommendationProvider>();
    builder.Services.AddSingleton<IOfferRecommendationProvider>(sp =>
        sp.GetRequiredService<FakeOfferRecommendationProvider>()
    );
    builder.Services.AddSingleton<FakeWeeklyBriefProvider>();
    builder.Services.AddSingleton<IWeeklyBriefProvider>(sp =>
        sp.GetRequiredService<FakeWeeklyBriefProvider>()
    );
    builder.Services.AddSingleton<FakeCampaignMessageDraftProvider>();
    builder.Services.AddSingleton<ICampaignMessageDraftProvider>(sp =>
        sp.GetRequiredService<FakeCampaignMessageDraftProvider>()
    );
    builder.Services.AddSingleton<FakeAssistantLiveAnswerProvider>();
    builder.Services.AddSingleton<IAssistantLiveAnswerProvider>(sp =>
        sp.GetRequiredService<FakeAssistantLiveAnswerProvider>()
    );
}
else
{
    builder.Services.AddSingleton<
        IFeedbackRecoveryDraftProvider,
        AzureOpenAIFeedbackRecoveryDraftProvider
    >();
    builder.Services.AddSingleton<
        ICampaignRecommendationProvider,
        AzureOpenAICampaignRecommendationProvider
    >();
    builder.Services.AddSingleton<
        IHomeRecommendationProvider,
        AzureOpenAIHomeRecommendationProvider
    >();
    builder.Services.AddSingleton<
        IOfferRecommendationProvider,
        AzureOpenAIOfferRecommendationProvider
    >();
    builder.Services.AddSingleton<
        IWeeklyBriefProvider,
        AzureOpenAIWeeklyBriefProvider
    >();
    builder.Services.AddSingleton<
        ICampaignMessageDraftProvider,
        AzureOpenAICampaignMessageDraftProvider
    >();
    builder.Services.AddSingleton<
        IAssistantLiveAnswerProvider,
        AzureOpenAIAssistantLiveAnswerProvider
    >();
}

builder.Services.AddHttpClient(
    CampaignRecommendationStructuredOutput.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{FeedbackClassificationSettings.SectionName}:Endpoint"
        ];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        }

        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

builder.Services.AddHttpClient(
    HomeRecommendationStructuredOutput.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{FeedbackClassificationSettings.SectionName}:Endpoint"
        ];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        }

        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

builder.Services.AddHttpClient(
    OfferRecommendationStructuredOutput.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{FeedbackClassificationSettings.SectionName}:Endpoint"
        ];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        }

        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

builder.Services.AddHttpClient(
    WeeklyBriefStructuredOutput.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{FeedbackClassificationSettings.SectionName}:Endpoint"
        ];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        }

        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

builder.Services.AddHttpClient(
    CampaignMessageDraftStructuredOutput.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{FeedbackClassificationSettings.SectionName}:Endpoint"
        ];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        }

        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

builder.Services.AddHttpClient(
    AssistantLiveAnswerStructuredOutput.HttpClientName,
    client =>
    {
        var endpoint = builder.Configuration[
            $"{FeedbackClassificationSettings.SectionName}:Endpoint"
        ];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        }

        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

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

builder.Services.AddSingleton<
    IGuestResponseEmailDeliveryWork,
    GuestResponseEmailDeliveryWork
>();
builder.Services.AddHostedService<GuestResponseEmailDeliveryBackgroundService>();
builder.Services.AddHostedService<CreditReservationSweeperBackgroundService>();

builder.Services.AddHttpClient(
    SignInMetadataResolverHttpClient.Name,
    SignInMetadataResolverHttpClient.Configure
);

builder.Services.AddScoped<ISignInMetadataResolver, SignInMetadataResolver>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<ISmsService, TwilioVerifySmsService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRestaurantPermissionHelper, RestaurantPermissionHelper>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<IHelpCentreService, HelpCentreService>();

builder.Services.AddScoped<ISupportService, SupportService>();

builder.Services.AddScoped<IOperatorNotificationsService, OperatorNotificationsService>();

builder.Services.AddScoped<IAssistantFeedbackRetrieve, AssistantFeedbackRetrieve>();
builder.Services.AddScoped<IAssistantOffersRetrieve, AssistantOffersRetrieve>();
builder.Services.AddScoped<IAssistantCampaignsRetrieve, AssistantCampaignsRetrieve>();
builder.Services.AddScoped<IAssistantCaptureRetrieve, AssistantCaptureRetrieve>();
builder.Services.AddScoped<IAssistantHomeKpiRetrieve, AssistantHomeKpiRetrieve>();
builder.Services.AddScoped<IAssistantGuestsRetrieve, AssistantGuestsRetrieve>();
builder.Services.AddScoped<IAssistantAttentionRetrieve, AssistantAttentionRetrieve>();

builder.Services.AddScoped<IAssistantConversationService, AssistantConversationService>();

builder.Services.AddSingleton<
    IAssistantProgressPublisher,
    SignalRAssistantProgressPublisher
>();

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
builder.Services.AddHostedService<IncludedPeriodBackgroundService>();

builder.Services.AddHostedService<WeeklyBriefMondayBackgroundService>();

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

// Non-secret Revolut readiness for QA sandbox rehearsal. Does not gate deploy.
app.MapGet("/health/revolut", (
    Microsoft.Extensions.Options.IOptions<RevolutSettings> revolutOptions,
    Microsoft.Extensions.Options.IOptions<TummlySellerVatSettings> vatOptions
) =>
{
    var revolut = revolutOptions.Value;
    var vat = vatOptions.Value;
    var configuredVariations = 0;
    foreach (var key in RevolutPlanVariationKeys.All)
    {
        if (revolut.TryGetPlanVariationId(key, out _))
        {
            configuredVariations++;
        }
    }

    var gate = new RevolutMerchantCreateGate(vatOptions, revolutOptions);
    var createBlocked = gate.Evaluate(RevolutPlanVariationKeys.StarterMonthly);

    return Results.Ok(
        new
        {
            status = createBlocked is null ? "ready" : "not_ready",
            hostMode = revolut.HostMode,
            requireSandboxHost = revolut.RequireSandboxHost,
            merchantApiConfigured = revolut.HasMerchantApiConfig,
            webhookSigningSecretConfigured =
                !string.IsNullOrWhiteSpace(revolut.WebhookSigningSecret),
            sellerVatComplete = vat.IsComplete,
            planVariationsConfigured = configuredVariations,
            planVariationsExpected = RevolutPlanVariationKeys.All.Count,
            createBlockedCode = createBlocked,
        }
    );
});

app.MapControllers();

app.MapOperatorHub<NotificationsHub>(
    OperatorSignalRHubs.NotificationsPath
);
app.MapOperatorHub<FeedbackHomeHub>(
    OperatorSignalRHubs.FeedbackHomePath
);
app.MapOperatorHub<AssistantHub>(
    OperatorSignalRHubs.AssistantPath
);

app.Lifetime.ApplicationStarted.Register(() =>
{
    var initState = app.Services.GetRequiredService<DatabaseInitState>();
    var runStartupInitInTests = app.Configuration.GetValue<bool>(
        "Database:RunStartupInitInTests"
    );

    if (app.Environment.IsEnvironment("Testing") && !runStartupInitInTests)
    {
        // Integration tests use in-memory DB; skip migrate and mark ready.
        initState.MarkSucceeded();
        return;
    }

    _ = InitializeDatabaseAsync(
        app.Services,
        app.Configuration,
        initState
    );
});

if (WeeklyBriefOneTimeGenerateCommand.IsRequested(args))
{
    app.Logger.LogInformation(
        "Weekly brief one-time generate starting (ticket 08)"
    );
    Environment.ExitCode =
        await WeeklyBriefOneTimeGenerateCommand.ExecuteAsync(
            app.Services,
            DateTime.UtcNow,
            app.Logger
        );
    return;
}

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
        FailDatabaseInitExit(services);
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
                FailDatabaseInitExit(services);
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

    try
    {
        var backfill = scope.ServiceProvider
            .GetRequiredService<IFeedbackGuestBackfillService>();
        await backfill.BackfillAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Feedback guest backfill failed.");
        initState.MarkFailed();
        FailDatabaseInitExit(services);
        return;
    }

    try
    {
        var guestTagBackfill = scope.ServiceProvider
            .GetRequiredService<IGuestTagBackfillService>();
        await guestTagBackfill.BackfillAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Guest tag backfill failed.");
        initState.MarkFailed();
        FailDatabaseInitExit(services);
        return;
    }

    try
    {
        var activityBackfill = scope.ServiceProvider
            .GetRequiredService<ILocationGuestActivityBackfillService>();
        await activityBackfill.BackfillAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Location Guest activity backfill failed.");
        initState.MarkFailed();
        FailDatabaseInitExit(services);
        return;
    }

    initState.MarkSucceeded();
    logger.LogInformation("Database initialized successfully.");

    if (configuration.GetValue<bool>(
            WeeklyBriefOneTimeGenerateCommand.OneTimeGenerateOnStartupConfigKey
        ))
    {
        try
        {
            var weeklyBriefJob = scope.ServiceProvider
                .GetRequiredService<IWeeklyBriefMondayJob>();
            await WeeklyBriefOneTimeGenerateCommand.RunOnceAfterDeployAsync(
                context,
                weeklyBriefJob,
                logger,
                DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Weekly brief one-time generate after deploy failed; will retry on next start"
            );
        }
    }
}

static void FailDatabaseInitExit(IServiceProvider services)
{
    var environment = services.GetRequiredService<IHostEnvironment>();
    var configuration = services.GetRequiredService<IConfiguration>();
    if (
        environment.IsEnvironment("Testing")
        && configuration.GetValue<bool>("Database:RunStartupInitInTests")
    )
    {
        return;
    }

    Environment.Exit(1);
}

public partial class Program;
