import { Link, Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyTextGap20, bodyTextLast, linkStyle } from "./_components/styles"
import { supportEmail } from "./_components/tokens"

export type TrialDeclineEmailProps = {
  greetingLine: string
  /** Pre-built feedback HTML, or empty. */
  feedbackBlock: string
  helpCentreUrl: string
  termsUrl: string
  privacyUrl: string
  cookiePolicyUrl: string
  logoUrl: string
}

const tokenDefaults = {
  greetingLine: "{{greeting_line}}",
  feedbackBlock: "{{feedback_block}}",
  helpCentreUrl: "{{help_centre_url}}",
  termsUrl: "{{terms_url}}",
  privacyUrl: "{{privacy_url}}",
  cookiePolicyUrl: "{{cookie_policy_url}}",
  logoUrl: "{{logo_url}}",
} satisfies TrialDeclineEmailProps

const headline = "Trial request update"

export default function TrialDeclineEmail({
  greetingLine = tokenDefaults.greetingLine,
  feedbackBlock = tokenDefaults.feedbackBlock,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  termsUrl = tokenDefaults.termsUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  cookiePolicyUrl = tokenDefaults.cookiePolicyUrl,
  logoUrl = tokenDefaults.logoUrl,
}: TrialDeclineEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview="Update on your Tummly trial request"
      headline={headline}
      logoUrl={logoUrl}
      helpCentreUrl={helpCentreUrl}
      showLegalRow
      termsUrl={termsUrl}
      privacyUrl={privacyUrl}
      cookiePolicyUrl={cookiePolicyUrl}
    >
      <Text style={bodyTextGap20}>{greetingLine}</Text>
      <Text style={bodyTextGap20}>
        Thank you for your interest in Tummly. Unfortunately, your trial request
        cannot be approved at this time.
      </Text>
      <div dangerouslySetInnerHTML={{ __html: feedbackBlock }} />
      <Text style={bodyTextLast}>
        If you have questions,{" "}
        <Link href={`mailto:${supportEmail}`} style={linkStyle}>
          contact support
        </Link>
        .
      </Text>
    </TransactionalEmailShell>
  )
}

TrialDeclineEmail.PreviewProps = {
  greetingLine: "Hi Alex,",
  feedbackBlock:
    "<p style=\"margin:0 0 14px;font-size:14px;font-weight:600;line-height:20px;color:#141414;\">Reason</p><div style=\"margin:0 0 14px;padding:16px;background-color:#f9f9fa;border-left:4px solid #141414;\"><p style=\"margin:0;font-size:14px;line-height:20px;color:#141414;\">We need a stronger fit for this pilot cohort.</p></div>",
  helpCentreUrl: "https://app.tummly.com/help-center",
  termsUrl: "https://app.tummly.com/terms",
  privacyUrl: "https://app.tummly.com/privacy",
  cookiePolicyUrl: "https://app.tummly.com/cookie-policy",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies TrialDeclineEmailProps
