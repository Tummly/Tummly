import { Link, Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyTextGap20, bodyTextLast, linkStyle } from "./_components/styles"
import { colors, fontFunctional } from "./_components/tokens"

export type HelpCentreResolvedEmailProps = {
  submitterName: string
  topicLabel: string
  /** Pre-built excerpt messages HTML, or empty. */
  excerptBlock: string
  /** Pre-built My queries section HTML, or empty. */
  myQueriesBlock: string
  helpCentreUrl: string
  logoUrl: string
}

const tokenDefaults = {
  submitterName: "{{submitter_name}}",
  topicLabel: "{{topic_label}}",
  excerptBlock: "{{excerpt_block}}",
  myQueriesBlock: "{{my_queries_block}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies HelpCentreResolvedEmailProps

const headline = "Your query has been resolved"

export default function HelpCentreResolvedEmail({
  submitterName = tokenDefaults.submitterName,
  topicLabel = tokenDefaults.topicLabel,
  excerptBlock = tokenDefaults.excerptBlock,
  myQueriesBlock = tokenDefaults.myQueriesBlock,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: HelpCentreResolvedEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview={headline}
      headline={headline}
      logoUrl={logoUrl}
      helpCentreUrl={helpCentreUrl}
    >
      <Text style={bodyTextGap20}>Hi {submitterName},</Text>
      <Text style={bodyTextGap20}>
        We&apos;ve marked your query as resolved.
      </Text>
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
      <div dangerouslySetInnerHTML={{ __html: excerptBlock }} />
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

HelpCentreResolvedEmail.PreviewProps = {
  submitterName: "Alex",
  topicLabel: "Billing",
  excerptBlock: "",
  myQueriesBlock: "",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies HelpCentreResolvedEmailProps
