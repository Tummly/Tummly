using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IHttpClientFactory httpClientFactory
        )
        {
            _emailSettings = emailSettings.Value;
            _httpClientFactory = httpClientFactory;
        }

        private bool UsesResend =>
            !string.IsNullOrWhiteSpace(_emailSettings.ApiKey);

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
            var payload = new ResendEmailPayload
            {
                From = FormatFromAddress(),
                To = [toEmail],
                Subject = subject,
                Html = htmlBody,
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
            var htmlBody =
                BaseEmailTemplate.GenerateTemplate(
                    "Your Tummly Verification Code",
                    @"
                    <p class='text'>
                    Use this code to verify your email:
                    </p>
                    ",
                    otp,
                    "This verification code expires in 10 minutes."
                );

            await SendEmailAsync(
                toEmail,
                "Your Tummly Verification Code",
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
            var htmlBody =
                BaseEmailTemplate.GenerateTemplate(
                    "Your Tummly Account Has Been Approved",
                    $@"
                    <p class='text'>
                    Hello {fullName},
                    </p>

                    <p class='text'>
                    Your Tummly request has been approved.
                    </p>

                    <p class='text'>
                    Click below to setup your account:
                    </p>

                    <p style='margin-top:30px;'>
                    <a href='{setupLink}'
                    style='
                    background:#000;
                    color:#fff;
                    padding:14px 24px;
                    text-decoration:none;
                    border-radius:8px;
                    font-weight:600;
                    '>
                    Setup Account
                    </a>
                    </p>
                    ",
                    "Account Approved",
                    "This invitation link expires in 14 days."
                );

            await SendEmailAsync(
                toEmail,
                "Your Tummly Account Setup Invitation",
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
            string fullName
        )
        {
            var htmlBody =
                BaseEmailTemplate.GenerateTemplate(
                    "Trial Request Update",
                    $@"
                    <p class='text'>
                    Hello {fullName},
                    </p>

                    <p class='text'>
                    Thank you for your interest.
                    Unfortunately, your trial request
                    cannot be approved at this time.
                    </p>
                    ",
                    "Request Declined",
                    "If you have questions, please contact our support team."
                );

            await SendEmailAsync(
                toEmail,
                "Update on your Tummly Trial Request",
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
            string fullName
        )
        {
            var htmlBody =
                BaseEmailTemplate.GenerateTemplate(
                    "More Information Needed",
                    $@"
                    <p class='text'>
                    Hello {fullName},
                    </p>

                    <p class='text'>
                    Our team needs a few more details
                    before activating your trial.
                    Please reply to this email with your
                    restaurant's physical address or
                    business registration details.
                    </p>
                    ",
                    "More Info Requested",
                    "We will process your application as soon as you reply."
                );

            await SendEmailAsync(
                toEmail,
                "Action Required: Tummly Trial Request",
                htmlBody
            );
        }

        public async Task SendResetPasswordEmailAsync(
            string toEmail,
            string resetLink
        )
        {
            var htmlBody =
                BaseEmailTemplate.GenerateTemplate(
                    "Reset Your Password",
                    $@"
            <p class='text'>
            We received a request to reset your password.
            </p>

            <p class='text'>
            Click the button below to create a new password:
            </p>

            <p style='margin-top:30px;'>
            <a href='{resetLink}'
            style='
            background:#16A34A;
            color:#fff;
            padding:14px 24px;
            text-decoration:none;
            border-radius:8px;
            font-weight:600;
            display:inline-block;
            '>
            Reset Password
            </a>
            </p>

            <p class='text'>
            If you did not request this,
            you can safely ignore this email.
            </p>
            "
                );

            await SendEmailAsync(
                toEmail,
                "Reset Your Tummly Password",
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
