import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
} from "./_components/styles"

export type UsageWarningEmailProps = {
  allowanceKind: string
  firstName: string
  percentUsed: string
  usedAmount: string
  remainingAmount: string
  resetDate: string
  usageUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  allowanceKind: "{{allowance_kind}}",
  firstName: "{{first_name}}",
  percentUsed: "{{percent_used}}",
  usedAmount: "{{used_amount}}",
  remainingAmount: "{{remaining_amount}}",
  resetDate: "{{reset_date}}",
  usageUrl: "{{usage_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies UsageWarningEmailProps

/**
 * Usage warning — Figma Guest-Loop-MVP node 6852:42210.
 */
export default function UsageWarningEmail({
  allowanceKind = tokenDefaults.allowanceKind,
  firstName = tokenDefaults.firstName,
  percentUsed = tokenDefaults.percentUsed,
  usedAmount = tokenDefaults.usedAmount,
  remainingAmount = tokenDefaults.remainingAmount,
  resetDate = tokenDefaults.resetDate,
  usageUrl = tokenDefaults.usageUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: UsageWarningEmailProps = tokenDefaults) {
  const headline = `You're nearing your ${allowanceKind} allowance`

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
        You&apos;ve used {percentUsed} of your current {allowanceKind}{" "}
        allowance.
      </Text>
      <Text style={bodyTextGap20}>
        Used: {usedAmount}
        <br />
        Remaining: {remainingAmount}
        <br />
        Resets: {resetDate}
      </Text>
      <Section style={{ margin: "0 0 20px" }}>
        <Button href={usageUrl} style={ctaButtonStyle}>
          View usage
        </Button>
      </Section>
      <Text style={bodyTextGap20}>
        You can continue using your remaining allowance as normal.
      </Text>
      <Text style={bodyTextLast}>
        Thanks,
        <br />
        The Tummly Team
      </Text>
    </BrandEmailShell>
  )
}

UsageWarningEmail.PreviewProps = {
  allowanceKind: "Email",
  firstName: "Alex",
  percentUsed: "80%",
  usedAmount: "800",
  remainingAmount: "200",
  resetDate: "1 Oct 2026",
  usageUrl: "https://app.tummly.com/single-dashboard/settings/billing-credits",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies UsageWarningEmailProps
