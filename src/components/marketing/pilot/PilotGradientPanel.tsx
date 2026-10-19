import type { ReactNode } from "react"

import panelGradientImage from "@/assets/images/marketing/pilot/panel-gradient.png"
import {
  PILOT_SECTION_INSET,
  PILOT_SECTION_Y,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

type PilotGradientPanelProps = {
  headingId: string
  children: ReactNode
  /** Optional CTA row under the copy (Figma card gap 53px). */
  footer?: ReactNode
  className?: string
}

/**
 * Shared Figma Pilot panel chrome — green smoke frame + white inner card
 * (`5681:46128`).
 */
export function PilotGradientPanel({
  headingId,
  children,
  footer,
  className,
}: PilotGradientPanelProps) {
  return (
    <section
      className={cn("w-full bg-white", className)}
      aria-labelledby={headingId}
    >
      <div
        className={cn(
          "flex w-full flex-col items-stretch",
          PILOT_SECTION_INSET,
          PILOT_SECTION_Y,
        )}
      >
        <div className="relative flex w-full items-center justify-center overflow-hidden rounded-[12px] p-6 sm:p-10">
          <img
            src={panelGradientImage}
            alt=""
            width={3840}
            height={1600}
            className="pointer-events-none absolute inset-0 size-full object-cover object-center"
            decoding="async"
            aria-hidden
          />
          <div
            className={cn(
              "relative flex w-full flex-col items-center rounded-[10px] bg-white p-8 sm:p-[50px]",
              footer != null && "gap-[53px]",
            )}
          >
            <div className="flex w-full max-w-[615px] flex-col items-center justify-center gap-[18px] text-center">
              {children}
            </div>
            {footer}
          </div>
        </div>
      </div>
    </section>
  )
}
