using System.Net;
using TummlyBackend.Helpers;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Shared Guest-facing (non-transactional) email chrome — brand header,
    /// ticket, optional offer, legal footer, powered-by + green tear.
    /// Visual contract matches Guest preview React chrome.
    /// Tokens match Operator primitives: black #141414, gray-950 #2c2c2c,
    /// green-500 #14a74a, radius-xl 10px.
    /// </summary>
    public static class BaseNonTransactionalEmailTemplate
    {
        public const string EmptyValue = "—";

        public const string Font =
            "font-family:'Inter','Segoe UI',Roboto,Helvetica,Arial,sans-serif;";

        public const string ColorBlack = "#141414";
        public const string ColorGray995 = "#1b1b1b";
        public const string ColorGray980 = "#262626";
        public const string ColorGray950 = "#2c2c2c";
        public const string ColorGray550 = "#7c7c7c";
        public const string ColorWhite = "#ffffff";
        public const string ColorPrimary = "#14a74a";

        public static string Generate(
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string? subject,
            string message,
            string frontendBaseUrl,
            string tummlyLogoDataUri,
            string? brandLogoUrl,
            GuestResponseEmailOfferBlock? offer = null,
            string? topDecorationDataUri = null,
            string? bottomStripDataUri = null
        )
        {
            var title = string.IsNullOrWhiteSpace(brandTitle)
                ? EmptyValue
                : brandTitle.Trim();
            var safeTitle = WebUtility.HtmlEncode(title);
            var subtitle = string.IsNullOrWhiteSpace(brandSubtitle)
                ? null
                : brandSubtitle.Trim();
            var trimmedSubject = subject?.Trim() ?? string.Empty;
            var trimmedMessage = string.IsNullOrWhiteSpace(message)
                ? EmptyValue
                : message.Trim();
            var address = string.IsNullOrWhiteSpace(locationAddress)
                ? EmptyValue
                : locationAddress.Trim();
            var safeAddress = WebUtility.HtmlEncode(address);
            var baseUrl = frontendBaseUrl.Trim().TrimEnd('/');

            var subtitleHtml = subtitle == null
                ? string.Empty
                : $@"
            <p style='margin:0;font-size:12px;font-weight:600;line-height:normal;color:rgba(255,255,255,0.8);{Font}'>
              {WebUtility.HtmlEncode(subtitle)}
            </p>";

            var subjectHtml = trimmedSubject.Length == 0
                ? string.Empty
                : $@"
            <p data-guest-response-subject='1' style='margin:0 0 12px 0;font-size:14px;font-weight:600;line-height:20px;color:{ColorWhite};{Font}'>
              {WebUtility.HtmlEncode(trimmedSubject)}
            </p>";

            var messageHtml = WebUtility.HtmlEncode(trimmedMessage)
                .Replace("\r\n", "\n")
                .Replace("\n", "<br />");

            var logoHtml = string.IsNullOrWhiteSpace(brandLogoUrl)
                ? $@"
            <div style='width:48px;height:48px;border-radius:2px;background:{ColorGray980};'></div>"
                : $@"
            <img src='{WebUtility.HtmlEncode(brandLogoUrl.Trim())}'
                 alt=''
                 width='48'
                 height='48'
                 style='display:block;width:48px;height:48px;border:0;border-radius:2px;object-fit:cover;' />";

            var offerHtml = RenderOfferBlock(offer);
            var ticketBodyMargin = offer is null ? "0" : "0 0 30px 0";

            var disclaimer =
                $"You&#39;re receiving this because you joined {safeTitle} customer club after visiting or giving feedback.";
            var addressLine = $"{safeTitle}, {safeAddress}";

            var topDecorationHtml = RenderTopDecoration(topDecorationDataUri);
            var bottomStripHtml = RenderBottomStrip(bottomStripDataUri);

            return $@"
<!DOCTYPE html>
<html lang='en' style='{Font}'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>{safeTitle}</title>
  {BaseEmailTemplate.RenderFontHead()}
</head>
<body style='margin:0;padding:0;background-color:{ColorBlack};{Font}'>
  <div style='max-width:600px;margin:0 auto;background-color:{ColorBlack};overflow:hidden;position:relative;{Font}'>
    {topDecorationHtml}
    <div data-non-transactional-slot='brand' style='padding:62px 52px 0 32px;background-color:{ColorBlack};position:relative;z-index:1;{Font}'>
      <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='border-collapse:collapse;{Font}'>
        <tr>
          <td style='vertical-align:middle;padding-right:12px;{Font}'>
            {logoHtml}
          </td>
          <td style='vertical-align:middle;{Font}'>
            <p style='margin:0 0 4px 0;font-size:22px;font-weight:600;line-height:normal;color:{ColorWhite};{Font}'>
              {safeTitle}
            </p>
            {subtitleHtml}
          </td>
        </tr>
      </table>
    </div>

    <div style='padding:40px 32px;background-color:{ColorBlack};position:relative;z-index:1;{Font}'>
      <div style='position:relative;padding:32px;border:1px solid {ColorGray980};border-radius:10px;background-color:{ColorGray995};{Font}'>
        <div data-non-transactional-slot='ticket' style='margin:{ticketBodyMargin};{Font}'>
          {subjectHtml}
          <p style='margin:0;font-size:14px;font-weight:400;line-height:20px;color:{ColorWhite};white-space:normal;{Font}'>
            {messageHtml}
          </p>
        </div>
        {offerHtml}
        <div data-guest-response-notch='1' style='position:absolute;left:-12px;top:50%;width:18px;height:18px;margin-top:-9px;border-radius:20px;background-color:{ColorBlack};'></div>
        <div data-guest-response-notch='1' style='position:absolute;right:-12px;top:50%;width:18px;height:18px;margin-top:-9px;border-radius:20px;background-color:{ColorBlack};'></div>
      </div>
    </div>

    <div data-non-transactional-slot='legal' style='padding:32px 32px 60px;background-color:{ColorBlack};text-align:center;position:relative;z-index:1;{Font}'>
      <div style='max-width:440px;margin:0 auto 26px auto;{Font}'>
        <p style='margin:0 0 12px 0;font-size:14px;font-weight:400;line-height:19px;color:{ColorWhite};{Font}'>
          {disclaimer}
        </p>
        <p style='margin:0;font-size:12px;font-weight:400;line-height:normal;color:{ColorWhite};{Font}'>
          {addressLine}
        </p>
      </div>
      <div style='height:1px;background-color:{ColorGray980};margin:0 0 26px 0;'></div>
      <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse;margin:0 auto;{Font}'>
        <tr>
          {RenderLegalLinkCell($"{baseUrl}/unsubscribe", "Unsubscribe", withGap: true)}
          {RenderLegalLinkCell($"{baseUrl}/terms", "Terms", withGap: true)}
          {RenderLegalLinkCell($"{baseUrl}/privacy", "Privacy", withGap: true)}
          {RenderLegalLinkCell($"{baseUrl}/cookie-policy", "Cookie settings", withGap: false)}
        </tr>
      </table>
    </div>

    <div data-non-transactional-slot='poweredBy' style='text-align:center;padding-bottom:0;position:relative;z-index:1;{Font}'>
      <p style='margin:0 0 8px 0;font-size:10px;font-weight:500;line-height:normal;color:{ColorWhite};{Font}'>
        Powered by
        <img src='{tummlyLogoDataUri}'
             alt='Tummly'
             height='19'
             style='display:inline-block;vertical-align:middle;height:19px;width:auto;border:0;margin-left:6px;' />
      </p>
      {bottomStripHtml}
    </div>
  </div>
</body>
</html>";
        }

        private static string RenderLegalLinkCell(
            string href,
            string label,
            bool withGap
        )
        {
            var padding = withGap ? "padding-right:20px;" : "";
            return $@"
          <td style='{padding}vertical-align:top;{Font}'>
            <a href='{href}'
               style='color:{ColorGray550};font-size:12px;font-weight:500;line-height:normal;text-decoration:none;white-space:nowrap;{Font}'>
              {label}
            </a>
          </td>";
        }

        private static string RenderTopDecoration(string? topDecorationDataUri)
        {
            if (string.IsNullOrWhiteSpace(topDecorationDataUri))
            {
                return @"
    <div data-guest-response-top-decoration='1' style='display:none;'></div>";
            }

            var safeSrc = WebUtility.HtmlEncode(topDecorationDataUri.Trim());
            return $@"
    <div data-guest-response-top-decoration='1' style='position:absolute;right:0;top:0;height:138px;width:314px;overflow:hidden;pointer-events:none;z-index:0;'>
      <img src='{safeSrc}'
           alt=''
           width='314'
           height='138'
           style='display:block;width:314px;height:138px;border:0;object-fit:cover;' />
      <div style='position:absolute;inset:0;background-image:linear-gradient(19.66deg, {ColorBlack} 25.8%, transparent 110%), linear-gradient(37.61deg, {ColorBlack} 32.9%, transparent 71.4%);'></div>
    </div>";
        }

        private static string RenderBottomStrip(string? bottomStripDataUri)
        {
            if (string.IsNullOrWhiteSpace(bottomStripDataUri))
            {
                return $@"
      <div data-guest-response-footer-strip='1' style='height:56px;width:100%;background-color:{ColorPrimary};'></div>";
            }

            var safeSrc = WebUtility.HtmlEncode(bottomStripDataUri.Trim());
            return $@"
      <div data-guest-response-footer-strip='1' style=""height:56px;width:100%;overflow:hidden;background-color:{ColorPrimary};font-size:0;line-height:0;"">
        <div style=""height:112px;width:100%;margin-top:-28px;background-image:url({safeSrc});background-repeat:repeat-x;background-size:110% 100%;background-position:center center;-webkit-transform:rotate(180deg);-ms-transform:rotate(180deg);transform:rotate(180deg);""></div>
      </div>";
        }

        private static string RenderOfferBlock(GuestResponseEmailOfferBlock? offer)
        {
            if (offer is null)
            {
                return string.Empty;
            }

            var offerTitle = string.IsNullOrWhiteSpace(offer.Title)
                ? EmptyValue
                : offer.Title.Trim();
            var offerDescription = offer.Description?.Trim() ?? string.Empty;
            var redemptionCode = string.IsNullOrWhiteSpace(offer.RedemptionCode)
                ? EmptyValue
                : offer.RedemptionCode.Trim();
            var expiryLabel = string.IsNullOrWhiteSpace(offer.ExpiryLabel)
                ? EmptyValue
                : offer.ExpiryLabel.Trim();

            var descriptionHtml = offerDescription.Length == 0
                ? string.Empty
                : $@"
          <p style='margin:0;max-width:364px;font-size:12px;font-weight:500;line-height:17px;color:rgba(255,255,255,0.4);text-align:center;{Font}'>
            {WebUtility.HtmlEncode(offerDescription)}
          </p>";

            var claimCodeForQr = string.IsNullOrWhiteSpace(offer.RedemptionCode)
                ? null
                : offer.RedemptionCode.Trim();
            var qrHtml = claimCodeForQr == null
                ? string.Empty
                : $@"
          <div data-guest-response-offer-qr='1' style='margin:0 auto 33px auto;width:129px;height:129px;{Font}'>
            <img src='{WebUtility.HtmlEncode(OfferClaimQr.ToPngDataUri(claimCodeForQr))}'
                 alt=''
                 width='129'
                 height='129'
                 style='display:block;width:129px;height:129px;border:0;object-fit:contain;' />
          </div>";

            return $@"
        <div data-guest-response-offer='1' data-non-transactional-slot='offer' style='padding:20px;border-radius:10px;background-color:{ColorBlack};text-align:center;{Font}'>
          {qrHtml}
          <div style='margin:0 0 33px 0;{Font}'>
            <p style='margin:0 0 12px 0;font-size:16px;font-weight:500;line-height:normal;color:{ColorWhite};text-align:center;{Font}'>
              {WebUtility.HtmlEncode(offerTitle)}
            </p>
            {descriptionHtml}
          </div>
          <div style='max-width:384px;margin:0 auto;text-align:left;{Font}'>
            <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse;border:1px solid {ColorGray950};border-radius:4px;background-color:rgba(52,52,52,0.15);{Font}'>
              <tr>
                <td style='padding:12px 13px;font-size:14px;font-weight:400;line-height:normal;color:{ColorGray550};{Font}'>
                  {WebUtility.HtmlEncode(redemptionCode)}
                </td>
                <td style='width:1px;border-left:1px solid {ColorGray950};background-color:rgba(52,52,52,0.15);'></td>
                <td style='padding:12px 12px 12px 13px;font-size:14px;font-weight:500;line-height:normal;color:{ColorGray550};white-space:nowrap;{Font}'>
                  Copy
                </td>
              </tr>
            </table>
            <p style='margin:10px 0 0 0;font-size:12px;font-weight:500;line-height:17px;color:rgba(255,255,255,0.5);text-align:center;{Font}'>
              {WebUtility.HtmlEncode(expiryLabel)}
            </p>
          </div>
        </div>";
        }
    }
}
