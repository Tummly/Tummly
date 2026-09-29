import type { CSSProperties } from "react"

import { colors, fontFunctional, fontHeadline } from "./tokens"

export const bodyText: CSSProperties = {
  margin: "0 0 14px",
  fontFamily: fontFunctional,
  fontSize: "14px",
  fontWeight: 400,
  lineHeight: "20px",
  color: colors.black,
}

export const bodyTextLast: CSSProperties = {
  ...bodyText,
  margin: "0",
}

export const bodyTextGap20: CSSProperties = {
  ...bodyText,
  margin: "0 0 20px",
}

export const headingStyle: CSSProperties = {
  margin: "0 0 32px",
  fontFamily: fontHeadline,
  fontSize: "34px",
  fontWeight: 500,
  lineHeight: "42px",
  color: colors.black,
  textAlign: "center",
}

export const dividerStyle: CSSProperties = {
  border: "none",
  borderTop: `1px solid ${colors.divider}`,
  margin: "0 0 32px",
  width: "100%",
}

export const footerDividerStyle: CSSProperties = {
  border: "none",
  borderTop: `1px solid ${colors.divider}`,
  margin: "0 0 26px",
  width: "100%",
}

export const mainSectionStyle: CSSProperties = {
  padding: "38px 32px 48px",
  backgroundColor: colors.white,
}

export const footerSectionStyle: CSSProperties = {
  backgroundColor: colors.footerBg,
  padding: "48px 32px 38px",
}

export const helpTitleStyle: CSSProperties = {
  margin: "0 0 12px",
  fontFamily: fontFunctional,
  fontSize: "16px",
  fontWeight: 600,
  lineHeight: "24px",
  color: colors.black,
}

export const helpBodyStyle: CSSProperties = {
  margin: 0,
  fontFamily: fontFunctional,
  fontSize: "14px",
  fontWeight: 400,
  lineHeight: "20px",
  color: colors.black,
}

export const linkStyle: CSSProperties = {
  color: colors.black,
  textDecoration: "underline",
}

export const mutedNoteStyle: CSSProperties = {
  margin: 0,
  fontFamily: fontFunctional,
  fontSize: "12px",
  fontWeight: 500,
  lineHeight: "normal",
  color: colors.gray550,
}

export const legalLinkStyle: CSSProperties = {
  color: colors.gray555,
  fontFamily: fontFunctional,
  fontSize: "12px",
  fontWeight: 500,
  lineHeight: "20px",
  textDecoration: "none",
}

export const ctaButtonStyle: CSSProperties = {
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
}
