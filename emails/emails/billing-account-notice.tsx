import { Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyTextGap20 } from "./_components/styles"

export type BillingAccountNoticeEmailProps = {
  greetingLine: string
  headline: string
  body: string
  /** Pre-built CTA HTML, or empty. */
  ctaBlock: string
  helpCentreUrl: string
  logoUrl: string
}

const tokenDefaults = {
  greetingLine: "{{greeting_line}}",
  headline: "{{headline}}",
  body: "{{body}}",
  ctaBlock: "{{cta_block}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies BillingAccountNoticeEmailProps

export default function BillingAccountNoticeEmail({
  greetingLine = tokenDefaults.greetingLine,
  headline = tokenDefaults.headline,
  body = tokenDefaults.body,
  ctaBlock = tokenDefaults.ctaBlock,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: BillingAccountNoticeEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview={headline}
      headline={headline}
      logoUrl={logoUrl}
      helpCentreUrl={helpCentreUrl}
    >
      <Text style={bodyTextGap20}>{greetingLine}</Text>
      <Text style={bodyTextGap20}>{body}</Text>
      <div dangerouslySetInnerHTML={{ __html: ctaBlock }} />
    </TransactionalEmailShell>
  )
}

BillingAccountNoticeEmail.PreviewProps = {
  greetingLine: "Hi Alex,",
  headline: "Action needed on your billing account",
  body: "Please update your payment method to keep your workspace active.",
  ctaBlock:
    "<p style=\"margin:24px 0 0;\"><a href=\"https://app.tummly.com/billing\" style=\"background-color:#14a74a;color:#ffffff;display:inline-block;padding:15px 17px;border-radius:4px;font-size:16px;font-weight:500;line-height:20px;text-decoration:none;\">Open billing</a></p>",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies BillingAccountNoticeEmailProps
