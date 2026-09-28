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
  headingStyle,
  linkStyle,
  mainSectionStyle,
} from "./_components/styles"
import { colors, fontFunctional, supportEmail } from "./_components/tokens"

export type NewDeviceSignInEmailProps = {
  firstName: string
  signInTime: string
  deviceSummary: string
  locationSummary: string
  resetPasswordUrl: string
  helpCentreUrl: string
  logoUrl: string
}

const tokenDefaults = {
  firstName: "{{first_name}}",
  signInTime: "{{sign_in_time}}",
  deviceSummary: "{{device_summary}}",
  locationSummary: "{{location_summary}}",
  resetPasswordUrl: "{{reset_password_url}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies NewDeviceSignInEmailProps

/**
 * New device sign-in — Figma Guest-Loop-MVP node 6845:13419.
 */
export default function NewDeviceSignInEmail({
  firstName = tokenDefaults.firstName,
  signInTime = tokenDefaults.signInTime,
  deviceSummary = tokenDefaults.deviceSummary,
  locationSummary = tokenDefaults.locationSummary,
  resetPasswordUrl = tokenDefaults.resetPasswordUrl,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: NewDeviceSignInEmailProps = tokenDefaults) {
  const headline = "New sign-in to your Tummly account"

  return (
    <Html lang="en">
      <Head>
        <EmailFonts />
      </Head>
      <Preview>We noticed a sign-in from a new device.</Preview>
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

            <Text style={bodyText}>Hi {firstName},</Text>
            <Text style={bodyText}>
              Your Tummly account was used to sign in from a new device.
            </Text>
            <Text style={bodyText}>
              Time: {signInTime}
              <br />
              Device: {deviceSummary}
              <br />
              Approximate location: {locationSummary}
            </Text>
            <Text style={bodyText}>If this was you, no action is needed.</Text>
            <Text style={bodyTextLast}>
              If this was not you,{" "}
              <Link href={resetPasswordUrl} style={linkStyle}>
                reset your password
              </Link>{" "}
              and{" "}
              <Link href={`mailto:${supportEmail}`} style={linkStyle}>
                contact Tummly support
              </Link>
              .
            </Text>
          </Section>

          <HelpFooter
            helpCentreUrl={helpCentreUrl}
            disclaimer="If you did not request access to Tummly, you can ignore this email."
          />
        </Container>
      </Body>
    </Html>
  )
}

NewDeviceSignInEmail.PreviewProps = {
  firstName: "Alex",
  signInTime: "28 Sep 2026, 15:42 BST",
  deviceSummary: "Chrome on macOS",
  locationSummary: "London, United Kingdom",
  resetPasswordUrl: "https://app.tummly.com/forgot-password",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies NewDeviceSignInEmailProps
