import {
  Body,
  Container,
  Head,
  Heading,
  Hr,
  Html,
  Link,
  Preview,
  Section,
  Text,
} from "react-email"

import { EmailFonts } from "./_components/EmailFonts"
import { EmailLogo } from "./_components/EmailLogo"
import { HelpFooter } from "./_components/HelpFooter"
import {
  bodyText,
  bodyTextLast,
  dividerStyle,
  emailBodyStyle,
  emailContainerStyle,
  headingStyle,
  linkStyle,
  mainSectionStyle,
} from "./_components/styles"
import { supportEmail } from "./_components/tokens"

export type PasswordChangedEmailProps = {
  firstName: string
  helpCentreUrl: string
  termsUrl: string
  privacyUrl: string
  cookiePolicyUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  helpCentreUrl: "{{help_centre_url}}",
  termsUrl: "{{terms_url}}",
  privacyUrl: "{{privacy_url}}",
  cookiePolicyUrl: "{{cookie_policy_url}}",
  logoUrl: "{{logo_url}}",
} satisfies PasswordChangedEmailProps

/**
 * Password changed — Figma Guest-Loop-MVP node 6845:13396.
 */
export default function PasswordChangedEmail({
  firstName = tokenDefaults.firstName,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  termsUrl = tokenDefaults.termsUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  cookiePolicyUrl = tokenDefaults.cookiePolicyUrl,
  logoUrl = tokenDefaults.logoUrl,
}: PasswordChangedEmailProps = tokenDefaults) {
  const headline = "Your Tummly password was changed"

  return (
    <Html lang="en">
      <Head>
        <EmailFonts />
      </Head>
      <Preview>No action is needed if you made this change.</Preview>
      <Body style={emailBodyStyle}>
        <Container style={emailContainerStyle}>
          <Section style={mainSectionStyle}>
            <EmailLogo logoUrl={logoUrl} />

            <Heading as="h1" style={headingStyle}>
              {headline}
            </Heading>

            <Hr style={dividerStyle} />

            <Text style={bodyText}>Hi {firstName},</Text>
            <Text style={bodyText}>Your Tummly password was changed.</Text>
            <Text style={bodyText}>
              If you made this change, no action is needed.
            </Text>
            <Text style={bodyTextLast}>
              If you did not change your password,{" "}
              <Link href={`mailto:${supportEmail}`} style={linkStyle}>
                contact Tummly support
              </Link>{" "}
              immediately.
            </Text>
          </Section>

          <HelpFooter
            helpCentreUrl={helpCentreUrl}
            showLegalRow
            termsUrl={termsUrl}
            privacyUrl={privacyUrl}
            cookiePolicyUrl={cookiePolicyUrl}
          />
        </Container>
      </Body>
    </Html>
  )
}

PasswordChangedEmail.PreviewProps = {
  firstName: "Alex",
  helpCentreUrl: "https://app.tummly.com/help-center",
  termsUrl: "https://app.tummly.com/terms",
  privacyUrl: "https://app.tummly.com/privacy",
  cookiePolicyUrl: "https://app.tummly.com/cookie-policy",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies PasswordChangedEmailProps
