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

        public EmailService(
            IOptions<EmailSettings> emailSettings
        )
        {
            _emailSettings = emailSettings.Value;
        }

        /*
         =========================================
         CREATE SMTP CLIENT
         =========================================
        */

        private async Task<SmtpClient> CreateSmtpClientAsync()
        {
            var smtp = new SmtpClient();

            /*
             =========================================
             DEVELOPMENT SSL FIX
             =========================================
            */

            smtp.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            await smtp.ConnectAsync(
                _emailSettings.SmtpServer,
                _emailSettings.Port,
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _emailSettings.Username,
                _emailSettings.Password
            );

            return smtp;
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
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail
                )
            );

            email.To.Add(
                MailboxAddress.Parse(toEmail)
            );

            email.Subject =
                "Your Tummly Verification Code";

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
         SEND ACCOUNT SETUP EMAIL
         =========================================
        */

        public async Task SendAccountSetupEmailAsync(
            string toEmail,
            string fullName,
            string setupLink
        )
        {
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail
                )
            );

            email.To.Add(
                MailboxAddress.Parse(toEmail)
            );

            email.Subject =
                "Your Tummly Account Setup Invitation";

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
         SEND DECLINE EMAIL
         =========================================
        */

        public async Task SendDeclineEmailAsync(
            string toEmail,
            string fullName
        )
        {
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail
                )
            );

            email.To.Add(
                MailboxAddress.Parse(toEmail)
            );

            email.Subject =
                "Update on your Tummly Trial Request";

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
         SEND MORE INFO REQUEST EMAIL
         =========================================
        */

        public async Task SendMoreInfoEmailAsync(
            string toEmail,
            string fullName
        )
        {
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail
                )
            );

            email.To.Add(
                MailboxAddress.Parse(toEmail)
            );

            email.Subject =
                "Action Required: Tummly Trial Request";

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

            email.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var smtp =
                await CreateSmtpClientAsync();

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
        public async Task SendResetPasswordEmailAsync(
    string toEmail,
    string resetLink
)
        {
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail
                )
            );

            email.To.Add(
                MailboxAddress.Parse(toEmail)
            );

            email.Subject =
                "Reset Your Tummly Password";

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

            email.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var smtp =
                await CreateSmtpClientAsync();

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
    }
}