import { Building2 } from "lucide-react"

import { resolveBrandLogoSrc } from "@/lib/brandLogo/resolveBrandLogoSrc"
import { cn } from "@/lib/utils"

type BrandLogoMarkProps = {
  brandLogoPublicUrl: string | null | undefined
  /** Outer slot size classes, e.g. `size-12` or `size-[26px]`. */
  className?: string
  /** Corner radius — Location switcher uses `rounded-[2px]`; guest form uses `rounded-md`. */
  roundedClassName?: string
}

/**
 * Brand mark — public logo URL when persisted; else Building2 on a rounded
 * square (light: gray fill + black icon; dark: black fill + white icon).
 */
export function BrandLogoMark({
  brandLogoPublicUrl,
  className,
  roundedClassName = "rounded-[2px]",
}: BrandLogoMarkProps) {
  const resolvedSrc = resolveBrandLogoSrc(brandLogoPublicUrl ?? null)

  return (
    <span
      className={cn(
        "relative flex shrink-0 items-center justify-center overflow-hidden",
        roundedClassName,
        className,
        resolvedSrc == null &&
          "bg-op-color-gray-200 text-black dark:bg-black dark:text-white"
      )}
      aria-hidden
    >
      {resolvedSrc != null ? (
        <img src={resolvedSrc} alt="" className="size-full object-cover" />
      ) : (
        <Building2 className="size-[55%] stroke-[1.75]" />
      )}
    </span>
  )
}
