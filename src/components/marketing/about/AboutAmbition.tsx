import { Mountain, Target } from "lucide-react"
import type { LucideIcon } from "lucide-react"

import {
  ABOUT_AMBITION,
  ABOUT_AMBITION_Y,
  ABOUT_HERO_EYEBROW,
  ABOUT_SECTION_HEADING,
  ABOUT_SECTION_INSET,
  type AboutAmbitionColumn,
} from "@/content/marketing/aboutPage"
import { cn } from "@/lib/utils"

const COLUMN_ICONS = {
  opportunity: Mountain,
  ambition: Target,
} as const satisfies Record<AboutAmbitionColumn["id"], LucideIcon>

/** Closing line — *headline* Jakarta 28 / 38 medium. */
const ambitionClosing =
  "font-jakarta text-[22px] font-medium leading-7 tracking-normal text-[#0e0e0e] lg:text-[28px] lg:leading-[38px]"

function AmbitionColumn({
  column,
}: {
  column: AboutAmbitionColumn
}) {
  const Icon = COLUMN_ICONS[column.id]

  return (
    <div className="flex w-full max-w-[535px] flex-col items-center gap-3.5 lg:gap-[14px]">
      <div
        className="flex size-[46px] shrink-0 items-center justify-center rounded-full bg-[#141414] text-white"
        aria-hidden
      >
        <Icon className="size-5" strokeWidth={1.75} />
      </div>
      <div className="flex w-full flex-col items-center gap-4 text-center text-[#141414]">
        <h3 className="m-0 w-full font-jakarta text-xl font-bold leading-[29px]">
          {column.title}
        </h3>
        <p className="m-0 w-full max-w-[479px] font-sans text-base font-normal leading-6">
          {column.body}
        </p>
      </div>
    </div>
  )
}

/**
 * Figma Our ambition `5673:24231`.
 * Centre intro, opportunity / ambition columns, closing line.
 * Bottom radii close the About white card before the footer.
 */
export function AboutAmbition() {
  return (
    <section
      className={cn(
        "w-full overflow-hidden rounded-bl-[12px] rounded-br-[12px] bg-white",
        ABOUT_SECTION_INSET,
        ABOUT_AMBITION_Y,
      )}
    >
      <div className="flex w-full flex-col items-center gap-10 lg:gap-[70px]">
        <div className="flex w-full max-w-[590px] flex-col items-center gap-3.5 text-center">
          <p className={cn("m-0 w-full", ABOUT_HERO_EYEBROW)}>
            {ABOUT_AMBITION.eyebrow}
          </p>
          <h2
            className={cn(
              "m-0 w-full",
              ABOUT_SECTION_HEADING,
              "text-[#0e0e0e]",
            )}
          >
            {ABOUT_AMBITION.title}
          </h2>
        </div>

        <div className="flex w-full flex-col items-center gap-10 lg:flex-row lg:items-start lg:justify-center lg:gap-[70px]">
          {ABOUT_AMBITION.columns.map((column) => (
            <AmbitionColumn key={column.id} column={column} />
          ))}
        </div>

        <p
          className={cn(
            "m-0 w-full max-w-[512px] text-center",
            ambitionClosing,
          )}
        >
          {ABOUT_AMBITION.closing}
        </p>
      </div>
    </section>
  )
}
