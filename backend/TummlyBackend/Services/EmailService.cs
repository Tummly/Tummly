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
            string htmlBody
        )
        {
            if (UsesResend)
            {
                await SendViaResendAsync(
                    toEmail,
                    subject,
                    htmlBody
                );
                return;
            }

            await SendViaSmtpAsync(
                toEmail,
                subject,
                htmlBody
            );
        }

        private async Task SendViaResendAsync(
            string toEmail,
            string subject,
            string htmlBody
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
                <strong>{toEmail}</strong>. Check this inbox for the OTP;
                verification still uses the address entered on the form.
                </div>";

            return (redirectTo, banner + htmlBody);
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
            string htmlBody
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

            email.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var smtp =
                await CreateSmtpClientAsync();

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
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
        }
    }
}
