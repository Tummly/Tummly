import { Hr, Img, Link, Section, Text } from "react-email"

import { footerDividerStyle, footerSectionStyle } from "./styles"
import { brandTagline, colors, fontFunctional } from "./tokens"

type BrandFooterProps = {
  logoUrl: string
  privacyUrl: string
  companyDetailsUrl: string
}

export function BrandFooter({
  logoUrl,
  privacyUrl,
  companyDetailsUrl,
}: BrandFooterProps) {
  return (
    <Section style={footerSectionStyle}>
      <Img
        src={logoUrl}
        alt="Tummly"
        width={96}
        height={24}
        style={{
          display: "block",
          margin: "0 0 10px",
          border: 0,
          outline: "none",
        }}
      />
      <Text
        style={{
          margin: "0 0 26px",
          fontFamily: fontFunctional,
          fontSize: "14px",
          fontWeight: 400,
          lineHeight: "20px",
          color: colors.black,
          maxWidth: "338px",
        }}
      >
        {brandTagline}
      </Text>

      <Hr style={footerDividerStyle} />

      <Text style={{ margin: 0 }}>
        <Link
          href={privacyUrl}
          style={{
            color: colors.black,
            fontFamily: fontFunctional,
            fontSize: "12px",
            fontWeight: 400,
            lineHeight: "normal",
            textDecoration: "none",
            marginRight: "18px",
          }}
        >
          Privacy Notice
        </Link>
        <Link
          href={companyDetailsUrl}
          style={{
            color: colors.black,
            fontFamily: fontFunctional,
            fontSize: "12px",
            fontWeight: 400,
            lineHeight: "normal",
            textDecoration: "none",
          }}
        >
          Company details
        </Link>
      </Text>
    </Section>
  )
}
