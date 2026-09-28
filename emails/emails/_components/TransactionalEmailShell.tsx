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

import { EmailFonts } from "./EmailFonts"
import { EmailLogo } from "./EmailLogo"
import { HelpFooter } from "./HelpFooter"
import {
  dividerStyle,
  headingStyle,
  mainSectionStyle,
} from "./styles"
import { colors, fontFunctional } from "./tokens"

type TransactionalEmailShellProps = {
  preview: string
  headline: string
  logoUrl: string
  helpCentreUrl: string
  /** When true, footer includes copyright + legal links. */
  showLegalRow?: boolean
  termsUrl?: string
  privacyUrl?: string
  cookiePolicyUrl?: string
  children: ReactNode
}

/**
 * Light transactional chrome — logo, headline, divider, body, HelpFooter.
 * Used for trial / account-setup / help-centre mails migrated off BaseEmailTemplate.
 */
export function TransactionalEmailShell({
  preview,
  headline,
  logoUrl,
  helpCentreUrl,
  showLegalRow = false,
  termsUrl,
  privacyUrl,
  cookiePolicyUrl,
  children,
}: TransactionalEmailShellProps) {
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
          <HelpFooter
            helpCentreUrl={helpCentreUrl}
            showLegalRow={showLegalRow}
            termsUrl={termsUrl}
            privacyUrl={privacyUrl}
            cookiePolicyUrl={cookiePolicyUrl}
          />
        </Container>
      </Body>
    </Html>
  )
}
