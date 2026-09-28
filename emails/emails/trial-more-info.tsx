import { Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyTextGap20, bodyTextLast } from "./_components/styles"

export type TrialMoreInfoEmailProps = {
  greetingLine: string
  /** Pre-built request / feedback HTML. */
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
} satisfies TrialMoreInfoEmailProps

const headline = "More information needed"

export default function TrialMoreInfoEmail({
  greetingLine = tokenDefaults.greetingLine,
  feedbackBlock = tokenDefaults.feedbackBlock,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  termsUrl = tokenDefaults.termsUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  cookiePolicyUrl = tokenDefaults.cookiePolicyUrl,
  logoUrl = tokenDefaults.logoUrl,
}: TrialMoreInfoEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview="Action required: Tummly trial request"
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
        Our team needs a few more details before activating your trial.
      </Text>
      <div dangerouslySetInnerHTML={{ __html: feedbackBlock }} />
      <Text style={bodyTextLast}>
        We will process your application as soon as you reply.
      </Text>
    </TransactionalEmailShell>
  )
}

TrialMoreInfoEmail.PreviewProps = {
  greetingLine: "Hi Alex,",
  feedbackBlock:
    "<p style=\"margin:0 0 14px;font-size:14px;font-weight:600;line-height:20px;color:#141414;\">What we need from you</p><div style=\"margin:0 0 14px;padding:16px;background-color:#f9f9fa;border-left:4px solid #141414;\"><p style=\"margin:0;font-size:14px;line-height:20px;color:#141414;\">Please share your restaurant address.</p></div>",
  helpCentreUrl: "https://app.tummly.com/help-center",
  termsUrl: "https://app.tummly.com/terms",
  privacyUrl: "https://app.tummly.com/privacy",
  cookiePolicyUrl: "https://app.tummly.com/cookie-policy",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies TrialMoreInfoEmailProps
