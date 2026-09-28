import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyText,
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type ShopOrderConfirmedEmailProps = {
  firstName: string
  locationName: string
  orderNumber: string
  /** Pre-built HTML lines: `Title × Qty<br />…` */
  materialsLinesHtml: string
  /** Pre-built address HTML with `<br />` separators. */
  deliveryAddressHtml: string
  orderUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  locationName: "{{location_name}}",
  orderNumber: "{{order_number}}",
  materialsLinesHtml: "{{materials_lines_html}}",
  deliveryAddressHtml: "{{delivery_address_html}}",
  orderUrl: "{{order_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies ShopOrderConfirmedEmailProps

/**
 * Shop order confirmed — Figma Guest-Loop-MVP node 6852:41958.
 * Sent when payment reaches Paid (including complimentary starter).
 */
export default function ShopOrderConfirmedEmail({
  firstName = tokenDefaults.firstName,
  locationName = tokenDefaults.locationName,
  orderNumber = tokenDefaults.orderNumber,
  materialsLinesHtml = tokenDefaults.materialsLinesHtml,
  deliveryAddressHtml = tokenDefaults.deliveryAddressHtml,
  orderUrl = tokenDefaults.orderUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: ShopOrderConfirmedEmailProps = tokenDefaults) {
  return (
    <BrandEmailShell
      preview="Your Tummly order is confirmed"
      headline="Your Tummly order is confirmed"
      logoUrl={logoUrl}
      privacyUrl={privacyUrl}
      companyDetailsUrl={companyDetailsUrl}
    >
      <Text style={bodyTextGap20}>Hi {firstName},</Text>
      <Text style={bodyText}>
        We’ve received your order for {locationName}.
      </Text>
      <Text style={bodyText}>Order: {orderNumber}</Text>
      <div
        style={bodyText}
        dangerouslySetInnerHTML={{ __html: materialsLinesHtml }}
      />
      <Text style={bodyText}>Delivery address</Text>
      <div
        style={bodyTextGap20}
        dangerouslySetInnerHTML={{ __html: deliveryAddressHtml }}
      />
      <Text style={bodyTextGap20}>
        We’ll let you know when your order has been dispatched.
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={orderUrl} style={ctaButtonStyle}>
          View order
        </Button>
      </Section>
      <Text style={bodyTextLast}>
        Thanks,
        <br />
        The Tummly Team
      </Text>
    </BrandEmailShell>
  )
}

ShopOrderConfirmedEmail.PreviewProps = {
  firstName: "Alex",
  locationName: "Camden High Street",
  orderNumber: "ORD-1042",
  materialsLinesHtml: "Table Tent QR × 2<br />Window Sticker QR × 1",
  deliveryAddressHtml:
    "12 High Street<br />Camden<br />NW1 8AB<br />United Kingdom",
  orderUrl:
    "https://app.tummly.test/multi-dashboard/shop?location=12&view=orders&shopOrderId=00000000-0000-0000-0000-000000000001",
  privacyUrl: "https://app.tummly.test/privacy",
  companyDetailsUrl: "https://app.tummly.test/terms",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies ShopOrderConfirmedEmailProps
