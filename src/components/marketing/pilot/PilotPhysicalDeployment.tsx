import { PilotGradientPanel } from "@/components/marketing/pilot/PilotGradientPanel"
import {
  PILOT_PHYSICAL_DEPLOYMENT,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

/**
 * Figma Pilot “Physical deployment” `5681:46057`.
 * Online setup vs physical materials copy.
 */
export function PilotPhysicalDeployment() {
  return (
    <PilotGradientPanel headingId="pilot-physical-deployment-heading">
      <p className="m-0 font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
        {PILOT_PHYSICAL_DEPLOYMENT.eyebrow}
      </p>
      <h2
        id="pilot-physical-deployment-heading"
        className={cn("m-0 w-full text-black", PILOT_SECTION_HEADING)}
      >
        {PILOT_PHYSICAL_DEPLOYMENT.title}
      </h2>
      <div className={cn("flex w-full flex-col", PILOT_SECTION_BODY)}>
        <p className="m-0">{PILOT_PHYSICAL_DEPLOYMENT.intro}</p>
        <div className="mt-6 flex w-full flex-col">
          {PILOT_PHYSICAL_DEPLOYMENT.details.map((line) => (
            <p key={line} className="m-0">
              {line}
            </p>
          ))}
        </div>
        <p className="m-0 mt-6">{PILOT_PHYSICAL_DEPLOYMENT.footnote}</p>
      </div>
    </PilotGradientPanel>
  )
}
