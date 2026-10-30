import { Target } from "lucide-react"

import { AboutIconBadge } from "@/components/marketing/about/AboutIconBadge"
import {
  ABOUT_HERO_EYEBROW,
  ABOUT_MISSION,
  ABOUT_MISSION_Y,
  ABOUT_SECTION_HEADING,
  ABOUT_SECTION_INSET,
} from "@/content/marketing/aboutPage"
import { cn } from "@/lib/utils"

/**
 * Figma Our mission `7023:13862`.
 * Centre intro, then the "Our ambition" icon, heading and paragraph.
 * The small corner bracket in the frame (`7024:14040`) is a stray layer — skipped.
 */
export function AboutMission() {
  return (
    <section
      className={cn("w-full bg-white", ABOUT_SECTION_INSET, ABOUT_MISSION_Y)}
    >
      <div className="flex w-full flex-col items-center gap-10 lg:gap-[70px]">
        <div className="flex w-full max-w-[638px] flex-col items-center gap-3.5 text-center">
          <p className={cn("m-0 w-full", ABOUT_HERO_EYEBROW)}>
            {ABOUT_MISSION.eyebrow}
          </p>
          <h2
            className={cn(
              "m-0 w-full",
              ABOUT_SECTION_HEADING,
              "text-[#0e0e0e]",
            )}
          >
            {ABOUT_MISSION.title}
          </h2>
        </div>

        <div className="flex w-full flex-col items-center gap-[22px] text-center text-[#141414]">
          <AboutIconBadge icon={Target} />
          <div className="flex w-full flex-col items-center gap-4">
            <h3 className="m-0 w-full font-jakarta text-xl font-bold leading-[29px]">
              {ABOUT_MISSION.ambitionTitle}
            </h3>
            <p className="m-0 w-full max-w-[562px] font-sans text-base font-normal leading-6">
              {ABOUT_MISSION.ambitionBody}
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}
