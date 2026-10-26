import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type PlanStartedEmailProps = {
  firstName: string
  planName: string
  restaurantName: string
  dashboardUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  planName: "{{plan_name}}",
  restaurantName: "{{restaurant_name}}",
  dashboardUrl: "{{dashboard_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies PlanStartedEmailProps

/**
 * Paid plan started (Starter / Growth / Group / Custom) — same layout as
 * pilot-started, Figma Guest-Loop-MVP node 6852:41801.
 */
export default function PlanStartedEmail({
  firstName = tokenDefaults.firstName,
  planName = tokenDefaults.planName,
  restaurantName = tokenDefaults.restaurantName,
  dashboardUrl = tokenDefaults.dashboardUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: PlanStartedEmailProps = tokenDefaults) {
  const headline = `Your Tummly ${planName} plan has started`

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
        {`Your Tummly ${planName} plan for ${restaurantName} is now active.`}
      </Text>
      <Text style={bodyTextGap20}>
        You can start using Tummly to collect private Feedback, build your Guest
        list, create Offers and explore the Guest Loop.
      </Text>
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

PlanStartedEmail.PreviewProps = {
  firstName: "Alex",
  planName: "Growth",
  restaurantName: "Mehmet's Grill",
  dashboardUrl: "https://app.tummly.com/single-dashboard",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies PlanStartedEmailProps
