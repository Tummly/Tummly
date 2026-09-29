import { Hr, Link, Section, Text } from "react-email"

import {
  footerDividerStyle,
  footerSectionStyle,
  helpBodyStyle,
  helpTitleStyle,
  linkStyle,
  mutedNoteStyle,
} from "./styles"
import { supportEmail } from "./tokens"

type HelpFooterProps = {
  helpCentreUrl: string
  /** Optional disclaimer under the divider. Omit for help-only footers. */
  disclaimer?: string
  /** When true, render copyright + legal links instead of disclaimer. */
  showLegalRow?: boolean
  termsUrl?: string
  privacyUrl?: string
  cookiePolicyUrl?: string
  copyrightYear?: string
}

export function HelpFooter({
  helpCentreUrl,
  disclaimer,
  showLegalRow = false,
  termsUrl = "{{terms_url}}",
  privacyUrl = "{{privacy_url}}",
  cookiePolicyUrl = "{{cookie_policy_url}}",
  copyrightYear = "2026",
}: HelpFooterProps) {
  return (
    <Section style={footerSectionStyle}>
      <Text style={helpTitleStyle}>Need help?</Text>
      <Text
        style={{
          ...helpBodyStyle,
          margin: showLegalRow || disclaimer ? "0 0 26px" : "0",
        }}
      >
        Contact us at{" "}
        <Link href={`mailto:${supportEmail}`} style={linkStyle}>
          {supportEmail}
        </Link>{" "}
        or visit our{" "}
        <Link href={helpCentreUrl} style={linkStyle}>
          Help Centre
        </Link>
        .
      </Text>

      {(disclaimer || showLegalRow) && (
        <Hr style={footerDividerStyle} />
      )}

      {disclaimer ? <Text style={mutedNoteStyle}>{disclaimer}</Text> : null}

      {showLegalRow ? (
        <table
          role="presentation"
          width="100%"
          cellPadding={0}
          cellSpacing={0}
          style={{ borderCollapse: "collapse" }}
        >
          <tbody>
            <tr>
              <td
                style={{
                  fontFamily: helpBodyStyle.fontFamily,
                  fontSize: "12px",
                  fontWeight: 500,
                  color: "#555555",
                  verticalAlign: "middle",
                }}
              >
                © {copyrightYear} Tummly
              </td>
              <td align="right" style={{ verticalAlign: "middle" }}>
                <Link
                  href={helpCentreUrl}
                  style={{
                    color: "#555555",
                    fontFamily: helpBodyStyle.fontFamily,
                    fontSize: "12px",
                    fontWeight: 500,
                    lineHeight: "20px",
                    textDecoration: "none",
                    marginRight: "24px",
                  }}
                >
                  Help Centre
                </Link>
                <Link
                  href={termsUrl}
                  style={{
                    color: "#555555",
                    fontFamily: helpBodyStyle.fontFamily,
                    fontSize: "12px",
                    fontWeight: 500,
                    lineHeight: "20px",
                    textDecoration: "none",
                    marginRight: "24px",
                  }}
                >
                  Terms
                </Link>
                <Link
                  href={privacyUrl}
                  style={{
                    color: "#555555",
                    fontFamily: helpBodyStyle.fontFamily,
                    fontSize: "12px",
                    fontWeight: 500,
                    lineHeight: "20px",
                    textDecoration: "none",
                    marginRight: "24px",
                  }}
                >
                  Privacy
                </Link>
                <Link
                  href={cookiePolicyUrl}
                  style={{
                    color: "#555555",
                    fontFamily: helpBodyStyle.fontFamily,
                    fontSize: "12px",
                    fontWeight: 500,
                    lineHeight: "20px",
                    textDecoration: "none",
                  }}
                >
                  Cookie settings
                </Link>
              </td>
            </tr>
          </tbody>
        </table>
      ) : null}
    </Section>
  )
}
