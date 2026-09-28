import type { CSSProperties } from "react"
import { Img } from "react-email"

type EmailLogoProps = {
  logoUrl: string
  width?: number
  height?: number
  style?: CSSProperties
}

export function EmailLogo({
  logoUrl,
  width = 150,
  height = 38,
  style,
}: EmailLogoProps) {
  return (
    <Img
      src={logoUrl}
      alt="Tummly"
      width={width}
      height={height}
      style={{
        display: "block",
        margin: "0 auto 32px",
        border: 0,
        outline: "none",
        ...style,
      }}
    />
  )
}
