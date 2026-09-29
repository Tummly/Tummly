import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type PilotEndedEmailProps = {
  firstName: string
  restaurantName: string
  plansUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  restaurantName: "{{restaurant_name}}",
  plansUrl: "{{plans_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies PilotEndedEmailProps

/**
 * Pilot ended — Figma Guest-Loop-MVP node 6852:41906.
 */
export default function PilotEndedEmail({
  firstName = tokenDefaults.firstName,
  restaurantName = tokenDefaults.restaurantName,
  plansUrl = tokenDefaults.plansUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: PilotEndedEmailProps = tokenDefaults) {
  const headline = "Your Tummly Pilot has ended"

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
        Your 30-day Tummly Pilot for {restaurantName} has ended.
      </Text>
      <Text style={bodyTextGap20}>
        You can still sign in and review your account, but actions that require
        an active plan may now be unavailable.
      </Text>
      <Text style={bodyTextGap20}>
        Choose a plan to continue using the full Guest Loop.
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={plansUrl} style={ctaButtonStyle}>
          Choose a plan
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

PilotEndedEmail.PreviewProps = {
  firstName: "Alex",
  restaurantName: "Mehmet's Grill",
  plansUrl: "https://app.tummly.com/single-dashboard/settings/billing-credits",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies PilotEndedEmailProps
