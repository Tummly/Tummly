import {
  Body,
  Button,
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
  bodyTextGap20,
  ctaButtonStyle,
  dividerStyle,
  headingStyle,
  linkStyle,
  mainSectionStyle,
  mutedNoteStyle,
} from "./_components/styles"
import { colors, fontFunctional, supportEmail } from "./_components/tokens"

export type ResetPasswordEmailProps = {
  resetUrl: string
  helpCentreUrl: string
  termsUrl: string
  privacyUrl: string
  cookiePolicyUrl: string
  logoUrl: string
}

const tokenDefaults = {
  resetUrl: "{{reset_url}}",
  helpCentreUrl: "{{help_centre_url}}",
  termsUrl: "{{terms_url}}",
  privacyUrl: "{{privacy_url}}",
  cookiePolicyUrl: "{{cookie_policy_url}}",
  logoUrl: "{{logo_url}}",
} satisfies ResetPasswordEmailProps

/**
 * Reset password — Figma Guest-Loop-MVP node 6845:13367.
 */
export default function ResetPasswordEmail({
  resetUrl = tokenDefaults.resetUrl,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  termsUrl = tokenDefaults.termsUrl,
  privacyUrl = tokenDefaults.privacyUrl,
  cookiePolicyUrl = tokenDefaults.cookiePolicyUrl,
  logoUrl = tokenDefaults.logoUrl,
}: ResetPasswordEmailProps = tokenDefaults) {
  const headline = "Reset your Tummly password"

  return (
    <Html lang="en">
      <Head>
        <EmailFonts />
      </Head>
      <Preview>Use this secure link to create a new password.</Preview>
      <Body
        style={{
          margin: 0,
          padding: 0,
          backgroundColor: colors.bodyBg,
          fontFamily: fontFunctional,
        }}
      >
        <Container
          style={{
            margin: "0 auto",
            maxWidth: "600px",
            width: "100%",
            backgroundColor: colors.white,
          }}
        >
          <Section style={mainSectionStyle}>
            <EmailLogo logoUrl={logoUrl} />

            <Heading as="h1" style={headingStyle}>
              {headline}
            </Heading>

            <Hr style={dividerStyle} />

            <Text style={bodyTextGap20}>
              We received a request to reset the password for your Tummly
              account.
            </Text>

            <Section style={{ margin: "0 0 20px" }}>
              <Button href={resetUrl} style={ctaButtonStyle}>
                Reset password
              </Button>
            </Section>

            <Text style={bodyTextGap20}>This link expires in 30 minutes.</Text>
            <Text style={bodyTextGap20}>
              If you did not request a password reset, ignore this email or{" "}
              <Link href={`mailto:${supportEmail}`} style={linkStyle}>
                contact Tummly support
              </Link>
              .
            </Text>
            <Text style={mutedNoteStyle}>Do not reply to this automated email.</Text>
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

ResetPasswordEmail.PreviewProps = {
  resetUrl: "https://app.tummly.com/reset-password?token=preview",
  helpCentreUrl: "https://app.tummly.com/help-center",
  termsUrl: "https://app.tummly.com/terms",
  privacyUrl: "https://app.tummly.com/privacy",
  cookiePolicyUrl: "https://app.tummly.com/cookie-policy",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies ResetPasswordEmailProps
