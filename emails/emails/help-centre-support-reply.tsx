import { Link, Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyTextGap20, bodyTextLast, linkStyle } from "./_components/styles"
import { colors, fontFunctional } from "./_components/tokens"

export type HelpCentreSupportReplyEmailProps = {
  submitterName: string
  topicLabel: string
  replyBody: string
  /** Pre-built My queries section HTML, or empty. */
  myQueriesBlock: string
  helpCentreUrl: string
  logoUrl: string
}

const tokenDefaults = {
  submitterName: "{{submitter_name}}",
  topicLabel: "{{topic_label}}",
  replyBody: "{{reply_body}}",
  myQueriesBlock: "{{my_queries_block}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies HelpCentreSupportReplyEmailProps

const headline = "Reply from Tummly Support"

export default function HelpCentreSupportReplyEmail({
  submitterName = tokenDefaults.submitterName,
  topicLabel = tokenDefaults.topicLabel,
  replyBody = tokenDefaults.replyBody,
  myQueriesBlock = tokenDefaults.myQueriesBlock,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: HelpCentreSupportReplyEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview={headline}
      headline={headline}
      logoUrl={logoUrl}
      helpCentreUrl={helpCentreUrl}
    >
      <Text style={bodyTextGap20}>Hi {submitterName},</Text>
      <Text
        style={{
          margin: "0 0 8px",
          fontFamily: fontFunctional,
          fontSize: "14px",
          lineHeight: "20px",
          color: colors.gray550,
        }}
      >
        Re: {topicLabel}
      </Text>
      <div
        style={{
          margin: "16px 0",
          padding: "16px",
          background: "#F9FAFB",
          borderRadius: "8px",
          border: "1px solid #E5E7EB",
        }}
      >
        <Text
          style={{
            margin: 0,
            fontFamily: fontFunctional,
            fontSize: "16px",
            lineHeight: "24px",
            color: colors.black,
            whiteSpace: "pre-wrap",
          }}
        >
          {replyBody}
        </Text>
      </div>
      <div dangerouslySetInnerHTML={{ __html: myQueriesBlock }} />
      <Text style={bodyTextLast}>
        If you need more help, reply to this email or visit our{" "}
        <Link href={helpCentreUrl} style={linkStyle}>
          Help Centre
        </Link>
        .
      </Text>
    </TransactionalEmailShell>
  )
}

HelpCentreSupportReplyEmail.PreviewProps = {
  submitterName: "Alex",
  topicLabel: "Billing",
  replyBody: "Thanks for reaching out — we have updated your plan.",
  myQueriesBlock:
    "<p style=\"margin:24px 0 0 0;font-size:16px;line-height:24px;color:#141414;\">You can view this conversation and reply in <a href=\"https://app.tummly.com/help-center/queries\" style=\"color:#141414;text-decoration:underline;\">My queries</a>.</p>",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies HelpCentreSupportReplyEmailProps
