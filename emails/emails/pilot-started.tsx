import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type PilotStartedEmailProps = {
  firstName: string
  restaurantName: string
  pilotEndDate: string
  dashboardUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  restaurantName: "{{restaurant_name}}",
  pilotEndDate: "{{pilot_end_date}}",
  dashboardUrl: "{{dashboard_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies PilotStartedEmailProps

/**
 * Pilot started — Figma Guest-Loop-MVP node 6852:41801.
 */
export default function PilotStartedEmail({
  firstName = tokenDefaults.firstName,
  restaurantName = tokenDefaults.restaurantName,
  pilotEndDate = tokenDefaults.pilotEndDate,
  dashboardUrl = tokenDefaults.dashboardUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: PilotStartedEmailProps = tokenDefaults) {
  const headline = "Your 30-day Tummly Pilot has started"

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
        Your 30-day Tummly Pilot for {restaurantName} is now active.
      </Text>
      <Text style={bodyTextGap20}>
        You can start using Tummly to collect private Feedback, build your Guest
        list, create Offers and explore the Guest Loop.
      </Text>
      <Text style={bodyTextGap20}>Your Pilot ends on {pilotEndDate}.</Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={dashboardUrl} style={ctaButtonStyle}>
          Go to Tummly
        </Button>
      </Section>
      <Text style={bodyTextGap20}>
        To add information, reply to this email and keep the reference in the
        subject.
      </Text>
      <Text style={bodyTextLast}>
        Thanks,
        <br />
        The Tummly Team
      </Text>
    </BrandEmailShell>
  )
}

PilotStartedEmail.PreviewProps = {
  firstName: "Alex",
  restaurantName: "Mehmet's Grill",
  pilotEndDate: "28 Oct 2026",
  dashboardUrl: "https://app.tummly.com/single-dashboard",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies PilotStartedEmailProps
