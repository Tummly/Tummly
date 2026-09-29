import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type PaymentActionRequiredEmailProps = {
  firstName: string
  orderDescription: string
  billingUrl: string
  ctaLabel: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  orderDescription: "{{order_description}}",
  billingUrl: "{{billing_url}}",
  ctaLabel: "{{cta_label}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies PaymentActionRequiredEmailProps

/**
 * Payment pending / failure — Figma Guest-Loop-MVP node 6852:42160.
 */
export default function PaymentActionRequiredEmail({
  firstName = tokenDefaults.firstName,
  orderDescription = tokenDefaults.orderDescription,
  billingUrl = tokenDefaults.billingUrl,
  ctaLabel = tokenDefaults.ctaLabel,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: PaymentActionRequiredEmailProps = tokenDefaults) {
  const headline = "Action required for your Tummly payment"

  return (
    <BrandEmailShell
      preview={headline}
      headline={headline}
      logoUrl={logoUrl}
      privacyUrl={privacyUrl}
      companyDetailsUrl={companyDetailsUrl}
    >
      <Text style={bodyTextGap20}>Hi {firstName},</Text>
      <Text style={bodyTextGap20}>
        We couldn&apos;t confirm your latest payment for {orderDescription}.
      </Text>
      <Text style={bodyTextGap20}>
        No successful payment has been recorded yet.
      </Text>
      <Text style={bodyTextGap20}>
        Please review your billing details or try again from Tummly.
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={billingUrl} style={ctaButtonStyle}>
          {ctaLabel}
        </Button>
      </Section>
      <Text style={bodyTextGap20}>
        If you&apos;ve already completed the payment, the status may still be
        updating.
      </Text>
      <Text style={bodyTextLast}>
        Thanks,
        <br />
        The Tummly Team
      </Text>
    </BrandEmailShell>
  )
}

PaymentActionRequiredEmail.PreviewProps = {
  firstName: "Alex",
  orderDescription: "Growth — Monthly",
  billingUrl: "https://app.tummly.com/single-dashboard/settings/billing-credits",
  ctaLabel: "Review billing",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies PaymentActionRequiredEmailProps
