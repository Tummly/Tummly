import { PilotGradientPanel } from "@/components/marketing/pilot/PilotGradientPanel"
import {
  PILOT_ACTIVATION_JOURNEY,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

/**
 * Figma Pilot “From setup to live” `5681:46039`.
 * Activation journey steps + live-start footnote.
 */
export function PilotActivationJourney() {
  return (
    <PilotGradientPanel headingId="pilot-activation-journey-heading">
      <p className="m-0 font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
        {PILOT_ACTIVATION_JOURNEY.eyebrow}
      </p>
      <h2
        id="pilot-activation-journey-heading"
        className={cn("m-0 w-full text-black", PILOT_SECTION_HEADING)}
      >
        {PILOT_ACTIVATION_JOURNEY.title}
      </h2>
      <div className={cn("flex w-full flex-col gap-0", PILOT_SECTION_BODY)}>
        {PILOT_ACTIVATION_JOURNEY.steps.map((step) => (
          <p key={step} className="m-0">
            {step}
          </p>
        ))}
        <p className="m-0 mt-6">{PILOT_ACTIVATION_JOURNEY.footnote}</p>
      </div>
    </PilotGradientPanel>
  )
}
