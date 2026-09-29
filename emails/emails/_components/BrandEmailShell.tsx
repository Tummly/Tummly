import type { ReactNode } from "react"
import {
  Body,
  Container,
  Head,
  Heading,
  Hr,
  Html,
  Preview,
  Section,
} from "react-email"

import { BrandFooter } from "./BrandFooter"
import { EmailFonts } from "./EmailFonts"
import { EmailLogo } from "./EmailLogo"
import {
  dividerStyle,
  headingStyle,
  mainSectionStyle,
} from "./styles"
import { colors, fontFunctional } from "./tokens"

type BrandEmailShellProps = {
  preview: string
  headline: string
  logoUrl: string
  privacyUrl: string
  companyDetailsUrl: string
  children: ReactNode
}

export function BrandEmailShell({
  preview,
  headline,
  logoUrl,
  privacyUrl,
  companyDetailsUrl,
  children,
}: BrandEmailShellProps) {
  return (
    <Html lang="en">
      <Head>
        <EmailFonts />
      </Head>
      <Preview>{preview}</Preview>
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
            {children}
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
