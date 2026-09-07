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

/** Brand AI glyph — green-to-blue petal ring (`assets/svg/ui-icons/ai-icon.svg`). */
export function AiIcon({ className, size = 18 }: AiIconProps) {
  return (
    <img
      src={aiIcon}
      alt=""
      width={size}
      height={size}
      className={cn("shrink-0 object-contain", className)}
      style={{ width: size, height: size }}
      aria-hidden
    />
  )
}
