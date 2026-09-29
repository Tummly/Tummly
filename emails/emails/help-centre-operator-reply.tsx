import { Link, Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyTextGap20, bodyTextLast, linkStyle } from "./_components/styles"
import { colors, fontFunctional } from "./_components/tokens"

export type HelpCentreOperatorReplyEmailProps = {
  topicLabel: string
  submitterName: string
  submitterEmail: string
  businessName: string
  replyBody: string
  supportDashboardUrl: string
  helpCentreUrl: string
  logoUrl: string
}

const tokenDefaults = {
  topicLabel: "{{topic_label}}",
  submitterName: "{{submitter_name}}",
  submitterEmail: "{{submitter_email}}",
  businessName: "{{business_name}}",
  replyBody: "{{reply_body}}",
  supportDashboardUrl: "{{support_dashboard_url}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies HelpCentreOperatorReplyEmailProps

const headline = "Operator replied to a Help Centre query"
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

export default function HelpCentreOperatorReplyEmail({
  topicLabel = tokenDefaults.topicLabel,
  submitterName = tokenDefaults.submitterName,
  submitterEmail = tokenDefaults.submitterEmail,
  businessName = tokenDefaults.businessName,
  replyBody = tokenDefaults.replyBody,
  supportDashboardUrl = tokenDefaults.supportDashboardUrl,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: HelpCentreOperatorReplyEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview="Operator replied to Help Centre query"
      headline={headline}
      logoUrl={logoUrl}
      helpCentreUrl={helpCentreUrl}
    >
      <Text style={bodyTextGap20}>
        An operator has posted a follow-up on a query you are handling.
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
      <Text
        style={{
          margin: "16px 0 8px",
          fontFamily: fontFunctional,
          fontSize: "14px",
          fontWeight: 600,
          color: colors.black,
        }}
      >
        Operator reply
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
        {replyBody}
      </Text>
      <Text style={{ ...bodyTextLast, marginTop: "24px" }}>
        Open the{" "}
        <Link href={supportDashboardUrl} style={linkStyle}>
          Support dashboard
        </Link>{" "}
        to respond.
      </Text>
    </TransactionalEmailShell>
  )
}

HelpCentreOperatorReplyEmail.PreviewProps = {
  topicLabel: "Billing",
  submitterName: "Alex",
  submitterEmail: "alex@example.com",
  businessName: "Burger House",
  replyBody: "Please check the attached invoice.",
  supportDashboardUrl: "https://app.tummly.com/support",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies HelpCentreOperatorReplyEmailProps
