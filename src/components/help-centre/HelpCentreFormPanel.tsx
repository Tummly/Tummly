import type { ReactNode } from "react"

import { AuthFormAccent } from "@/components/auth/AuthFormAccent"
import { HELP_CENTRE_FORM_WIDTH } from "@/components/help-centre/helpCentreLayout"
import { cn } from "@/lib/utils"

type HelpCentreFormPanelProps = {
  children: ReactNode
  className?: string
}

/** Centered form shell with Figma line-art accent (contact + success). */
export function HelpCentreFormPanel({
  children,
  className,
}: HelpCentreFormPanelProps) {
  return (
    <div
      className={cn(
        "relative w-full overflow-hidden bg-white px-[25px] py-[70px] lg:px-16 lg:py-[90px] xl:px-45",
        className
      )}
    >
      <AuthFormAccent variant="onboarding" />
      <div className={cn("relative z-10 mx-auto flex w-full flex-col items-center", HELP_CENTRE_FORM_WIDTH)}>
        {children}
      </div>
    </div>
  )
}
