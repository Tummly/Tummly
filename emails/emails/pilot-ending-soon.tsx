import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type PilotEndingSoonEmailProps = {
  daysRemaining: string
  firstName: string
  restaurantName: string
  pilotEndDate: string
  plansUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  daysRemaining: "{{days_remaining}}",
  firstName: "{{first_name}}",
  restaurantName: "{{restaurant_name}}",
  pilotEndDate: "{{pilot_end_date}}",
  plansUrl: "{{plans_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies PilotEndingSoonEmailProps

/**
 * Pilot ending soon — Figma Guest-Loop-MVP node 6852:41855.
 */
export default function PilotEndingSoonEmail({
  daysRemaining = tokenDefaults.daysRemaining,
  firstName = tokenDefaults.firstName,
  restaurantName = tokenDefaults.restaurantName,
  pilotEndDate = tokenDefaults.pilotEndDate,
  plansUrl = tokenDefaults.plansUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: PilotEndingSoonEmailProps = tokenDefaults) {
  const headline = `Your Tummly Pilot ends in ${daysRemaining} days`

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
        Your Tummly Pilot for {restaurantName} ends on {pilotEndDate}.
      </Text>
      <Text style={bodyTextGap20}>
        To continue using Tummly after your Pilot, choose the plan that best
        fits your restaurant.
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={plansUrl} style={ctaButtonStyle}>
          View plans
        </Button>
      </Section>
      <Text style={bodyTextGap20}>
        Your existing data will remain available subject to your account and
        plan status.
      </Text>
      <Text style={bodyTextLast}>
        Thanks,
        <br />
        The Tummly Team
      </Text>
    </BrandEmailShell>
  )
}

PilotEndingSoonEmail.PreviewProps = {
  daysRemaining: "5",
  firstName: "Alex",
  restaurantName: "Mehmet's Grill",
  pilotEndDate: "28 Oct 2026",
  plansUrl: "https://app.tummly.com/single-dashboard/settings/billing-credits",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies PilotEndingSoonEmailProps
