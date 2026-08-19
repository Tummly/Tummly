using System.Net;
using TummlyBackend.Helpers;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Shared Guest-facing (non-transactional) email chrome — brand header,
    /// ticket, optional offer, legal footer, powered-by.
    /// Table layout. Chrome images are public HTTPS URLs so Resend and
    /// Gmail can paint them. Offer claim QR is a PNG data URI in the body.
    /// Tokens match Operator primitives: black #141414, gray-950 #2c2c2c,
    /// radius-xl 10px.
    /// </summary>
    public static class BaseNonTransactionalEmailTemplate
    {
        public const string EmptyValue = "—";

        public const string PublicLogoPath = "/email/logo.png";
        public const string PublicTopDecorationPath = "/email/top-decoration.png";

        public const string Font =
            "font-family:Arial,Helvetica,sans-serif;";

        public const string ColorBlack = "#141414";
        public const string ColorGray995 = "#1b1b1b";
        public const string ColorGray980 = "#262626";
        public const string ColorGray950 = "#2c2c2c";
        public const string ColorGray550 = "#7c7c7c";
        public const string ColorWhite = "#ffffff";

        /// <summary>
        /// Top-right pattern size. Keep the PNG aspect (786×464).
        /// Negative margin pulls brand + ticket over the image.
        /// </summary>
        public const int TopDecorationWidthPx = 560;
        public const int TopDecorationHeightPx = 330;

        public static string Generate(
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string? subject,
            string message,
            string frontendBaseUrl,
            string? brandLogoUrl,
            GuestResponseEmailOfferBlock? offer = null
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
            var topDecorationUrl = WebUtility.HtmlEncode(
                $"{baseUrl}{PublicTopDecorationPath}"
            );
            var poweredByLogoUrl = WebUtility.HtmlEncode(
                $"{baseUrl}{PublicLogoPath}"
            );

            var subtitleHtml = subtitle == null
                ? string.Empty
                : $@"
                          <p style='margin:0;font-size:12px;font-weight:600;line-height:normal;color:#cccccc;{Font}'>
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
                          <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='48' height='48' style='border-collapse:collapse;'>
                            <tr>
                              <td width='48' height='48' bgcolor='{ColorGray980}' style='width:48px;height:48px;background-color:{ColorGray980};border-radius:2px;font-size:0;line-height:0;'>&nbsp;</td>
                            </tr>
                          </table>"
                : $@"
                          <img src='{WebUtility.HtmlEncode(brandLogoUrl.Trim())}'
                               alt=''
                               width='48'
                               height='48'
                               style='display:block;width:48px;height:48px;border:0;border-radius:2px;' />";

            var offerHtml = RenderOfferBlock(offer);
            var perforationHtml = offer is null
                ? string.Empty
                : RenderPerforationRow();

            var disclaimer =
                $"You&#39;re receiving this because you joined {safeTitle} customer club after visiting or giving feedback.";
            var addressLine = $"{safeTitle}, {safeAddress}";

            return $@"
<!DOCTYPE html>
<html lang='en' style='{Font}'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>{safeTitle}</title>
</head>
<body style='margin:0;padding:0;background-color:{ColorBlack};{Font}'>
  <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse;background-color:{ColorBlack};{Font}'>
    <tr>
      <td align='center' bgcolor='{ColorBlack}' style='background-color:{ColorBlack};{Font}'>
        <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='600' style='border-collapse:collapse;width:100%;max-width:600px;background-color:{ColorBlack};{Font}'>
          <tr>
            <td data-guest-response-top-decoration='1' align='right' valign='top' style='padding:0;font-size:0;line-height:0;text-align:right;'>
              <img src='{topDecorationUrl}'
                   alt=''
                   width='{TopDecorationWidthPx}'
                   height='{TopDecorationHeightPx}'
                   style='display:block;width:{TopDecorationWidthPx}px;height:{TopDecorationHeightPx}px;border:0;' />
            </td>
          </tr>
          <tr>
            <td style='padding:0;font-size:0;line-height:0;'>
              <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse;margin-top:-{TopDecorationHeightPx}px;width:100%;{Font}'>
          <tr>
            <td data-non-transactional-slot='brand' style='padding:48px 32px 0 32px;{Font}'>
              <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='border-collapse:collapse;{Font}'>
                <tr>
                  <td valign='middle' style='vertical-align:middle;padding-right:12px;{Font}'>
                    {logoHtml}
                  </td>
                  <td valign='middle' style='vertical-align:middle;{Font}'>
                    <p style='margin:0 0 4px 0;font-size:22px;font-weight:600;line-height:normal;color:{ColorWhite};{Font}'>
                      {safeTitle}
                    </p>
                    {subtitleHtml}
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <tr>
            <td style='padding:40px 32px;{Font}'>
              <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' bgcolor='{ColorGray995}' style='border-collapse:collapse;border:1px solid {ColorGray980};border-radius:10px;background-color:{ColorGray995};{Font}'>
                <tr>
                  <td data-non-transactional-slot='ticket' style='padding:32px;{Font}'>
                    {subjectHtml}
                    <p style='margin:0;font-size:14px;font-weight:400;line-height:20px;color:{ColorWhite};{Font}'>
                      {messageHtml}
                    </p>
                  </td>
                </tr>
                {perforationHtml}
                {offerHtml}
              </table>
            </td>
          </tr>

          <tr>
            <td data-non-transactional-slot='legal' align='center' style='padding:32px 32px 48px;background-color:{ColorBlack};text-align:center;{Font}'>
              <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='440' style='border-collapse:collapse;max-width:440px;{Font}'>
                <tr>
                  <td align='center' style='text-align:center;{Font}'>
                    <p style='margin:0 0 12px 0;font-size:14px;font-weight:400;line-height:19px;color:{ColorWhite};{Font}'>
                      {disclaimer}
                    </p>
                    <p style='margin:0 0 26px 0;font-size:12px;font-weight:400;line-height:normal;color:{ColorWhite};{Font}'>
                      {addressLine}
                    </p>
                  </td>
                </tr>
                <tr>
                  <td height='1' bgcolor='{ColorGray980}' style='height:1px;background-color:{ColorGray980};font-size:0;line-height:0;'>&nbsp;</td>
                </tr>
                <tr>
                  <td align='center' style='padding-top:26px;text-align:center;{Font}'>
                    <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse;margin:0 auto;{Font}'>
                      <tr>
                        {RenderLegalLinkCell($"{baseUrl}/unsubscribe", "Unsubscribe", withGap: true)}
                        {RenderLegalLinkCell($"{baseUrl}/terms", "Terms", withGap: true)}
                        {RenderLegalLinkCell($"{baseUrl}/privacy", "Privacy", withGap: true)}
                        {RenderLegalLinkCell($"{baseUrl}/cookie-policy", "Cookie settings", withGap: false)}
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <tr>
            <td data-non-transactional-slot='poweredBy' align='center' style='padding:0 0 32px 0;text-align:center;background-color:{ColorBlack};{Font}'>
              <p style='margin:0;font-size:10px;font-weight:500;line-height:normal;color:{ColorWhite};{Font}'>
                Powered by
                <img src='{poweredByLogoUrl}'
                     alt='Tummly'
                     height='19'
                     style='display:inline-block;vertical-align:middle;height:19px;width:auto;border:0;margin-left:6px;' />
              </p>
            </td>
          </tr>
              </table>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
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

        private static string RenderPerforationRow()
        {
            return $@"
                <tr>
                  <td data-guest-response-notch='1' style='padding:0;font-size:0;line-height:0;'>
                    <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse;'>
                      <tr>
                        <td width='18' height='18' bgcolor='{ColorBlack}' style='width:18px;height:18px;background-color:{ColorBlack};border-radius:18px;font-size:0;line-height:0;'>&nbsp;</td>
                        <td style='border-bottom:1px dashed {ColorGray980};font-size:0;line-height:0;'>&nbsp;</td>
                        <td width='18' height='18' bgcolor='{ColorBlack}' style='width:18px;height:18px;background-color:{ColorBlack};border-radius:18px;font-size:0;line-height:0;'>&nbsp;</td>
                      </tr>
                    </table>
                  </td>
                </tr>";
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
                          <p style='margin:0;font-size:12px;font-weight:500;line-height:17px;color:#999999;text-align:center;{Font}'>
                            {WebUtility.HtmlEncode(offerDescription)}
                          </p>";

            var hasClaimCode = !string.IsNullOrWhiteSpace(offer.RedemptionCode);
            var qrHtml = hasClaimCode
                ? $@"
                      <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse;margin:0 auto 24px auto;'>
                        <tr>
                          <td data-guest-response-offer-qr='1' align='center' style='padding:0;font-size:0;line-height:0;'>
                            <img src='{OfferClaimQr.ToPngDataUri(redemptionCode)}'
                                 alt=''
                                 width='129'
                                 height='129'
                                 style='display:block;width:129px;height:129px;border:0;' />
                          </td>
                        </tr>
                      </table>"
                : string.Empty;

            return $@"
                <tr>
                  <td data-guest-response-offer='1' data-non-transactional-slot='offer' align='center' bgcolor='{ColorBlack}' style='padding:20px;border-radius:10px;background-color:{ColorBlack};text-align:center;{Font}'>
                    {qrHtml}
                    <p style='margin:0 0 12px 0;font-size:16px;font-weight:500;line-height:normal;color:{ColorWhite};text-align:center;{Font}'>
                      {WebUtility.HtmlEncode(offerTitle)}
                    </p>
                    {descriptionHtml}
                    <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse;margin:24px auto 0 auto;max-width:384px;border:1px solid {ColorGray950};border-radius:4px;{Font}'>
                      <tr>
                        <td style='padding:12px 13px;font-size:14px;font-weight:400;line-height:normal;color:{ColorGray550};{Font}'>
                          {WebUtility.HtmlEncode(redemptionCode)}
                        </td>
                        <td width='1' bgcolor='{ColorGray950}' style='width:1px;background-color:{ColorGray950};font-size:0;line-height:0;'>&nbsp;</td>
                        <td style='padding:12px;font-size:14px;font-weight:500;line-height:normal;color:{ColorGray550};white-space:nowrap;{Font}'>
                          Copy
                        </td>
                      </tr>
                    </table>
                    <p style='margin:10px 0 0 0;font-size:12px;font-weight:500;line-height:17px;color:#888888;text-align:center;{Font}'>
                      {WebUtility.HtmlEncode(expiryLabel)}
                    </p>
                  </td>
                </tr>";
        }
    }
}
