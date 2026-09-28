import { NonTransactionalEmailShell } from "./_components/NonTransactionalEmailShell"

export type GuestResponseEmailProps = {
  brandTitle: string
  brandSubtitle: string
  brandLogoUrl: string
  /** Full optional subject paragraph HTML, or empty. */
  subjectBlock: string
  messageHtml: string
  /** Full optional offer section HTML, or empty. */
  offerBlock: string
  disclaimer: string
  addressLine: string
  unsubscribeUrl: string
  termsUrl: string
  privacyUrl: string
  cookieUrl: string
  topDecorationUrl: string
  poweredByLogoUrl: string
  bottomStripUrl: string
  preview: string
}

const tokenDefaults = {
  brandTitle: "{{brand_title}}",
  brandSubtitle: "{{brand_subtitle}}",
  brandLogoUrl: "{{brand_logo_url}}",
  subjectBlock: "{{subject_block}}",
  messageHtml: "{{message_html}}",
  offerBlock: "{{offer_block}}",
  disclaimer: "{{disclaimer}}",
  addressLine: "{{address_line}}",
  unsubscribeUrl: "{{unsubscribe_url}}",
  termsUrl: "{{terms_url}}",
  privacyUrl: "{{privacy_url}}",
  cookieUrl: "{{cookie_url}}",
  topDecorationUrl: "{{top_decoration_url}}",
  poweredByLogoUrl: "{{powered_by_logo_url}}",
  bottomStripUrl: "{{bottom_strip_url}}",
  preview: "{{preview}}",
} satisfies GuestResponseEmailProps

const bodyText = {
  margin: "0 0 30px 0",
  fontSize: "14px",
  fontWeight: 400,
  lineHeight: "20px",
  color: "#ffffff",
  fontFamily: "Arial, Helvetica, sans-serif",
} as const

/**
 * Guest response / offer unlocked — Figma Guest-Loop-MVP node 6852:49917.
 * Offer block is optional: C# leaves `{{offer_block}}` empty when no offer.
 * Email send requires email-marketing opt-in for the Email channel.
 */
export default function GuestResponseEmail({
  brandTitle = tokenDefaults.brandTitle,
  brandSubtitle = tokenDefaults.brandSubtitle,
  brandLogoUrl = tokenDefaults.brandLogoUrl,
  subjectBlock = tokenDefaults.subjectBlock,
  messageHtml = tokenDefaults.messageHtml,
  offerBlock = tokenDefaults.offerBlock,
  disclaimer = tokenDefaults.disclaimer,
  addressLine = tokenDefaults.addressLine,
  unsubscribeUrl = tokenDefaults.unsubscribeUrl,
  termsUrl = tokenDefaults.termsUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  cookieUrl = tokenDefaults.cookieUrl,
  topDecorationUrl = tokenDefaults.topDecorationUrl,
  poweredByLogoUrl = tokenDefaults.poweredByLogoUrl,
  bottomStripUrl = tokenDefaults.bottomStripUrl,
  preview = tokenDefaults.preview,
}: GuestResponseEmailProps = tokenDefaults) {
  return (
    <NonTransactionalEmailShell
      preview={preview}
      brandTitle={brandTitle}
      brandSubtitle={brandSubtitle}
      brandLogoUrl={brandLogoUrl}
      topDecorationUrl={topDecorationUrl}
      poweredByLogoUrl={poweredByLogoUrl}
      bottomStripUrl={bottomStripUrl}
      disclaimer={disclaimer}
      addressLine={addressLine}
      unsubscribeUrl={unsubscribeUrl}
      termsUrl={termsUrl}
      privacyUrl={privacyUrl}
      cookieUrl={cookieUrl}
      offerBlock={
        <div
          data-offer-block-slot="1"
          dangerouslySetInnerHTML={{ __html: offerBlock }}
        />
      }
    >
      <div dangerouslySetInnerHTML={{ __html: subjectBlock }} />
      <div
        style={bodyText}
        dangerouslySetInnerHTML={{ __html: messageHtml }}
      />
    </NonTransactionalEmailShell>
  )
}

GuestResponseEmail.PreviewProps = {
  brandTitle: "KFC",
  brandSubtitle: "Camden High Street",
  brandLogoUrl: "/static/brand-logo-placeholder.png",
  subjectBlock: "",
  messageHtml:
    "Hi Alex,<br /><br />Thank you for sharing your feedback with KFC.<br /><br />We are sorry your visit was not right. The team at Camden will use your notes to improve.<br /><br />Thanks,<br />The KFC Team",
  offerBlock: `
    <div data-guest-response-offer="1" data-non-transactional-slot="offer" style="margin-top:0;padding:40px 20px;border-radius:8px;background-color:#141414;text-align:center;font-family:Arial,Helvetica,sans-serif;">
      <p style="margin:0 0 8px 0;font-size:12px;font-weight:500;line-height:normal;color:#f4f4f4;">Your thank-you offer</p>
      <p style="margin:0 0 22px 0;font-size:24px;font-weight:800;line-height:normal;color:#f4f4f4;text-transform:uppercase;">15% OFF YOUR NEXT ORDER</p>
      <div style="margin:0 auto 22px auto;padding:12px;background:#ffffff;border-radius:4px;display:inline-block;line-height:0;">
        <div style="width:129px;height:129px;background:#111111;"></div>
      </div>
      <p style="margin:0 0 40px 0;font-size:14px;font-weight:500;line-height:19px;color:rgba(244,244,244,0.4);">Show this QR code or offer code to the team on your next eligible visit.</p>
      <div style="margin:0 0 14px 0;padding:15px 22px;border:1px solid #2f2f30;border-radius:14px;background:rgba(54,54,56,0.15);">
        <p style="margin:0;font-size:14px;font-weight:400;color:#7c7c7c;">KFC-4829</p>
      </div>
      <div style="margin:0 0 12px 0;padding:16px;border-radius:54px;background:#232323;">
        <p style="margin:0;font-size:14px;font-weight:500;color:#777777;">Copy offer code</p>
      </div>
      <table role="presentation" width="100%" style="border-collapse:collapse;">
        <tr>
          <td align="left" style="font-size:12px;font-weight:500;line-height:17px;color:rgba(244,244,244,0.5);">Terms apply</td>
          <td align="right" style="font-size:12px;font-weight:500;line-height:17px;color:rgba(244,244,244,0.5);">Expires: 31 July 2026</td>
        </tr>
      </table>
    </div>
  `,
  disclaimer:
    "You're receiving this because you joined KFC customer club after visiting or giving feedback.",
  addressLine: "KFC, Camden High Street, London",
  unsubscribeUrl: "https://app.tummly.com/unsubscribe",
  termsUrl: "https://app.tummly.com/terms",
  privacyUrl: "https://app.tummly.com/privacy",
  cookieUrl: "https://app.tummly.com/cookie-policy",
  topDecorationUrl: "/static/top-decoration.png",
  poweredByLogoUrl: "/static/tummly-wordmark.png",
  bottomStripUrl: "/static/bottom-strip.png",
  preview: "Thank you for sharing your feedback with KFC.",
} satisfies GuestResponseEmailProps
