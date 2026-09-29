import { Link, Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyTextGap20, bodyTextLast, linkStyle } from "./_components/styles"
import { colors, fontFunctional } from "./_components/tokens"

export type HelpCentreEscalationEmailProps = {
  topicLabel: string
  submitterName: string
  submitterEmail: string
  businessName: string
  locationRow: string
  threadSummary: string
  noteBlock: string
  supportDashboardUrl: string
  helpCentreUrl: string
  logoUrl: string
}

const tokenDefaults = {
  topicLabel: "{{topic_label}}",
  submitterName: "{{submitter_name}}",
  submitterEmail: "{{submitter_email}}",
  businessName: "{{business_name}}",
  locationRow: "{{location_row}}",
  threadSummary: "{{thread_summary}}",
  noteBlock: "{{note_block}}",
  supportDashboardUrl: "{{support_dashboard_url}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies HelpCentreEscalationEmailProps

const headline = "Help Centre query escalated"
const cellLabel = {
  padding: "8px 0",
  fontSize: "14px",
  color: colors.gray550,
  fontFamily: fontFunctional,
  width: "140px",
} as const
const cellValue = {
  padding: "8px 0",
  fontSize: "14px",
  color: colors.black,
  fontFamily: fontFunctional,
} as const

export default function HelpCentreEscalationEmail({
  topicLabel = tokenDefaults.topicLabel,
  submitterName = tokenDefaults.submitterName,
  submitterEmail = tokenDefaults.submitterEmail,
  businessName = tokenDefaults.businessName,
  locationRow = tokenDefaults.locationRow,
  threadSummary = tokenDefaults.threadSummary,
  noteBlock = tokenDefaults.noteBlock,
  supportDashboardUrl = tokenDefaults.supportDashboardUrl,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: HelpCentreEscalationEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview={headline}
      headline={headline}
      logoUrl={logoUrl}
      helpCentreUrl={helpCentreUrl}
    >
      <Text style={bodyTextGap20}>
        Support has escalated a Help Centre query that needs Admin attention.
      </Text>
      <table
        style={{
          width: "100%",
          borderCollapse: "collapse",
          margin: "16px 0",
        }}
      >
        <tbody>
          <tr>
            <td style={cellLabel}>Topic</td>
            <td style={cellValue}>{topicLabel}</td>
          </tr>
          <tr>
            <td style={cellLabel}>Submitter</td>
            <td style={cellValue}>{submitterName}</td>
          </tr>
          <tr>
            <td style={cellLabel}>Email</td>
            <td style={cellValue}>{submitterEmail}</td>
          </tr>
          <tr>
            <td style={cellLabel}>Business</td>
            <td style={cellValue}>{businessName}</td>
          </tr>
        </tbody>
      </table>
      <div dangerouslySetInnerHTML={{ __html: locationRow }} />
      <Text
        style={{
          margin: "16px 0 8px",
          fontFamily: fontFunctional,
          fontSize: "14px",
          fontWeight: 600,
          color: colors.black,
        }}
      >
        Thread summary
      </Text>
      <Text
        style={{
          margin: 0,
          fontFamily: fontFunctional,
          fontSize: "14px",
          lineHeight: "22px",
          color: colors.black,
          whiteSpace: "pre-wrap",
        }}
      >
        {threadSummary}
      </Text>
      <div dangerouslySetInnerHTML={{ __html: noteBlock }} />
      <Text style={{ ...bodyTextLast, marginTop: "24px" }}>
        Review escalated queries in the{" "}
        <Link href={supportDashboardUrl} style={linkStyle}>
          Support dashboard
        </Link>
        .
      </Text>
    </TransactionalEmailShell>
  )
}

HelpCentreEscalationEmail.PreviewProps = {
  topicLabel: "Billing",
  submitterName: "Alex",
  submitterEmail: "alex@example.com",
  businessName: "Burger House",
  locationRow: "",
  threadSummary: "Guest asked about invoice timing.",
  noteBlock: "",
  supportDashboardUrl: "https://app.tummly.com/support",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies HelpCentreEscalationEmailProps
