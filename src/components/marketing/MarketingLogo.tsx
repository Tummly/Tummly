import type { ImgHTMLAttributes } from "react"

import logo from "@/assets/svg/logo.svg"
import { cn } from "@/lib/utils"

type MarketingLogoProps = Omit<
  ImgHTMLAttributes<HTMLImageElement>,
  "src" | "alt"
> & {
  /** Light chrome needs a dark wordmark — existing logo is white-on-transparent. */
  onLight?: boolean
}

/** Tummly wordmark for marketing chrome. */
export function MarketingLogo({
  onLight = true,
  className,
  width = 118,
  height = 30,
  ...props
}: MarketingLogoProps) {
  return (
    <img
      src={logo}
      alt="Tummly"
      width={width}
      height={height}
      className={cn(
        "block h-[30px] w-auto object-contain",
        onLight && "brightness-0",
        className,
      )}
      {...props}
    />
  )
}
