using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Auth;
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
                !Uri.TryCreate(
                    frontendBaseUrl,
                    UriKind.Absolute,
                    out var parsed
                )
                || (
                    parsed.Scheme != Uri.UriSchemeHttp
                    && parsed.Scheme != Uri.UriSchemeHttps
                )
            )
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl must be an absolute http(s) URL."
                );
            }

            return frontendBaseUrl;
        }

        private string FormatFromAddress() =>
            $"{_emailSettings.SenderName} <{_emailSettings.SenderEmail}>";

        /*
         =========================================
         SEND (Resend API or SMTP fallback)
         =========================================
        */

        private async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IReadOnlyList<EmailInlineImage>? inlineImages = null,
            IReadOnlyList<EmailFileAttachment>? fileAttachments = null
        )
        {
            if (UsesResend)
            {
                await SendViaResendAsync(
                    toEmail,
                    subject,
                    htmlBody,
                    inlineImages,
                    fileAttachments
                );
                return;
            }

            await SendViaSmtpAsync(
                toEmail,
                subject,
                htmlBody,
                inlineImages,
                fileAttachments
            );
        }

        private async Task SendViaResendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IReadOnlyList<EmailInlineImage>? inlineImages,
            IReadOnlyList<EmailFileAttachment>? fileAttachments
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

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var errorBody =
                await response.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Failed to send email via Resend ({(int)response.StatusCode}): {errorBody}"
            );
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
            string otp
        )
        {
            var htmlBody = BaseEmailTemplate.Generate(
                OtpEmailTemplate.Subject,
                OtpEmailTemplate.GenerateBody(otp),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment),
                EmailFooterVariant.Otp
            );

            await SendEmailAsync(
                toEmail,
                OtpEmailTemplate.Subject,
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
            var htmlBody = BaseEmailTemplate.Generate(
                TrialRequestReceivedEmailTemplate.Subject,
                TrialRequestReceivedEmailTemplate.GenerateBody(
                    fullName,
                    businessName
                ),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
            );

            await SendEmailAsync(
                toEmail,
                TrialRequestReceivedEmailTemplate.Subject,
                htmlBody
            );
        }

        /*
         =========================================
         SEND ACCOUNT SETUP EMAIL
         =========================================
        */

        public async Task SendAccountSetupEmailAsync(
            string toEmail,
            string fullName,
            string setupLink
        )
        {
            _ = fullName;

            var htmlBody = BaseEmailTemplate.Generate(
                AccountSetupEmailTemplate.Subject,
                AccountSetupEmailTemplate.GenerateBody(setupLink),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
            );

            await SendEmailAsync(
                toEmail,
                AccountSetupEmailTemplate.Subject,
                htmlBody
            );
        }

        public async Task SendAccountSetupReminderEmailAsync(
            string toEmail,
            string fullName,
            string setupLink,
            DateTime expiresAtUtc
        )
        {
            _ = fullName;

            var htmlBody = BaseEmailTemplate.Generate(
                AccountSetupReminderEmailTemplate.Subject,
                AccountSetupReminderEmailTemplate.GenerateBody(
                    setupLink,
                    expiresAtUtc
                ),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
            );

            await SendEmailAsync(
                toEmail,
                AccountSetupReminderEmailTemplate.Subject,
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
            var htmlBody = BaseEmailTemplate.Generate(
                TrialDeclineEmailTemplate.Subject,
                TrialDeclineEmailTemplate.GenerateBody(fullName, declineReason),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
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
            var htmlBody = BaseEmailTemplate.Generate(
                TrialMoreInfoEmailTemplate.Subject,
                TrialMoreInfoEmailTemplate.GenerateBody(fullName, moreInfoMessage),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
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
            var htmlBody = BaseEmailTemplate.Generate(
                ResetPasswordEmailTemplate.Subject,
                ResetPasswordEmailTemplate.GenerateBody(resetLink),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment),
                EmailFooterVariant.Transactional
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
            var htmlBody = BaseEmailTemplate.Generate(
                PasswordChangedEmailTemplate.Subject,
                PasswordChangedEmailTemplate.GenerateBody(firstName),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment),
                EmailFooterVariant.Transactional
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
            var htmlBody = BaseEmailTemplate.Generate(
                NewDeviceSignInEmailTemplate.Subject,
                NewDeviceSignInEmailTemplate.GenerateBody(
                    details.FirstName,
                    details.SignInTime,
                    details.DeviceSummary,
                    details.LocationSummary
                ),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment),
                EmailFooterVariant.Otp
            );

            await SendEmailAsync(
                toEmail,
                NewDeviceSignInEmailTemplate.Subject,
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
            const string subject = "Reply from Tummly Support";
            var htmlBody = HelpCentreSupportReplyEmailTemplate.Generate(
                submitterName,
                topicLabel,
                replyBody,
                myQueriesUrl,
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
            );

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendHelpCentreResolvedEmailAsync(
            string toEmail,
            string submitterName,
            string topicLabel,
            IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages,
            string? myQueriesUrl
        )
        {
            var htmlBody = HelpCentreResolvedEmailTemplate.Generate(
                submitterName,
                topicLabel,
                excerptMessages,
                myQueriesUrl,
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
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
            const string subject = "Help Centre query escalated";
            var htmlBody = HelpCentreEscalationEmailTemplate.Generate(
                topicLabel,
                submitterName,
                submitterEmail,
                businessName,
                locationLabel,
                threadSummary,
                escalationNote,
                supportDashboardUrl,
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
            );

            await SendEmailAsync(toEmail, subject, htmlBody);
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

            const string subject = "Operator replied to Help Centre query";
            var htmlBody = HelpCentreOperatorReplyEmailTemplate.Generate(
                topicLabel,
                submitterName,
                submitterEmail,
                businessName,
                replyBody,
                supportDashboardUrl,
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
            );

            await SendEmailAsync(
                settings.SupportNotificationEmail,
                subject,
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

            var htmlBody = HelpCentreNewQueryEmailTemplate.Generate(
                topicLabel,
                submitterName,
                submitterEmail,
                businessName,
                locationLabel,
                messagePreview,
                attachmentCount,
                supportDashboardUrl,
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment)
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
            GuestResponseEmailOfferBlock? offer = null
        )
        {
            var htmlBody = GuestResponseEmailTemplate.Generate(
                brandTitle,
                brandSubtitle,
                locationAddress,
                subject,
                message,
                GetFrontendBaseUrl(),
                brandLogoUrl,
                offer
            );

            await SendEmailAsync(toEmail, subject, htmlBody);
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
            var htmlBody = BaseEmailTemplate.Generate(
                subject,
                TeamInvitationEmailTemplate.GenerateBody(
                    firstName,
                    inviterName,
                    workspaceName,
                    roleName,
                    locationScope,
                    invitationMessage,
                    acceptUrl
                ),
                GetFrontendBaseUrl(),
                EmailAssets.GetLogoDataUri(_environment),
                EmailFooterVariant.Transactional
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
            var htmlBody = BaseEmailTemplate.Generate(
                title,
                BillingAccountNoticeEmailTemplate.GenerateBody(
                    firstName,
                    title,
                    body,
                    ctaLabel,
                    ctaHref,
                    frontendBaseUrl
                ),
                frontendBaseUrl,
                EmailAssets.GetLogoDataUri(_environment),
                EmailFooterVariant.Transactional
            );

            await SendEmailAsync(toEmail, title, htmlBody);
        }

        public async Task SendTummlyVatInvoiceEmailAsync(
            string toEmail,
            string documentNumber,
            string lineDescription,
            int grossPence,
            byte[] pdfContent,
            string pdfFileName
        )
        {
            var subject = TummlyVatInvoiceEmailTemplate.Subject(documentNumber);
            var htmlBody = TummlyVatInvoiceEmailTemplate.GenerateHtml(
                documentNumber,
                lineDescription,
                grossPence,
                GetFrontendBaseUrl()
            );

            await SendEmailAsync(
                toEmail,
                subject,
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
