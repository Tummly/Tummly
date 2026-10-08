import type { ReactNode } from "react"
import {
  Body,
  Container,
  Head,
  Html,
  Img,
  Link,
  Preview,
  Section,
  Text,
} from "react-email"

import { EmailFonts } from "./EmailFonts"
import { emailBodyStyle, guestEmailContainerStyle } from "./styles"
import { colors } from "./tokens"

/** Figma 6852:49961 display size (asset is 2×). */
export const TOP_DECORATION_WIDTH_PX = 314
export const TOP_DECORATION_HEIGHT_PX = 138

/** Figma 6852:49959 display height. */
export const BOTTOM_STRIP_HEIGHT_PX = 30

const font = "Arial, Helvetica, sans-serif"

export type NonTransactionalEmailShellProps = {
  preview: string
  brandTitle: string
  brandSubtitle: string
  brandLogoUrl: string
  topDecorationUrl: string
  poweredByLogoUrl: string
  bottomStripUrl: string
  disclaimer: string
  addressLine: string
  unsubscribeUrl: string
  termsUrl: string
  privacyUrl: string
  cookieUrl: string
  children: ReactNode
  /** Injected by C# as HTML (`{{offer_block}}`) when an offer is attached. */
  offerBlock?: ReactNode
}

/**
 * Guest-facing non-transactional chrome — Figma Guest-Loop-MVP 6852:49917.
 * Used for guest response / campaign / offer-unlocked emails.
 */
