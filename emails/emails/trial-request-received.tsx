import { Text } from "react-email"

import { TransactionalEmailShell } from "./_components/TransactionalEmailShell"
import { bodyText, bodyTextGap20, bodyTextLast } from "./_components/styles"

export type TrialRequestReceivedEmailProps = {
  greetingLine: string
  introParagraph: string
  helpCentreUrl: string
  termsUrl: string
  privacyUrl: string
  cookiePolicyUrl: string
  logoUrl: string
}

const tokenDefaults = {
  greetingLine: "{{greeting_line}}",
  introParagraph: "{{intro_paragraph}}",
  helpCentreUrl: "{{help_centre_url}}",
  termsUrl: "{{terms_url}}",
  privacyUrl: "{{privacy_url}}",
  cookiePolicyUrl: "{{cookie_policy_url}}",
  logoUrl: "{{logo_url}}",
} satisfies TrialRequestReceivedEmailProps

const headline = "We've received your Tummly trial request"

export default function TrialRequestReceivedEmail({
  greetingLine = tokenDefaults.greetingLine,
  introParagraph = tokenDefaults.introParagraph,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  termsUrl = tokenDefaults.termsUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  cookiePolicyUrl = tokenDefaults.cookiePolicyUrl,
  logoUrl = tokenDefaults.logoUrl,
}: TrialRequestReceivedEmailProps = tokenDefaults) {
  return (
    <TransactionalEmailShell
      preview={headline}
      headline={headline}
      logoUrl={logoUrl}
      helpCentreUrl={helpCentreUrl}
      showLegalRow
      termsUrl={termsUrl}
      privacyUrl={privacyUrl}
      cookiePolicyUrl={cookiePolicyUrl}
    >
      <Text style={bodyTextGap20}>{greetingLine}</Text>
      <Text style={bodyTextGap20}>{introParagraph}</Text>
      <Text style={bodyTextGap20}>
        We&apos;ve received your details and will review your restaurant
        information before sending the next setup step.
      </Text>
      <Text style={bodyTextGap20}>
        We usually review requests within 1–2 working days.
      </Text>
      <Text style={bodyTextGap20}>
        If your request is approved, we&apos;ll send you a secure setup link so
        you can create your Tummly workspace and prepare your first guest
        feedback prompts.
      </Text>
      <Text style={{ ...bodyText, fontWeight: 600, margin: "0 0 14px" }}>
        What happens next
      </Text>
      <Text style={bodyTextGap20}>
        1. We review your restaurant and contact details.
        <br />
        2. If anything is missing, we may contact you for clarification.
        <br />
        3. If approved, we send your secure setup link.
        <br />
        4. You complete setup for your restaurant or locations.
        <br />
        5. We help prepare your QR guest prompts and onboarding materials.
      </Text>
      <Text style={bodyTextLast}>
        Thanks,
        <br />
        The Tummly team
      </Text>
    </TransactionalEmailShell>
  )
}

TrialRequestReceivedEmail.PreviewProps = {
  greetingLine: "Hi Alex,",
  introParagraph:
    "Thanks for requesting a guided Tummly trial for Burger House.",
  helpCentreUrl: "https://app.tummly.com/help-center",
  termsUrl: "https://app.tummly.com/terms",
  privacyUrl: "https://app.tummly.com/privacy",
  cookiePolicyUrl: "https://app.tummly.com/cookie-policy",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies TrialRequestReceivedEmailProps
