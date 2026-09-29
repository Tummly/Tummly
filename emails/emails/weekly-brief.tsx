import { Button, Section, Text } from "react-email"

import { BrandEmailShell } from "./_components/BrandEmailShell"
import {
  bodyTextGap20,
  bodyTextLast,
  ctaButtonStyle,
  dividerStyle,
} from "./_components/styles"
import { colors, fontFunctional, fontHeadline } from "./_components/tokens"

export type WeeklyBriefEmailProps = {
  firstName: string
  locationName: string
  periodLabel: string
  qrScans: string
  feedbackReceived: string
  guestsCaptured: string
  offerClaimed: string
  redemptions: string
  campaignEngagement: string
  whatChanged: string
  recommendedNextStep: string
  weeklyBriefUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  locationName: "{{location_name}}",
  periodLabel: "{{period_label}}",
  qrScans: "{{qr_scans}}",
  feedbackReceived: "{{feedback_received}}",
  guestsCaptured: "{{guests_captured}}",
  offerClaimed: "{{offer_claimed}}",
  redemptions: "{{redemptions}}",
  campaignEngagement: "{{campaign_engagement}}",
  whatChanged: "{{what_changed}}",
  recommendedNextStep: "{{recommended_next_step}}",
  weeklyBriefUrl: "{{weekly_brief_url}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies WeeklyBriefEmailProps

const metricTileStyle = {
  backgroundColor: "#e9e9e9",
  padding: "16px 18px",
  textAlign: "center" as const,
  verticalAlign: "middle" as const,
}

const metricValueStyle = {
  margin: "0 0 8px",
  fontFamily: fontFunctional,
  fontSize: "16px",
  fontWeight: 400,
  lineHeight: "normal",
  color: colors.black,
  textAlign: "center" as const,
}

const metricLabelStyle = {
  margin: 0,
  fontFamily: fontHeadline,
  fontSize: "10px",
  fontWeight: 700,
  lineHeight: "normal",
  color: colors.black,
  textAlign: "center" as const,
}

const sectionHeadingStyle = {
  margin: "0 0 8px",
  fontFamily: fontFunctional,
  fontSize: "14px",
  fontWeight: 600,
  lineHeight: "20px",
  color: colors.black,
}

function MetricTile({ value, label }: { value: string; label: string }) {
  return (
    <td style={{ ...metricTileStyle, width: "33.33%" }}>
      <Text style={metricValueStyle}>{value}</Text>
      <Text style={metricLabelStyle}>{label}</Text>
    </td>
  )
}

/**
 * Weekly Brief ready — Figma Guest-Loop-MVP node 6852:50384.
 * Sent after AI generate on the Monday / first-write notify seam.
 */
export default function WeeklyBriefEmail({
  firstName = tokenDefaults.firstName,
  locationName = tokenDefaults.locationName,
  periodLabel = tokenDefaults.periodLabel,
  qrScans = tokenDefaults.qrScans,
  feedbackReceived = tokenDefaults.feedbackReceived,
  guestsCaptured = tokenDefaults.guestsCaptured,
  offerClaimed = tokenDefaults.offerClaimed,
  redemptions = tokenDefaults.redemptions,
  campaignEngagement = tokenDefaults.campaignEngagement,
  whatChanged = tokenDefaults.whatChanged,
  recommendedNextStep = tokenDefaults.recommendedNextStep,
  weeklyBriefUrl = tokenDefaults.weeklyBriefUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: WeeklyBriefEmailProps = tokenDefaults) {
  const headline = "Your Tummly Weekly Brief"

  return (
    <BrandEmailShell
      preview={`${headline} — ${locationName}`}
      headline={headline}
      logoUrl={logoUrl}
      privacyUrl={privacyUrl}
      companyDetailsUrl={companyDetailsUrl}
    >
      <Text style={bodyTextGap20}>Hi {firstName},</Text>
      <Text style={bodyTextGap20}>
        Here&apos;s your Weekly Brief for {locationName}, covering{" "}
        {periodLabel}.
      </Text>
      <Text style={bodyTextGap20}>This week at a glance</Text>

      <Section style={{ margin: "0 0 20px" }}>
        <table
          role="presentation"
          cellPadding={0}
          cellSpacing={0}
          border={0}
          width="100%"
          style={{ borderCollapse: "separate", borderSpacing: "10px 10px" }}
        >
          <tbody>
            <tr>
              <MetricTile value={qrScans} label="QR scans" />
              <MetricTile value={feedbackReceived} label="Feedback received" />
              <MetricTile value={guestsCaptured} label="Guests captured" />
            </tr>
            <tr>
              <MetricTile value={offerClaimed} label="Offer claimed" />
              <MetricTile value={redemptions} label="Redemptions" />
              <MetricTile
                value={campaignEngagement}
                label="Campaign engagement"
              />
            </tr>
          </tbody>
        </table>
      </Section>

      <Section style={{ margin: "0 0 20px" }}>
        <Button href={weeklyBriefUrl} style={ctaButtonStyle}>
          View full Weekly Brief
        </Button>
      </Section>

      <hr style={{ ...dividerStyle, margin: "0 0 20px" }} />

      <Text style={sectionHeadingStyle}>What changed</Text>
      <Text style={bodyTextGap20}>{whatChanged}</Text>

      <hr style={{ ...dividerStyle, margin: "0 0 20px" }} />

      <Text style={sectionHeadingStyle}>Recommended next step</Text>
      <Text style={bodyTextGap20}>{recommendedNextStep}</Text>

      <Text style={bodyTextLast}>
        Thanks,
        <br />
        The Tummly Team
      </Text>
    </BrandEmailShell>
  )
}

WeeklyBriefEmail.PreviewProps = {
  firstName: "Alex",
  locationName: "Harbour Kitchen",
  periodLabel: "6–12 July",
  qrScans: "142",
  feedbackReceived: "38",
  guestsCaptured: "27",
  offerClaimed: "19",
  redemptions: "11",
  campaignEngagement: "64",
  whatChanged:
    "QR scans and feedback both rose versus last week. Guests are engaging more with table prompts.",
  recommendedNextStep:
    "Follow up with guests who left contact details after negative feedback.",
  weeklyBriefUrl:
    "https://app.tummly.com/single-dashboard/reports/weekly-brief?location=1",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies WeeklyBriefEmailProps