export function NonTransactionalEmailShell({
  preview,
  brandTitle,
  brandSubtitle,
  brandLogoUrl,
  topDecorationUrl,
  poweredByLogoUrl,
  bottomStripUrl,
  disclaimer,
  addressLine,
  unsubscribeUrl,
  termsUrl,
  privacyUrl,
  cookieUrl,
  children,
  offerBlock,
}: NonTransactionalEmailShellProps) {
  return (
    <Html lang="en">
      <Head>
        <EmailFonts />
      </Head>
      <Preview>{preview}</Preview>
      <Body
        style={{
          ...emailBodyStyle,
          backgroundColor: colors.black,
          fontFamily: font,
        }}
      >
        <Container style={guestEmailContainerStyle}>
          {/* Top-right food line-art — Figma 6852:49961 */}
          <Section
            data-guest-response-top-decoration="1"
            style={{ padding: 0, textAlign: "right", lineHeight: 0 }}
          >
            <Img
              src={topDecorationUrl}
              alt=""
              width={TOP_DECORATION_WIDTH_PX}
              height={TOP_DECORATION_HEIGHT_PX}
              style={{
                display: "block",
                width: `${TOP_DECORATION_WIDTH_PX}px`,
                height: `${TOP_DECORATION_HEIGHT_PX}px`,
                border: 0,
                marginLeft: "auto",
              }}
            />
          </Section>

          <Section
            style={{
              marginTop: `-${TOP_DECORATION_HEIGHT_PX}px`,
              position: "relative",
              zIndex: 1,
            }}
          >
            {/* Brand header — Figma 6852:49918 pt 62 / px 32 */}
            <Section
              data-non-transactional-slot="brand"
              style={{
                padding: "62px 32px 0 32px",
                fontFamily: font,
              }}
            >
              <table
                role="presentation"
                cellPadding={0}
                cellSpacing={0}
                border={0}
                style={{ borderCollapse: "collapse" }}
              >
                <tr>
                  <td
                    valign="middle"
                    style={{
                      verticalAlign: "middle",
                      paddingRight: "12px",
                    }}
                  >
                    <Img
                      src={brandLogoUrl}
                      alt=""
                      width={48}
                      height={48}
                      style={{
                        display: "block",
                        width: "48px",
                        height: "48px",
                        border: 0,
                        borderRadius: "2px",
                        objectFit: "cover",
                      }}
                    />
                  </td>
                  <td valign="middle" style={{ verticalAlign: "middle" }}>
                    <Text
                      style={{
                        margin: "0 0 4px 0",
                        fontSize: "22px",
                        fontWeight: 600,
                        lineHeight: "normal",
                        color: colors.white,
                        fontFamily: font,
                      }}
                    >
                      {brandTitle}
                    </Text>
                    {brandSubtitle.trim().length > 0 ? (
                      <Text
                        style={{
                          margin: 0,
                          fontSize: "12px",
                          fontWeight: 600,
                          lineHeight: "normal",
                          color: colors.white,
                          fontFamily: font,
                        }}
                      >
                        {brandSubtitle}
                      </Text>
                    ) : null}
                  </td>
                </tr>
              </table>
            </Section>

            {/* Ticket card — Figma 6852:49926 */}
            <Section style={{ padding: "40px 32px", fontFamily: font }}>
              <Section
                style={{
                  border: `1px solid ${colors.gray980}`,
                  borderRadius: "6px",
                  backgroundColor: colors.gray995,
                  padding: "32px",
                  fontFamily: font,
                }}
              >
                <Section data-non-transactional-slot="ticket">
                  {children}
                </Section>
                {/* Optional offer nest — Figma 6852:49929; empty when no offer */}
                {offerBlock}
              </Section>
            </Section>

            {/* Legal footer — Figma 6852:49944 */}
            <Section
              data-non-transactional-slot="legal"
              style={{
                padding: "32px 32px 60px",
                backgroundColor: colors.black,
                textAlign: "center",
                fontFamily: font,
              }}
            >
              <Text
                style={{
                  margin: "0 0 12px 0",
                  fontSize: "14px",
                  fontWeight: 400,
                  lineHeight: "19px",
                  color: colors.white,
                  fontFamily: font,
                  textAlign: "center",
                }}
              >
                {disclaimer}
              </Text>
              <Text
                style={{
                  margin: "0 0 26px 0",
                  fontSize: "12px",
                  fontWeight: 400,
                  lineHeight: "normal",
                  color: colors.white,
                  fontFamily: font,
                  textAlign: "center",
                }}
              >
                {addressLine}
              </Text>
              <Section
                style={{
                  borderTop: `1px solid ${colors.gray980}`,
                  paddingTop: "26px",
                  textAlign: "center",
                }}
              >
                <Link href={unsubscribeUrl} style={legalLinkStyle}>
                  Unsubscribe
                </Link>
                <Text style={legalGapStyle}> </Text>
                <Link href={termsUrl} style={legalLinkStyle}>
                  Terms
                </Link>
                <Text style={legalGapStyle}> </Text>
                <Link href={privacyUrl} style={legalLinkStyle}>
                  Privacy
                </Link>
                <Text style={legalGapStyle}> </Text>
                <Link href={cookieUrl} style={legalLinkStyle}>
                  Cookie settings
                </Link>
              </Section>
            </Section>

            {/* Powered by — Figma 6852:49955 */}
            <Section
              data-non-transactional-slot="poweredBy"
              style={{
                padding: "0 0 0 0",
                textAlign: "center",
                backgroundColor: colors.black,
                fontFamily: font,
              }}
            >
              <Text
                style={{
                  margin: "0 0 12px 0",
                  fontSize: "10px",
                  fontWeight: 500,
                  lineHeight: "normal",
                  color: "#f4f4f4",
                  fontFamily: font,
                  textAlign: "center",
                }}
              >
                Powered by{" "}
                <Img
                  src={poweredByLogoUrl}
                  alt="Tummly"
                  height={19}
                  style={{
                    display: "inline-block",
                    verticalAlign: "middle",
                    height: "19px",
                    width: "auto",
                    border: 0,
                    marginLeft: "6px",
                  }}
                />
              </Text>
            </Section>

            {/* Green grass strip — Figma 6852:49959 */}
            <Section
              data-guest-response-footer-strip="1"
              style={{ padding: 0, lineHeight: 0, fontSize: 0 }}
            >
              <Img
                src={bottomStripUrl}
                alt=""
                width={600}
                height={BOTTOM_STRIP_HEIGHT_PX}
                style={{
                  display: "block",
                  width: "100%",
                  maxWidth: "100%",
                  height: `${BOTTOM_STRIP_HEIGHT_PX}px`,
                  border: 0,
                }}
              />
            </Section>
          </Section>
        </Container>
      </Body>
    </Html>
  )
}

const legalLinkStyle = {
  color: colors.gray550,
  fontSize: "12px",
  fontWeight: 500,
  lineHeight: "normal",
  textDecoration: "none",
  whiteSpace: "nowrap" as const,
  fontFamily: font,
}

const legalGapStyle = {
  display: "inline",
  margin: "0 10px",
  fontSize: "12px",
  color: colors.gray550,
}
