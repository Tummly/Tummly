import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type PaymentConfirmedEmailProps = {
  firstName: string
  orderDescription: string
  amount: string
  paymentDate: string
  reference: string
  billingUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  orderDescription: "{{order_description}}",
  amount: "{{amount}}",
  paymentDate: "{{payment_date}}",
  reference: "{{reference}}",
  billingUrl: "{{billing_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies PaymentConfirmedEmailProps

/**
 * Payment confirmed — Figma Guest-Loop-MVP node 6852:42109.
 * Invoice PDF is attached by EmailService separately.
 */
export default function PaymentConfirmedEmail({
  firstName = tokenDefaults.firstName,
  orderDescription = tokenDefaults.orderDescription,
  amount = tokenDefaults.amount,
  paymentDate = tokenDefaults.paymentDate,
  reference = tokenDefaults.reference,
  billingUrl = tokenDefaults.billingUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: PaymentConfirmedEmailProps = tokenDefaults) {
  const headline = "Your payment has been confirmed."

  return (
    <BrandEmailShell
      preview="Payment confirmed — Tummly"
      headline={headline}
      logoUrl={logoUrl}
      privacyUrl={privacyUrl}
      companyDetailsUrl={companyDetailsUrl}
    >
      <Text style={bodyTextGap20}>Hi {firstName},</Text>
      <Text style={bodyTextGap20}>Your payment has been confirmed.</Text>
      <Text style={bodyTextGap20}>
        Plan / Order: {orderDescription}
        <br />
        Amount: {amount}
        <br />
        Date: {paymentDate}
        <br />
        Reference: {reference}
      </Text>
      <Text style={bodyTextGap20}>
        Your account has been updated based on the confirmed payment status.
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={billingUrl} style={ctaButtonStyle}>
          View billing
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

PaymentConfirmedEmail.PreviewProps = {
  firstName: "Alex",
  orderDescription: "Growth — Monthly",
  amount: "£79.00",
  paymentDate: "28 Sep 2026",
  reference: "TM-000142",
  billingUrl: "https://app.tummly.com/single-dashboard/settings/billing-credits",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies PaymentConfirmedEmailProps
