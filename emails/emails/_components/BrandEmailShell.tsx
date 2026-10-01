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
  emailBodyStyle,
  emailContainerStyle,
  headingStyle,
  mainSectionStyle,
} from "./styles"

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
      <Body style={emailBodyStyle}>
        <Container style={emailContainerStyle}>
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
