import aiAssistantIcon from "@/assets/svg/ui-icons/ai-assistant.png"
import { cn } from "@/lib/utils"

type AiAssistantIconProps = {
  className?: string
  /** Icon box size in px — navbar AI assistant button uses 18. */
  size?: 18 | 22 | 32
}

export function AiAssistantIcon({ className, size = 18 }: AiAssistantIconProps) {
  return (
    <span
      className={cn("relative shrink-0 overflow-hidden", className)}
      style={{ width: size, height: size }}
      aria-hidden
    >
      <img
        src={aiAssistantIcon}
        alt=""
        width={size}
        height={size}
        className="size-full object-cover"
        aria-hidden
      />
      <span
        aria-hidden
        className="absolute inset-0 bg-gradient-to-r from-[#14a946] to-[#135acc] mix-blend-hue"
      />
    </span>
  )
}
