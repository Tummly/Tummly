using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Shared HTML fragments for React Email token injection.
    /// </summary>
    public static class ReactEmailHtmlFragments
    {
        private const string Font =
            "font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;";

        public static string AdminFeedbackBlock(string heading, string message)
        {
            var safeHeading = WebUtility.HtmlEncode(heading);
            var safeMessage = WebUtility
                .HtmlEncode(message.Trim())
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\n", "<br />", StringComparison.Ordinal);

            return $@"
                    <p style='margin:0 0 8px;font-size:14px;font-weight:600;line-height:20px;color:#141414;{Font}'>
                        {safeHeading}
                    </p>
                    <div style='margin:0 0 14px;padding:16px;background-color:#f9f9fa;border-left:4px solid #141414;{Font}'>
                        <p style='margin:0;font-size:14px;line-height:20px;color:#141414;{Font}'>
                            {safeMessage}
                        </p>
                    </div>";
        }

        public static string CtaButton(string label, string href)
        {
            return $@"
                        <p style='margin:24px 0 0;{Font}'>
                            <a href='{WebUtility.HtmlEncode(href)}'
                               target='_blank'
                               rel='noopener noreferrer'
                               style='background-color:#14a74a;color:#ffffff;display:inline-block;padding:15px 17px;border-radius:4px;font-size:16px;font-weight:500;line-height:20px;text-decoration:none;text-align:center;{Font}'>
                                {WebUtility.HtmlEncode(label)}
                            </a>
                        </p>";
        }

        public static string DetailRow(string label, string valueHtml)
        {
            return $@"
                <table style='width:100%;border-collapse:collapse;margin:0 0 8px 0;'>
                    <tr>
                        <td style='padding:8px 0;font-size:14px;color:#7d7d7d;width:140px;'>{WebUtility.HtmlEncode(label)}</td>
                        <td style='padding:8px 0;font-size:14px;color:#141414;'>{valueHtml}</td>
                    </tr>
                </table>";
        }

        public static string MyQueriesBlock(string? myQueriesUrl, string linkLabel)
        {
            if (string.IsNullOrWhiteSpace(myQueriesUrl))
            {
                return string.Empty;
            }

            return $@"
                    <p style='margin:24px 0 0 0;font-size:16px;line-height:24px;color:#141414;'>
                        You can view this conversation and reply in
                        <a href='{WebUtility.HtmlEncode(myQueriesUrl)}' style='color:#141414;text-decoration:underline;'>{WebUtility.HtmlEncode(linkLabel)}</a>.
                    </p>";
        }

        public static string ExtractFirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "there";
            }

            var firstToken = fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

            return string.IsNullOrWhiteSpace(firstToken) ? "there" : firstToken;
        }
    }
}
