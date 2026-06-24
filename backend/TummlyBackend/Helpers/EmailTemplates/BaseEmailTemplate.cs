using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public enum EmailFooterVariant
    {
        Standard,
        Otp,
        Transactional,
    }

    public static class BaseEmailTemplate
    {
        private const string SupportEmail = "support@tummly.com";

        private const string FooterLegalLinkGap = "32px";

        public const string FontFamily =
            "font-family:'Inter','Segoe UI',Roboto,Helvetica,Arial,sans-serif;";

        public static string RenderFontHead()
        {
            return @"
    <link rel='preconnect' href='https://fonts.googleapis.com' />
    <link rel='preconnect' href='https://fonts.gstatic.com' crossorigin />
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet' />
    <style>
        html, body, table, td, th, tr, p, a, h1, h2, h3, h4, span, div, li, strong, em {
            font-family: 'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
        }
    </style>";
        }

        public static string Generate(
            string pageTitle,
            string bodyHtml,
            string frontendBaseUrl,
            string logoDataUri,
            EmailFooterVariant footerVariant = EmailFooterVariant.Standard
        )
        {
            var baseUrl = frontendBaseUrl.Trim().TrimEnd('/');
            var footerHtml = footerVariant switch
            {
                EmailFooterVariant.Otp => RenderOtpFooter(baseUrl),
                EmailFooterVariant.Transactional => RenderTransactionalFooter(baseUrl),
                _ => RenderStandardFooter(baseUrl),
            };

            return $@"
<!DOCTYPE html>
<html lang='en' style='{FontFamily}'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>{WebUtility.HtmlEncode(pageTitle)}</title>
    {RenderFontHead()}
</head>
<body style='margin:0;
             padding:0;
             background-color:#ffffff;
             {FontFamily}'>
    <div style='max-width:600px;
                margin:0 auto;
                background-color:#ffffff;
                overflow:hidden;
                {FontFamily}'>
        <div style='background-color:#141414;
                    min-height:84px;
                    padding:32px;
                    box-sizing:border-box;
                    {FontFamily}'>
            <img src='{logoDataUri}'
                 alt='Tummly'
                 width='139'
                 height='35'
                 style='display:block;border:0;max-width:139px;height:auto;' />
        </div>

        <div style='padding:48px 32px;{FontFamily}'>
            {bodyHtml}
        </div>

        {footerHtml}
    </div>
</body>
</html>";
        }

        private static string RenderStandardFooter(string baseUrl)
        {
            var helpCenterUrl = $"{baseUrl}/help-center";
            var termsUrl = $"{baseUrl}/terms";
            var privacyUrl = $"{baseUrl}/privacy";
            var cookieSettingsUrl = $"{baseUrl}/cookie-settings";

            return $@"
        <div style='background-color:#f9f9fa;
                    padding:38px 32px;
                    {FontFamily}'>
            <div style='margin-bottom:26px;{FontFamily}'>
                <p style='margin:0 0 12px;
                          font-size:16px;
                          font-weight:600;
                          line-height:24px;
                          color:#141414;
                          {FontFamily}'>
                    Need help?
                </p>
                <p style='margin:0;
                          font-size:14px;
                          line-height:20px;
                          color:#141414;
                          {FontFamily}'>
                    <a href='mailto:{SupportEmail}'
                       style='color:#141414;text-decoration:underline;{FontFamily}'>
                        Contact support
                    </a>
                    or visit the
                    <a href='{helpCenterUrl}'
                       target='_blank'
                       rel='noopener noreferrer'
                       style='color:#141414;text-decoration:underline;{FontFamily}'>
                        Help Centre
                    </a>.
                </p>
            </div>

            {RenderDivider()}

            <div style='margin-top:26px;{FontFamily}'>
                {RenderLegalFooterLinks(
                    helpCenterUrl,
                    termsUrl,
                    privacyUrl,
                    cookieSettingsUrl
                )}
            </div>
        </div>";
        }

        private static string RenderLegalFooterLinks(
            string helpCenterUrl,
            string termsUrl,
            string privacyUrl,
            string cookieSettingsUrl
        )
        {
            return $@"
                <table role='presentation'
                       cellpadding='0'
                       cellspacing='0'
                       border='0'
                       style='border-collapse:collapse;{FontFamily}'>
                    <tr>
                        {RenderLegalFooterLinkCell(helpCenterUrl, "Help Centre", withGap: true)}
                        {RenderLegalFooterLinkCell(termsUrl, "Terms", withGap: true)}
                        {RenderLegalFooterLinkCell(privacyUrl, "Privacy", withGap: true)}
                        {RenderLegalFooterLinkCell(cookieSettingsUrl, "Cookie settings", withGap: false)}
                    </tr>
                </table>";
        }

        private static string RenderLegalFooterLinkCell(
            string href,
            string label,
            bool withGap
        )
        {
            var cellPadding =
                withGap ? $"padding-right:{FooterLegalLinkGap};" : "";

            return $@"
                        <td style='{cellPadding}vertical-align:top;{FontFamily}'>
                            <a href='{href}'
                               target='_blank'
                               rel='noopener noreferrer'
                               style='color:#7d7d7d;
                                      font-size:12px;
                                      font-weight:500;
                                      line-height:15px;
                                      text-decoration:none;
                                      white-space:nowrap;
                                      {FontFamily}'>
                                {label}
                            </a>
                        </td>";
        }

        private static string RenderOtpFooter(string baseUrl)
        {
            return $@"
        <div style='background-color:#f9f9fa;
                    padding:48px 32px 38px;
                    {FontFamily}'>
            <div style='margin-bottom:26px;{FontFamily}'>
                {RenderHelpBlock(baseUrl)}
            </div>

            <p style='margin:0;
                      font-size:12px;
                      font-weight:500;
                      line-height:normal;
                      color:#7d7d7d;
                      {FontFamily}'>
                If you did not try to sign in to Tummly, contact support.
            </p>
        </div>";
        }

        private static string RenderTransactionalFooter(string baseUrl)
        {
            return $@"
        <div style='background-color:#f9f9fa;
                    padding:48px 32px 38px;
                    {FontFamily}'>
            {RenderHelpBlock(baseUrl)}
        </div>";
        }

        private static string RenderHelpBlock(string baseUrl)
        {
            var helpCenterUrl = $"{baseUrl}/help-center";

            return $@"
            <div style='{FontFamily}'>
                <p style='margin:0 0 12px;
                          font-size:16px;
                          font-weight:600;
                          line-height:24px;
                          color:#141414;
                          {FontFamily}'>
                    Need help?
                </p>
                <p style='margin:0;
                          font-size:14px;
                          line-height:20px;
                          color:#141414;
                          {FontFamily}'>
                    Contact us at
                    <a href='mailto:{SupportEmail}'
                       style='color:#141414;text-decoration:underline;{FontFamily}'>
                        {SupportEmail}
                    </a>
                    or visit our
                    <a href='{helpCenterUrl}'
                       target='_blank'
                       rel='noopener noreferrer'
                       style='color:#141414;text-decoration:underline;{FontFamily}'>
                        Help Centre
                    </a>.
                </p>
            </div>";
        }

        public static string RenderDivider()
        {
            return @"<div style='height:1px;background-color:#e8e8e8;width:100%;line-height:1px;font-size:1px;'>&nbsp;</div>";
        }
    }
}
