using System.Net;
using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class BillingAccountNoticeEmailTemplate
    {
        private const string Font = BaseEmailTemplate.FontFamily;

        public static string GenerateBody(
            string firstName,
            string title,
            string body,
            string? ctaLabel,
            string? ctaHref,
            string frontendBaseUrl
        )
        {
            var safeFirstName = WebUtility.HtmlEncode(firstName);
            var safeTitle = WebUtility.HtmlEncode(title);
            var safeBody = WebUtility.HtmlEncode(body);
            var ctaBlock = RenderCtaBlock(ctaLabel, ctaHref, frontendBaseUrl);

            return $@"
                <div style='display:block;{Font}'>
                    <h1 style='margin:0 0 32px;
                               font-size:30px;
                               font-weight:600;
                               line-height:1.2;
                               color:#141414;
                               {Font}'>
                        {safeTitle}
                    </h1>

                    {BaseEmailTemplate.RenderDivider()}

                    <div style='margin-top:32px;{Font}'>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Hi {safeFirstName},
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            {safeBody}
                        </p>

                        {ctaBlock}
                    </div>
                </div>";
        }

        private static string RenderCtaBlock(
            string? ctaLabel,
            string? ctaHref,
            string frontendBaseUrl
        )
        {
            if (
                string.IsNullOrWhiteSpace(ctaLabel)
                || string.IsNullOrWhiteSpace(ctaHref)
            )
            {
                return string.Empty;
            }

            var safeLabel = WebUtility.HtmlEncode(ctaLabel);
            var href = ctaHref.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? ctaHref
                : $"{frontendBaseUrl.TrimEnd('/')}{ctaHref}";
            var safeHref = WebUtility.HtmlEncode(href);

            return $@"
                        <p style='margin:24px 0 0;
                                  font-size:14px;
                                  line-height:20px;
                                  {Font}'>
                            <a href='{safeHref}'
                               style='display:inline-block;
                                      padding:12px 24px;
                                      background:#141414;
                                      color:#ffffff;
                                      text-decoration:none;
                                      border-radius:8px;
                                      font-weight:600;
                                      {Font}'>
                                {safeLabel}
                            </a>
                        </p>";
        }
    }
}
