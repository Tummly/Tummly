import {
  Body,
  Container,
  Head,
  Heading,
  Hr,
  Html,
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
  mainSectionStyle,
} from "./_components/styles"
import { colors, fontFunctional, fontHeadline } from "./_components/tokens"

export type OtpEmailProps = {
  /** Full H1 + subject line from C#. */
  headline: string
  otp: string
  helpCentreUrl: string
  logoUrl: string
}

const tokenDefaults = {
  headline: "{{headline}}",
  otp: "{{otp}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies OtpEmailProps

/**
 * OTP verification — subject pattern:
 * "Verify your email to join {{restaurant_name}} on Tummly"
 * (headline filled by C#; auth may use a shorter headline).
 */
export default function OtpEmail({
  headline = tokenDefaults.headline,
  otp = tokenDefaults.otp,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: OtpEmailProps = tokenDefaults) {
  return (
    <Html lang="en">
      <Head>
        <EmailFonts />
      </Head>
      <Preview>Your Tummly verification code</Preview>
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

            <Text style={bodyText}>
              Use this code to finish verifying your email on Tummly:
            </Text>
            <Text
              style={{
                margin: "0 0 32px",
                fontFamily: fontHeadline,
                fontSize: "30px",
                fontWeight: 600,
                lineHeight: "28px",
                letterSpacing: "0.02em",
                color: colors.black,
              }}
            >
              {otp}
            </Text>
            <Text style={bodyTextLast}>This code expires in 10 minutes.</Text>
          </Section>

          <HelpFooter
            helpCentreUrl={helpCentreUrl}
            disclaimer="If you did not try to sign in to Tummly, contact support."
          />
        </Container>
      </Body>
    </Html>
  )
}

OtpEmail.PreviewProps = {
  headline: "Verify your email to join Mehmet's Grill on Tummly",
  otp: "482913",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies OtpEmailProps
