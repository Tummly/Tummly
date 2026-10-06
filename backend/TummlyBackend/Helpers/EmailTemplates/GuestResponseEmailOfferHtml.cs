using System.Net;
using TummlyBackend.Helpers;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Offer unlocked card HTML fragment injected into
    /// <c>{{offer_block}}</c> on guest-response React Email export
    /// (Figma Guest-Loop-MVP 6852:49466).
    /// </summary>
    public static class GuestResponseEmailOfferHtml
    {
        private const string Font = "font-family:Arial,Helvetica,sans-serif;";
        private const string EmptyValue = "—";
        private const string ColorBlack = "#141414";
        private const string ColorGray550 = "#7c7c7c";

        public static string Render(GuestResponseEmailOfferBlock? offer)
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
                ? "Expires: —"
                : offer.ExpiryLabel.Trim();
            var instruction = offerDescription.Length == 0
                ? "Show this QR code or offer code to the team on your next eligible visit."
                : offerDescription;

            var hasClaimCode = !string.IsNullOrWhiteSpace(offer.RedemptionCode);
            var qrHtml = hasClaimCode
                ? $@"
                      <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse;margin:0 auto 22px auto;'>
                        <tr>
                          <td data-guest-response-offer-qr='1' align='center' bgcolor='#ffffff' style='padding:12px;background-color:#ffffff;border-radius:4px;font-size:0;line-height:0;'>
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
                <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' bgcolor='{ColorBlack}' style='border-collapse:collapse;margin-top:30px;border-radius:8px;background-color:{ColorBlack};{Font}'>
                  <tr>
                    <td data-guest-response-offer='1' data-non-transactional-slot='offer' align='center' style='padding:40px 20px;border-radius:8px;background-color:{ColorBlack};text-align:center;{Font}'>
                      <p style='margin:0 0 8px 0;font-size:12px;font-weight:500;line-height:normal;color:#f4f4f4;text-align:center;{Font}'>
                        Your thank-you offer
                      </p>
                      <p style='margin:0 0 22px 0;font-size:24px;font-weight:800;line-height:normal;color:#f4f4f4;text-align:center;text-transform:uppercase;{Font}'>
                        {WebUtility.HtmlEncode(offerTitle)}
                      </p>
                      {qrHtml}
                      <p style='margin:0 0 40px 0;font-size:14px;font-weight:500;line-height:19px;color:rgba(244,244,244,0.4);text-align:center;{Font}'>
                        {WebUtility.HtmlEncode(instruction)}
                      </p>
                      <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse;margin:0 auto 14px auto;border:1px solid #2f2f30;border-radius:14px;background-color:rgba(54,54,56,0.15);{Font}'>
                        <tr>
                          <td align='center' style='padding:15px 22px;font-size:14px;font-weight:400;line-height:normal;color:{ColorGray550};text-align:center;{Font}'>
                            {WebUtility.HtmlEncode(redemptionCode)}
                          </td>
                        </tr>
                      </table>
                      <table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse;{Font}'>
                        <tr>
                          <td align='left' style='font-size:12px;font-weight:500;line-height:17px;color:rgba(244,244,244,0.5);text-align:left;{Font}'>
                            Terms apply
                          </td>
                          <td align='right' style='font-size:12px;font-weight:500;line-height:17px;color:rgba(244,244,244,0.5);text-align:right;{Font}'>
                            {WebUtility.HtmlEncode(expiryLabel)}
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>";
        }
    }
}
