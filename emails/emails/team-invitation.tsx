import type { CSSProperties } from "react"
import {
  Body,
  Button,
  Container,
  Head,
  Heading,
  Hr,
  Html,
  Img,
  Link,
  Preview,
  Section,
  Text,
} from "react-email"

import {
  emailBodyStyle,
  emailContainerStyle,
} from "./_components/styles"
import {
  colors,
  fontFunctional,
  fontHeadline,
  supportEmail,
} from "./_components/tokens"

export type TeamInvitationEmailProps = {
  greetingLine: string
  inviterName: string
  workspaceName: string
  roleName: string
  locationScope: string
  /** Empty string omits the personal-message paragraph. */
  invitationMessage: string
  acceptUrl: string
  helpCentreUrl: string
  logoUrl: string
}

/** Token defaults for `email export` → C# string replace. */
const tokenDefaults = {
  greetingLine: "{{greeting_line}}",
  inviterName: "{{inviter_name}}",
  workspaceName: "{{workspace_name}}",
  roleName: "{{role_name}}",
  locationScope: "{{location_scope}}",
  invitationMessage: "{{invitation_message}}",
  acceptUrl: "{{accept_url}}",
  helpCentreUrl: "{{help_centre_url}}",
  logoUrl: "{{logo_url}}",
} satisfies TeamInvitationEmailProps

const bodyText: CSSProperties = {
  margin: "0 0 14px",
  fontFamily: fontFunctional,
  fontSize: "14px",
  fontWeight: 400,
  lineHeight: "20px",
  color: colors.black,
}

const bodyTextLast: CSSProperties = {
  ...bodyText,
  margin: "0",
}

/**
 * Team invite — Figma Guest-Loop-MVP node 6845:13279.
 * Default props use {{tokens}} for C# fill after `email export`.
 */
export default function TeamInvitationEmail({
  greetingLine = tokenDefaults.greetingLine,
  inviterName = tokenDefaults.inviterName,
  workspaceName = tokenDefaults.workspaceName,
  roleName = tokenDefaults.roleName,
  locationScope = tokenDefaults.locationScope,
  invitationMessage = tokenDefaults.invitationMessage,
  acceptUrl = tokenDefaults.acceptUrl,
  helpCentreUrl = tokenDefaults.helpCentreUrl,
  logoUrl = tokenDefaults.logoUrl,
}: TeamInvitationEmailProps = tokenDefaults) {
  const headline = `You've been invited to ${workspaceName} on Tummly`
  const preview = `${inviterName} has invited you to join ${workspaceName} on Tummly.`

  return (
    <Html lang="en">
      <Head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link
          rel="preconnect"
          href="https://fonts.gstatic.com"
          crossOrigin="anonymous"
        />
        <link
          href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@500;600&display=swap"
          rel="stylesheet"
        />
      </Head>
      <Preview>{preview}</Preview>
      <Body style={emailBodyStyle}>
        <Container style={emailContainerStyle}>
          <Section
            style={{
              padding: "38px 32px 48px",
              backgroundColor: colors.white,
            }}
          >
            <Img
              src={logoUrl}
              alt="Tummly"
              width={150}
              height={38}
              style={{
                display: "block",
                margin: "0 auto 32px",
                border: 0,
                outline: "none",
              }}
            />

            <Heading
              as="h1"
              style={{
                margin: "0 0 32px",
                fontFamily: fontHeadline,
                fontSize: "34px",
                fontWeight: 500,
                lineHeight: "42px",
                color: colors.black,
                textAlign: "center",
              }}
            >
              {headline}
            </Heading>

            <Hr
              style={{
                border: "none",
                borderTop: `1px solid ${colors.divider}`,
                margin: "0 0 32px",
                width: "100%",
              }}
            />

            <Text style={bodyText}>{greetingLine}</Text>
            <Text style={bodyText}>
              {inviterName} has invited you to join {workspaceName} on Tummly.
            </Text>
            <Text style={bodyText}>
              You&apos;ll have access based on the role and Locations assigned
              to you:
            </Text>
            <Text style={bodyText}>
              Role: {roleName}
              <br />
              Location access: {locationScope}
            </Text>
            {invitationMessage ? (
              <Text data-slot="invitation-message" style={bodyText}>
                {invitationMessage}
              </Text>
            ) : null}
            <Text style={bodyTextLast}>Accept your invitation:</Text>

            <Section style={{ margin: "20px 0" }}>
              <Button
                href={acceptUrl}
                style={{
                  backgroundColor: colors.buttonGreen,
                  borderRadius: "4px",
                  color: colors.white,
                  display: "inline-block",
                  fontFamily: fontFunctional,
                  fontSize: "16px",
                  fontWeight: 500,
                  lineHeight: "20px",
                  padding: "15px 17px",
                  textAlign: "center",
                  textDecoration: "none",
                }}
              >
                Accept invitation
              </Button>
            </Section>

            <Text style={bodyText}>
              After accepting, you&apos;ll be able to sign in to Tummly and
              access the areas available to your role.
            </Text>
            <Text style={bodyText}>
              If you already have a Tummly account, sign in with the Email
              address this invitation was sent to. If not, you&apos;ll be guided
              through creating your account.
            </Text>
            <Text style={bodyText}>
              If you weren&apos;t expecting this invitation, you can ignore this
              Email.
            </Text>
            <Text style={bodyTextLast}>
              Thanks,
              <br />
              Tummly
            </Text>
          </Section>

          <Section
            style={{
              backgroundColor: colors.footerBg,
              padding: "48px 32px 38px",
            }}
          >
            <Text
              style={{
                margin: "0 0 12px",
                fontFamily: fontFunctional,
                fontSize: "16px",
                fontWeight: 600,
                lineHeight: "24px",
                color: colors.black,
              }}
            >
              Need help?
            </Text>
            <Text
              style={{
                margin: "0 0 26px",
                fontFamily: fontFunctional,
                fontSize: "14px",
                fontWeight: 400,
                lineHeight: "20px",
                color: colors.black,
              }}
            >
              Contact us at{" "}
              <Link
                href={`mailto:${supportEmail}`}
                style={{
                  color: colors.black,
                  textDecoration: "underline",
                }}
              >
                {supportEmail}
              </Link>{" "}
              or visit our{" "}
              <Link
                href={helpCentreUrl}
                style={{
                  color: colors.black,
                  textDecoration: "underline",
                }}
              >
                Help Centre
              </Link>
              .
            </Text>

            <Hr
              style={{
                border: "none",
                borderTop: `1px solid ${colors.divider}`,
                margin: "0 0 26px",
                width: "100%",
              }}
            />

            <Text
              style={{
                margin: 0,
                fontFamily: fontFunctional,
                fontSize: "12px",
                fontWeight: 500,
                lineHeight: "normal",
                color: colors.gray550,
              }}
            >
              If you did not request access to Tummly, you can ignore this
              email.
            </Text>
          </Section>
        </Container>
      </Body>
    </Html>
  )
}

/** Preview server sample (Figma frame copy). */
TeamInvitationEmail.PreviewProps = {
  greetingLine: "Hi Alex,",
  inviterName: "Jordan Lee",
  workspaceName: "Mehmet's Grill",
  roleName: "Manager",
  locationScope: "All locations",
  invitationMessage:
    "Looking forward to having you on the team — shout if you have any questions before you join.",
  acceptUrl: "https://app.tummly.com/team/invitations/preview-token",
  helpCentreUrl: "https://app.tummly.com/help-center",
  logoUrl: "/static/tummly-logo-dark.png",
} satisfies TeamInvitationEmailProps
