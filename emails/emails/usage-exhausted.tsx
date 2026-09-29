import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type UsageExhaustedEmailProps = {
  allowanceKind: string
  firstName: string
  ctaUrl: string
  ctaLabel: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  allowanceKind: "{{allowance_kind}}",
  firstName: "{{first_name}}",
  ctaUrl: "{{cta_url}}",
  ctaLabel: "{{cta_label}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies UsageExhaustedEmailProps

/**
 * Usage exhausted — Figma Guest-Loop-MVP node 6852:42260.
 * Heading follows product subject (Figma frame reused the warning title).
 */
export default function UsageExhaustedEmail({
  allowanceKind = tokenDefaults.allowanceKind,
  firstName = tokenDefaults.firstName,
  ctaUrl = tokenDefaults.ctaUrl,
  ctaLabel = tokenDefaults.ctaLabel,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: UsageExhaustedEmailProps = tokenDefaults) {
  const headline = `Your ${allowanceKind} allowance has been used`

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
        You&apos;ve used your available {allowanceKind} allowance for the
        current period.
      </Text>
      <Text style={bodyTextGap20}>
        Only the affected feature is restricted; your other Tummly features
        remain available.
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={ctaUrl} style={ctaButtonStyle}>
          {ctaLabel}
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

UsageExhaustedEmail.PreviewProps = {
  allowanceKind: "Email",
  firstName: "Alex",
  ctaUrl: "https://app.tummly.com/single-dashboard/settings/billing-credits",
  ctaLabel: "Add credits",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies UsageExhaustedEmailProps
