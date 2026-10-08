using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IWebHostEnvironment environment
        )
        {
            _emailSettings = emailSettings.Value;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _environment = environment;
        }

        private bool UsesResend =>
            !string.IsNullOrWhiteSpace(_emailSettings.ApiKey);

        private string GetFrontendBaseUrl()
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );
            }

            if (
                !Uri.TryCreate(frontendBaseUrl, UriKind.Absolute, out var uri)
                || (
                    uri.Scheme != Uri.UriSchemeHttp
                    && uri.Scheme != Uri.UriSchemeHttps
                )
            )
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl must be an absolute http(s) URL."
                );
            }

            return frontendBaseUrl;
        }

        private string GetEmailChromeBaseUrl() =>
            EmailAssets.ResolveChromeBaseUrl(_configuration);

        private string GetDarkLogoUrl() =>
            EmailAssets.GetDarkLogoPublicUrl(GetEmailChromeBaseUrl());

        private string FormatFromAddress() =>
            $"{_emailSettings.SenderName} <{_emailSettings.SenderEmail}>";

        /*
         =========================================
         SEND (Resend API or SMTP fallback)
         =========================================
        */

        private async Task<string?> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IReadOnlyList<EmailInlineImage>? inlineImages = null,
            IReadOnlyList<EmailFileAttachment>? fileAttachments = null,
            IReadOnlyDictionary<string, string>? tags = null
        )
        {
            var (html, images) = EmbedLoopbackChromeImages(
                htmlBody,
                inlineImages
            );

            if (UsesResend)
            {
                return await SendViaResendAsync(
                    toEmail,
                    subject,
                    html,
                    images,
                    fileAttachments,
                    tags
                );
            }

            await SendViaSmtpAsync(
                toEmail,
                subject,
                html,
                images,
                fileAttachments
            );
            return null;
        }

        /// <summary>
        /// When chrome assets resolve to a loopback host (local smoke),
        /// rewrite <c>http(s)://localhost…/email/*.png</c> to CID attachments.
        /// Remote clients cannot fetch loopback URLs — without CID, logos break.
        /// </summary>
        private (
            string Html,
            IReadOnlyList<EmailInlineImage>? InlineImages
        ) EmbedLoopbackChromeImages(
            string htmlBody,
            IReadOnlyList<EmailInlineImage>? inlineImages
        )
        {
            var chromeBase = GetEmailChromeBaseUrl();
            if (!EmailAssets.IsLoopbackChromeBase(chromeBase))
            {
                return (htmlBody, inlineImages);
            }

            var replacements = new (string Path, string ContentId, string FileName)[]
            {
                (
                    EmailAssets.PublicDarkLogoPath,
                    EmailAssets.DarkLogoContentId,
                    "tummly-logo-dark.png"
                ),
                (
                    EmailAssets.PublicPoweredByLogoPath,
                    EmailAssets.PoweredByLogoContentId,
                    "logo.png"
                ),
                (
                    EmailAssets.PublicWordmarkPath,
                    EmailAssets.PoweredByLogoContentId,
                    "tummly-wordmark.png"
                ),
                (
                    EmailAssets.PublicTopDecorationPath,
                    EmailAssets.TopDecorationContentId,
                    "top-decoration.png"
                ),
                (
                    EmailAssets.PublicBottomStripPath,
                    EmailAssets.BottomStripContentId,
                    "bottom-strip.png"
                ),
                (
                    EmailAssets.PublicBrandLogoPlaceholderPath,
                    EmailAssets.BrandLogoPlaceholderContentId,
                    "brand-logo-placeholder.png"
                ),
            };

            var html = htmlBody;
            List<EmailInlineImage>? merged = null;

            foreach (var (path, contentId, fileName) in replacements)
            {
                var absolute = $"{chromeBase.TrimEnd('/')}{path}";
                if (
                    html.IndexOf(absolute, StringComparison.OrdinalIgnoreCase)
                    < 0
                )
                {
                    continue;
                }

                html = html.Replace(
                    absolute,
                    $"cid:{contentId}",
                    StringComparison.OrdinalIgnoreCase
                );

                merged ??= inlineImages?.ToList() ?? [];
                if (
                    merged.Any(image =>
                        string.Equals(
                            image.ContentId,
                            contentId,
                            StringComparison.Ordinal
                        )
                    )
                )
                {
                    continue;
                }

                merged.Add(
                    new EmailInlineImage(
                        contentId,
                        fileName,
                        EmailAssets.ReadPublicPngBytes(_environment, fileName)
                    )
                );
            }

            return (html, merged ?? inlineImages);
        }

        private async Task<string?> SendViaResendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IReadOnlyList<EmailInlineImage>? inlineImages,
            IReadOnlyList<EmailFileAttachment>? fileAttachments,
            IReadOnlyDictionary<string, string>? tags = null
        )
        {
            var (deliverTo, html) =
                ApplyQaRedirect(toEmail, htmlBody);

            var deliverSubject =
                deliverTo.Equals(
                    toEmail,
                    StringComparison.OrdinalIgnoreCase
                )
                    ? subject
                    : $"[QA for {toEmail}] {subject}";

            ResendTag[]? resendTags = null;
            if (tags is { Count: > 0 })
            {
                resendTags = tags
                    .Where(
                        pair =>
                            !string.IsNullOrWhiteSpace(pair.Key)
                            && !string.IsNullOrWhiteSpace(pair.Value)
                    )
                    .Select(
                        pair => new ResendTag
                        {
                            Name = pair.Key.Trim(),
                            Value = pair.Value.Trim(),
                        }
                    )
                    .ToArray();
                if (resendTags.Length == 0)
                {
                    resendTags = null;
                }
            }

            var payload = new ResendEmailPayload
            {
                From = FormatFromAddress(),
                To = [deliverTo],
                Subject = deliverSubject,
                Html = html,
                ReplyTo = string.IsNullOrWhiteSpace(_emailSettings.ReplyToEmail)
                    ? null
                    : _emailSettings.ReplyToEmail,
                Attachments = ToResendAttachments(inlineImages, fileAttachments),
                Tags = resendTags,
            };

            var client = _httpClientFactory.CreateClient("Resend");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "emails"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _emailSettings.ApiKey
                );

            request.Content = JsonContent.Create(payload);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody =
                    await response.Content.ReadAsStringAsync();

                throw new InvalidOperationException(
                    $"Failed to send email via Resend ({(int)response.StatusCode}): {errorBody}"
                );
            }

            try
            {
                var parsed =
                    await response.Content.ReadFromJsonAsync<ResendSendResponse>();
                var id = parsed?.Id?.Trim();
                return string.IsNullOrWhiteSpace(id) ? null : id;
            }
            catch
            {
                return null;
            }
        }

        private (string DeliverTo, string Html) ApplyQaRedirect(
            string toEmail,
            string htmlBody
        )
        {
            var redirectTo = _emailSettings.QaRedirectTo?.Trim();

            if (string.IsNullOrWhiteSpace(redirectTo)
                || toEmail.Equals(
                    redirectTo,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return (toEmail, htmlBody);
            }

            var banner =
                $@"
                <div style='background:#fff3cd;padding:12px;border-radius:8px;margin-bottom:16px;font-size:14px;color:#664d03;'>
                <strong>QA redirect:</strong> This email was meant for
                <strong>{System.Net.WebUtility.HtmlEncode(toEmail)}</strong>. Check this inbox for the OTP;
                verification still uses the address entered on the form.
                </div>";

            return (redirectTo, InjectHtmlAfterBodyOpen(htmlBody, banner));
        }

        private static string InjectHtmlAfterBodyOpen(
            string htmlBody,
            string snippet
        )
        {
            var bodyOpen = htmlBody.IndexOf(
                "<body",
                StringComparison.OrdinalIgnoreCase
            );
            if (bodyOpen < 0)
            {
                return snippet + htmlBody;
            }

            var tagEnd = htmlBody.IndexOf('>', bodyOpen);
            if (tagEnd < 0)
            {
                return snippet + htmlBody;
            }

            return htmlBody.Insert(tagEnd + 1, snippet);
        }

        private async Task<SmtpClient> CreateSmtpClientAsync()
        {
            if (string.IsNullOrWhiteSpace(_emailSettings.Username)
                || string.IsNullOrWhiteSpace(_emailSettings.Password))
            {
                throw new InvalidOperationException(
                    "Email is not configured. Set EmailSettings__ApiKey for Resend, "
                    + "or EmailSettings__Username and EmailSettings__Password for SMTP."
                );
            }

            var smtp = new SmtpClient
            {
                Timeout = 30_000,
            };

            smtp.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            var socketOptions =
                _emailSettings.Port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(
                _emailSettings.SmtpServer,
                _emailSettings.Port,
                socketOptions
            );

            await smtp.AuthenticateAsync(
                _emailSettings.Username,
                _emailSettings.Password
            );

            return smtp;
        }

        private async Task SendViaSmtpAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IReadOnlyList<EmailInlineImage>? inlineImages,
            IReadOnlyList<EmailFileAttachment>? fileAttachments
        )
        {
            var email = new MimeMessage();

            email.From.Add(
                MailboxAddress.Parse(FormatFromAddress())
            );

            email.To.Add(
                MailboxAddress.Parse(toEmail)
            );

            email.Subject = subject;

            if (!string.IsNullOrWhiteSpace(_emailSettings.ReplyToEmail))
            {
                email.ReplyTo.Add(
                    MailboxAddress.Parse(_emailSettings.ReplyToEmail)
                );
            }

            email.Body = BuildSmtpBody(htmlBody, inlineImages, fileAttachments);

            using var smtp =
                await CreateSmtpClientAsync();

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }

        private static MimeEntity BuildSmtpBody(
            string htmlBody,
            IReadOnlyList<EmailInlineImage>? inlineImages,
            IReadOnlyList<EmailFileAttachment>? fileAttachments
        )
        {
            var hasInline = inlineImages is { Count: > 0 };
            var hasFiles = fileAttachments is { Count: > 0 };
            if (!hasInline && !hasFiles)
            {
                return new TextPart("html")
                {
                    Text = htmlBody
                };
            }

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
            };

            if (hasInline)
            {
                foreach (var image in inlineImages!)
                {
                    var resource = builder.LinkedResources.Add(
                        image.Filename,
                        image.Content,
                        new ContentType("image", "png")
                    );
                    resource.ContentId = image.ContentId;
                    resource.ContentDisposition = new ContentDisposition(
                        ContentDisposition.Inline
                    );
                }
            }

            if (hasFiles)
            {
                foreach (var file in fileAttachments!)
                {
                    var parts = file.ContentType.Split(
                        '/',
                        2,
                        StringSplitOptions.TrimEntries
                    );
                    var contentType = parts.Length == 2
                        ? new ContentType(parts[0], parts[1])
                        : new ContentType("application", "octet-stream");
                    builder.Attachments.Add(
                        file.Filename,
                        file.Content,
                        contentType
                    );
                }
            }

            return builder.ToMessageBody();
        }

        private static ResendAttachment[]? ToResendAttachments(
            IReadOnlyList<EmailInlineImage>? inlineImages,
            IReadOnlyList<EmailFileAttachment>? fileAttachments
        )
        {
            var list = new List<ResendAttachment>();

            if (inlineImages is { Count: > 0 })
            {
                foreach (var image in inlineImages)
                {
                    list.Add(
                        new ResendAttachment
                        {
                            Content = Convert.ToBase64String(image.Content),
                            Filename = image.Filename,
                            ContentId = image.ContentId,
                            ContentType = "image/png",
                        }
                    );
                }
            }

            if (fileAttachments is { Count: > 0 })
            {
                foreach (var file in fileAttachments)
                {
                    list.Add(
                        new ResendAttachment
                        {
                            Content = Convert.ToBase64String(file.Content),
                            Filename = file.Filename,
                            ContentType = file.ContentType,
                        }
                    );
                }
            }

            return list.Count == 0 ? null : list.ToArray();
        }

        /*
         =========================================
         SEND OTP EMAIL
         =========================================
        */

        public async Task SendOtpEmailAsync(
            string toEmail,
            string otp,
            string? restaurantName = null
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = OtpEmailTemplate.Generate(
                _environment,
                otp,
                restaurantName,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                OtpEmailTemplate.Subject(restaurantName),
                htmlBody
            );
        }

        /*
         =========================================
         SEND TRIAL REQUEST RECEIVED EMAIL
         =========================================
        */

        public async Task SendTrialRequestReceivedEmailAsync(
            string toEmail,
            string fullName,
            string businessName
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = TrialRequestReceivedEmailTemplate.Generate(
                _environment,
                fullName,
                businessName,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                EmailFrontendUrls.Terms(frontendBaseUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CookiePolicy(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                TrialRequestReceivedEmailTemplate.Subject,
                htmlBody
            );
        }

        /*
         =========================================
         SEND DECLINE EMAIL
         =========================================
        */

        public async Task SendDeclineEmailAsync(
            string toEmail,
            string fullName,
            string declineReason
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = TrialDeclineEmailTemplate.Generate(
                _environment,
                fullName,
                declineReason,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                EmailFrontendUrls.Terms(frontendBaseUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CookiePolicy(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                TrialDeclineEmailTemplate.Subject,
                htmlBody
            );
        }

        /*
         =========================================
         SEND MORE INFO REQUEST EMAIL
         =========================================
        */

        public async Task SendMoreInfoEmailAsync(
            string toEmail,
            string fullName,
            string moreInfoMessage
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = TrialMoreInfoEmailTemplate.Generate(
                _environment,
                fullName,
                moreInfoMessage,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                EmailFrontendUrls.Terms(frontendBaseUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CookiePolicy(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                TrialMoreInfoEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendResetPasswordEmailAsync(
            string toEmail,
            string resetLink
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = ResetPasswordEmailTemplate.Generate(
                _environment,
                resetLink,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                EmailFrontendUrls.Terms(frontendBaseUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CookiePolicy(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                ResetPasswordEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendPasswordChangedEmailAsync(
            string toEmail,
            string firstName
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = PasswordChangedEmailTemplate.Generate(
                _environment,
                firstName,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                EmailFrontendUrls.Terms(frontendBaseUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CookiePolicy(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                PasswordChangedEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendNewDeviceSignInEmailAsync(
            string toEmail,
            NewDeviceSignInDetails details
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = NewDeviceSignInEmailTemplate.Generate(
                _environment,
                details.FirstName,
                details.SignInTime,
                details.DeviceSummary,
                details.LocationSummary,
                EmailFrontendUrls.ForgotPassword(frontendBaseUrl),
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                NewDeviceSignInEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendContactEnquiryReceivedEmailAsync(
            string toEmail,
            string topicLabel,
            string reference
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = ContactEnquiryReceivedEmailTemplate.Generate(
                _environment,
                topicLabel,
                reference,
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                ContactEnquiryReceivedEmailTemplate.Subject(reference),
                htmlBody
            );
        }

        public async Task SendHelpCentreSupportReplyEmailAsync(
            string toEmail,
            string submitterName,
            string topicLabel,
            string replyBody,
            string? myQueriesUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = HelpCentreSupportReplyEmailTemplate.Generate(
                _environment,
                submitterName,
                topicLabel,
                replyBody,
                myQueriesUrl,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                HelpCentreSupportReplyEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendHelpCentreResolvedEmailAsync(
            string toEmail,
            string submitterName,
            string topicLabel,
            IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages,
            string? myQueriesUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = HelpCentreResolvedEmailTemplate.Generate(
                _environment,
                submitterName,
                topicLabel,
                excerptMessages,
                myQueriesUrl,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                HelpCentreResolvedEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendHelpCentreEscalationEmailAsync(
            string toEmail,
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string threadSummary,
            string? escalationNote,
            string supportDashboardUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = HelpCentreEscalationEmailTemplate.Generate(
                _environment,
                topicLabel,
                submitterName,
                submitterEmail,
                businessName,
                locationLabel,
                threadSummary,
                escalationNote,
                supportDashboardUrl,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                HelpCentreEscalationEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendHelpCentreOperatorReplyEmailAsync(
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string replyBody,
            string supportDashboardUrl
        )
        {
            var settings = _configuration
                .GetSection("HelpCentre")
                .Get<HelpCentreSettings>()
                ?? new HelpCentreSettings();

            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = HelpCentreOperatorReplyEmailTemplate.Generate(
                _environment,
                topicLabel,
                submitterName,
                submitterEmail,
                businessName,
                replyBody,
                supportDashboardUrl,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                settings.SupportNotificationEmail,
                HelpCentreOperatorReplyEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendHelpCentreNewQueryEmailAsync(
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string messagePreview,
            int attachmentCount,
            string supportDashboardUrl
        )
        {
            var settings = _configuration
                .GetSection("HelpCentre")
                .Get<HelpCentreSettings>()
                ?? new HelpCentreSettings();

            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = HelpCentreNewQueryEmailTemplate.Generate(
                _environment,
                topicLabel,
                submitterName,
                submitterEmail,
                businessName,
                locationLabel,
                messagePreview,
                attachmentCount,
                supportDashboardUrl,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                settings.SupportNotificationEmail,
                HelpCentreNewQueryEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendGuestResponseEmailAsync(
            string toEmail,
            string subject,
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string message,
            string? brandLogoUrl = null,
            GuestResponseEmailOfferBlock? offer = null,
            string? unsubscribeHref = null,
            string? ticketSubject = null
        )
        {
            var htmlBody = GuestResponseEmailTemplate.Generate(
                _environment,
                brandTitle,
                brandSubtitle,
                locationAddress,
                ticketSubject ?? subject,
                message,
                GetFrontendBaseUrl(),
                brandLogoUrl,
                offer,
                unsubscribeHref,
                emailAssetsBaseUrl: GetEmailChromeBaseUrl()
            );

            _ = await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<string?> SendCampaignGuestEmailAsync(
            string toEmail,
            string subject,
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string message,
            string? brandLogoUrl = null,
            GuestResponseEmailOfferBlock? offer = null,
            string? unsubscribeHref = null,
            string? ticketSubject = null,
            IReadOnlyDictionary<string, string>? tags = null
        )
        {
            var htmlBody = GuestResponseEmailTemplate.Generate(
                _environment,
                brandTitle,
                brandSubtitle,
                locationAddress,
                ticketSubject ?? subject,
                message,
                GetFrontendBaseUrl(),
                brandLogoUrl,
                offer,
                unsubscribeHref,
                emailAssetsBaseUrl: GetEmailChromeBaseUrl()
            );

            return await SendEmailAsync(
                toEmail,
                subject,
                htmlBody,
                tags: tags
            );
        }

        public async Task SendTeamInvitationEmailAsync(
            string toEmail,
            string subject,
            string acceptUrl,
            string firstName,
            string inviterName,
            string workspaceName,
            string roleName,
            string locationScope,
            string? invitationMessage
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = TeamInvitationEmailTemplate.Generate(
                _environment,
                firstName,
                inviterName,
                workspaceName,
                roleName,
                locationScope,
                invitationMessage,
                acceptUrl,
                $"{frontendBaseUrl}/help-center",
                // Hosted HTTPS — Gmail strips data: URIs (same pattern as guest chrome).
                GetDarkLogoUrl()
            );

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendBillingAccountNoticeEmailAsync(
            string toEmail,
            string firstName,
            string title,
            string body,
            string? ctaLabel,
            string? ctaHref
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = BillingAccountNoticeEmailTemplate.Generate(
                _environment,
                firstName,
                title,
                body,
                ctaLabel,
                ctaHref,
                frontendBaseUrl,
                EmailFrontendUrls.HelpCentre(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(toEmail, title, htmlBody);
        }

        public async Task SendPilotStartedEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            string pilotEndDateLabel
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = PilotStartedEmailTemplate.Generate(
                _environment,
                firstName,
                restaurantName,
                pilotEndDateLabel,
                EmailFrontendUrls.AppHome(frontendBaseUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                PilotStartedEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendPilotEndingSoonEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            int daysRemaining,
            string pilotEndDateLabel,
            string plansUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = PilotEndingSoonEmailTemplate.Generate(
                _environment,
                daysRemaining.ToString(),
                firstName,
                restaurantName,
                pilotEndDateLabel,
                EmailFrontendUrls.Absolute(frontendBaseUrl, plansUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                PilotEndingSoonEmailTemplate.Subject(daysRemaining),
                htmlBody
            );
        }

        public async Task SendPilotEndedEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            string plansUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = PilotEndedEmailTemplate.Generate(
                _environment,
                firstName,
                restaurantName,
                EmailFrontendUrls.Absolute(frontendBaseUrl, plansUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                PilotEndedEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendPaymentActionRequiredEmailAsync(
            string toEmail,
            string firstName,
            string orderDescription,
            string billingUrl,
            string ctaLabel
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = PaymentActionRequiredEmailTemplate.Generate(
                _environment,
                firstName,
                orderDescription,
                EmailFrontendUrls.Absolute(frontendBaseUrl, billingUrl),
                ctaLabel,
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                PaymentActionRequiredEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendUsageWarningEmailAsync(
            string toEmail,
            string firstName,
            string allowanceKind,
            string percentUsed,
            string usedAmount,
            string remainingAmount,
            string resetDate,
            string usageUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = UsageWarningEmailTemplate.Generate(
                _environment,
                allowanceKind,
                firstName,
                percentUsed,
                usedAmount,
                remainingAmount,
                resetDate,
                EmailFrontendUrls.Absolute(frontendBaseUrl, usageUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                UsageWarningEmailTemplate.Subject(allowanceKind),
                htmlBody
            );
        }

        public async Task SendUsageExhaustedEmailAsync(
            string toEmail,
            string firstName,
            string allowanceKind,
            string ctaUrl,
            string ctaLabel
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = UsageExhaustedEmailTemplate.Generate(
                _environment,
                allowanceKind,
                firstName,
                EmailFrontendUrls.Absolute(frontendBaseUrl, ctaUrl),
                ctaLabel,
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                UsageExhaustedEmailTemplate.Subject(allowanceKind),
                htmlBody
            );
        }

        public async Task SendWeeklyBriefEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string periodLabel,
            int qrScans,
            int feedbackReceived,
            int guestsCaptured,
            int offerClaimed,
            int redemptions,
            int campaignEngagement,
            string whatChanged,
            string recommendedNextStep,
            string weeklyBriefUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = WeeklyBriefEmailTemplate.Generate(
                _environment,
                firstName,
                locationName,
                periodLabel,
                qrScans,
                feedbackReceived,
                guestsCaptured,
                offerClaimed,
                redemptions,
                campaignEngagement,
                whatChanged,
                recommendedNextStep,
                EmailFrontendUrls.Absolute(frontendBaseUrl, weeklyBriefUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                WeeklyBriefEmailTemplate.Subject(locationName),
                htmlBody
            );
        }

        public async Task SendTummlyVatInvoiceEmailAsync(
            string toEmail,
            string firstName,
            string documentNumber,
            string lineDescription,
            int grossPence,
            DateTime paymentSuccessUtc,
            string billingUrl,
            byte[] pdfContent,
            string pdfFileName
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var amount = TummlyVatInvoicePdfWriter.FormatAmountLabel(grossPence);
            var paymentDate = LondonDateFormat.DMmmYyyy(paymentSuccessUtc);
            var description = string.IsNullOrWhiteSpace(lineDescription)
                ? "your purchase"
                : lineDescription.Trim();

            var htmlBody = PaymentConfirmedEmailTemplate.Generate(
                _environment,
                firstName,
                description,
                amount,
                paymentDate,
                documentNumber.Trim(),
                EmailFrontendUrls.Absolute(frontendBaseUrl, billingUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                PaymentConfirmedEmailTemplate.Subject,
                htmlBody,
                inlineImages: null,
                fileAttachments:
                [
                    new EmailFileAttachment(
                        pdfFileName,
                        pdfContent,
                        "application/pdf"
                    ),
                ]
            );
        }

        public async Task SendShopOrderConfirmedEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string orderNumber,
            string materialsLinesHtml,
            string deliveryAddressHtml,
            string orderUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = ShopOrderConfirmedEmailTemplate.Generate(
                _environment,
                firstName,
                locationName,
                orderNumber,
                materialsLinesHtml,
                deliveryAddressHtml,
                EmailFrontendUrls.Absolute(frontendBaseUrl, orderUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                ShopOrderConfirmedEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendShopOrderDispatchedEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string orderNumber,
            string deliveryEstimate,
            string trackingDetails,
            string orderUrl
        )
        {
            var frontendBaseUrl = GetFrontendBaseUrl();
            var htmlBody = ShopOrderDispatchedEmailTemplate.Generate(
                _environment,
                firstName,
                locationName,
                orderNumber,
                deliveryEstimate,
                trackingDetails,
                EmailFrontendUrls.Absolute(frontendBaseUrl, orderUrl),
                EmailFrontendUrls.Privacy(frontendBaseUrl),
                EmailFrontendUrls.CompanyDetails(frontendBaseUrl),
                GetDarkLogoUrl()
            );

            await SendEmailAsync(
                toEmail,
                ShopOrderDispatchedEmailTemplate.Subject,
                htmlBody
            );
        }

        private sealed class ResendEmailPayload
        {
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public string[] To { get; set; } = [];

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("html")]
            public string Html { get; set; } = string.Empty;

            [JsonPropertyName("reply_to")]
            public string? ReplyTo { get; set; }

            [JsonPropertyName("attachments")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ResendAttachment[]? Attachments { get; set; }

            [JsonPropertyName("tags")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ResendTag[]? Tags { get; set; }
        }

        private sealed class ResendTag
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("value")]
            public string Value { get; set; } = string.Empty;
        }

        private sealed class ResendSendResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class ResendAttachment
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;

            [JsonPropertyName("filename")]
            public string Filename { get; set; } = string.Empty;

            [JsonPropertyName("content_id")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ContentId { get; set; }

            [JsonPropertyName("content_type")]
            public string ContentType { get; set; } = "image/png";
        }
    }
}
