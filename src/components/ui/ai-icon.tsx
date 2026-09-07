import aiIcon from "@/assets/svg/ui-icons/ai-icon.svg"
import { cn } from "@/lib/utils"

type AiIconProps = {
  className?: string
  /**
   * Icon box size in px. Omit and pass `className` (e.g. `size-4`) when the
   * parent already sets size. Default 18.
   * 16 / 24 — compact UI; 18 — navbar / AI actions; 32 — section headers;
   * 48 — overlays.
   */
  size?: 16 | 18 | 22 | 24 | 26 | 28 | 32 | 38 | 48
}

/**
 * Brand AI glyph (`assets/svg/ui-icons/ai-icon.svg`).
 * Fills with `currentColor` so it follows parent text (black in light mode,
 * white in dark mode via `text-op-text-primary` / shell chrome). Pass a text
 * color class when the parent is not already themed.
 */
export function AiIcon({ className, size = 18 }: AiIconProps) {
  return (
    <span
      aria-hidden
      className={cn("inline-block shrink-0 bg-current", className)}
      style={{
        width: size,
        height: size,
        maskImage: `url(${aiIcon})`,
        maskSize: "contain",
        maskRepeat: "no-repeat",
        maskPosition: "center",
        WebkitMaskImage: `url(${aiIcon})`,
        WebkitMaskSize: "contain",
        WebkitMaskRepeat: "no-repeat",
        WebkitMaskPosition: "center",
      }}
    />
  )
}
