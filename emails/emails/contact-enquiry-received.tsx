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

import { BrandFooter } from "./_components/BrandFooter"
import { EmailFonts } from "./_components/EmailFonts"
import { EmailLogo } from "./_components/EmailLogo"
import {
  bodyTextGap20,
  bodyTextLast,
  dividerStyle,
  headingStyle,
  mainSectionStyle,
} from "./_components/styles"
import { colors, fontFunctional } from "./_components/tokens"

export type ContactEnquiryReceivedEmailProps = {
  topic: string
  reference: string
  privacyUrl: string
  companyDetailsUrl: string
  logoUrl: string
}

const tokenDefaults = {
  topic: "{{topic}}",
  reference: "{{reference}}",
  privacyUrl: "{{privacy_url}}",
  companyDetailsUrl: "{{company_details_url}}",
  logoUrl: "{{logo_url}}",
} satisfies ContactEnquiryReceivedEmailProps

/**
 * Contact confirmation — Figma Guest-Loop-MVP node 6852:41752.
 */
export default function ContactEnquiryReceivedEmail({
  topic = tokenDefaults.topic,
  reference = tokenDefaults.reference,
  privacyUrl = tokenDefaults.privacyUrl,
  companyDetailsUrl = tokenDefaults.companyDetailsUrl,
  logoUrl = tokenDefaults.logoUrl,
}: ContactEnquiryReceivedEmailProps = tokenDefaults) {
  const headline = "We've received your Tummly enquiry"

  return (
    <Html lang="en">
      <Head>
        <EmailFonts />
      </Head>
      <Preview>{`We've received your Tummly enquiry — ${reference}`}</Preview>
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

            <Text style={bodyTextGap20}>Hi,</Text>
            <Text style={bodyTextGap20}>
              Thanks for contacting Tummly. We&apos;ve received your enquiry
              about {topic}. Our team will review it and reply to this email
              address.
            </Text>
            <Text style={bodyTextGap20}>Reference: {reference}</Text>
            <Text style={bodyTextGap20}>
              To add information, reply to this email and keep the reference
              in the subject.
            </Text>
            <Text style={bodyTextLast}>The Tummly team</Text>
          </Section>

          <BrandFooter
            logoUrl={logoUrl}
            privacyUrl={privacyUrl}
            companyDetailsUrl={companyDetailsUrl}
          />
        </Container>
      </Body>
    </Html>
  )
}

ContactEnquiryReceivedEmail.PreviewProps = {
  topic: "Plans and pricing",
  reference: "TUM-000042",
  privacyUrl: "https://app.tummly.com/privacy",
  companyDetailsUrl: "https://app.tummly.com/company",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies ContactEnquiryReceivedEmailProps
