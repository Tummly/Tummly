import aiAssistantIcon from "@/assets/svg/ui-icons/ai-assistant.png"
import { cn } from "@/lib/utils"

type AiAssistantIconProps = {
  className?: string
  /**
   * Icon box size in px.
   * 18 — navbar / AI actions; 26 — Feedback details AI classification;
   * 32 — Recommended next step; 48 — Preparing response draft overlay.
   */
  size?: 18 | 22 | 26 | 32 | 38 | 48
}

/** Brand gradient petals — mask keeps transparent corners clear (no square box). */
export function AiAssistantIcon({ className, size = 18 }: AiAssistantIconProps) {
  return (
    <span
      className={cn("block shrink-0", className)}
      style={{
        width: size,
        height: size,
        backgroundImage: "linear-gradient(90deg, #14a946 0%, #135acc 100%)",
        WebkitMaskImage: `url(${aiAssistantIcon})`,
        WebkitMaskSize: "contain",
        WebkitMaskRepeat: "no-repeat",
        WebkitMaskPosition: "center",
        maskImage: `url(${aiAssistantIcon})`,
        maskSize: "contain",
        maskRepeat: "no-repeat",
        maskPosition: "center",
      }}
      aria-hidden
    />
  )
}
