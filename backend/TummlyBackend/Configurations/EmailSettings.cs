namespace TummlyBackend.Configurations
{
    public class EmailSettings
    {
        /// <summary>
        /// Resend API key (re_...). When set, emails are sent via HTTPS (works on Railway Hobby).
        /// </summary>
        public string? ApiKey { get; set; }

        public string SmtpServer { get; set; } = string.Empty;

        public int Port { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Inbox for replies (e.g. engineering@tummly.com). Used as Resend reply_to.
        /// </summary>
        public string? ReplyToEmail { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
