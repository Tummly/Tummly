import { PilotGradientPanel } from "@/components/marketing/pilot/PilotGradientPanel"
import {
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_WHY_ZERO,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

/**
 * Figma Pilot “Why £0?” `5681:46128`.
 * Gradient panel + centred rationale copy.
 */
export function PilotWhyZero() {
  return (
    <PilotGradientPanel headingId="pilot-why-zero-heading">
      <p className="m-0 font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
        {PILOT_WHY_ZERO.eyebrow}
      </p>
      <h2
        id="pilot-why-zero-heading"
        className={cn("m-0 w-full text-black", PILOT_SECTION_HEADING)}
      >
        {PILOT_WHY_ZERO.title}
      </h2>
      <div className={cn("flex w-full flex-col", PILOT_SECTION_BODY)}>
        <p className="m-0">{PILOT_WHY_ZERO.intro}</p>
        <div className="mt-6 flex w-full flex-col">
          {PILOT_WHY_ZERO.points.map((line) => (
            <p key={line} className="m-0">
              {line}
            </p>
          ))}
        </div>
      </div>
    </PilotGradientPanel>
  )
}
