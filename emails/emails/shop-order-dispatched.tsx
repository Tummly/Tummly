import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type ShopOrderDispatchedEmailProps = {
  firstName: string
  locationName: string
  orderNumber: string
  deliveryEstimate: string
  trackingDetails: string
  orderUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  locationName: "{{location_name}}",
  orderNumber: "{{order_number}}",
  deliveryEstimate: "{{delivery_estimate}}",
  trackingDetails: "{{tracking_details}}",
  orderUrl: "{{order_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies ShopOrderDispatchedEmailProps

/**
 * Shop order dispatched — Figma Guest-Loop-MVP node 6852:42010.
 * Sent when fulfilment moves Processing → InTransit.
 */
export default function ShopOrderDispatchedEmail({
  firstName = tokenDefaults.firstName,
  locationName = tokenDefaults.locationName,
  orderNumber = tokenDefaults.orderNumber,
  deliveryEstimate = tokenDefaults.deliveryEstimate,
  trackingDetails = tokenDefaults.trackingDetails,
  orderUrl = tokenDefaults.orderUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: ShopOrderDispatchedEmailProps = tokenDefaults) {
  return (
    <BrandEmailShell
      preview="Your Tummly order is on its way"
      headline="Your Tummly order is on its way"
      logoUrl={logoUrl}
      privacyUrl={privacyUrl}
      companyDetailsUrl={companyDetailsUrl}
    >
      <Text style={bodyTextGap20}>Hi {firstName},</Text>
      <Text style={bodyTextGap20}>
        Your Tummly order for {locationName} has been dispatched.
        <br />
        <br />
        Order: {orderNumber}
        <br />
        <br />
        Estimated delivery: {deliveryEstimate}
        <br />
        <br />
        Tracking: {trackingDetails}
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={orderUrl} style={ctaButtonStyle}>
          Track / View order
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

ShopOrderDispatchedEmail.PreviewProps = {
  firstName: "Alex",
  locationName: "Camden High Street",
  orderNumber: "ORD-1042",
  deliveryEstimate: "Typically 5–7 working days",
  trackingDetails: "Available in your order",
  orderUrl:
    "https://app.tummly.test/multi-dashboard/shop?location=12&view=orders&shopOrderId=00000000-0000-0000-0000-000000000001",
  privacyUrl: "https://app.tummly.test/privacy",
  companyDetailsUrl: "https://app.tummly.test/terms",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies ShopOrderDispatchedEmailProps
