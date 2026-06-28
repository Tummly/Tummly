import { Button } from "@/components/ui/button"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionShell,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type SetupCardProps = {
  title: string
  description: string
  details: string
  buttonLabel: string
}

function SetupCard({
  title,
  description,
  details,
  buttonLabel,
}: SetupCardProps) {
  return (
    <article className="flex h-full flex-col overflow-hidden rounded-[6px] border border-[#d2d2d2] bg-white">
      <div className="bg-[#f4f4f4] px-7.5 py-7.5">
        <h3 className="m-0 text-[22px] font-bold leading-[normal] text-[#232323]">
          {title}
        </h3>
      </div>

      <div className="flex flex-1 flex-col gap-10 p-7.5">
        <div className="flex flex-col gap-2.5">
          <p className="m-0 text-sm font-normal leading-4.75 text-[#232323]">
            {description}
          </p>
          <p className="m-0 text-sm font-normal leading-4.75 text-[#232323]">
            {details}
          </p>
        </div>

        <Button variant="secondary" className="self-start" asChild>
          <RequestTrialLink>{buttonLabel}</RequestTrialLink>
        </Button>
      </div>
    </article>
  )
}

const setupOptions = [
  {
    title: "Single location",
    description:
      "For independent restaurants, cafés, takeaways and quick-service operators starting with one site.",
    details:
      "Includes one workspace, guest links, QR prompts, a short feedback form, one starter offer and a weekly brief.",
    buttonLabel: "Request single-location trial",
  },
  {
    title: "Multiple locations",
    description:
      "For operators with 2 or more locations who need location-level setup, team roles and shared reporting.",
    details:
      "Includes location structure, team access, location-specific guest links, rollout checklist and reporting by location.",
    buttonLabel: "Request multi-location setup",
  },
] as const

function Setup() {
  return (
    <section className="w-full bg-white">
      <div className={marketingSectionShell()}>
        <header className="flex flex-col gap-3">
          <h2 className={cn("m-0 max-w-4xl", marketingSectionHeading)}>
            Choose the setup that fits your restaurant
          </h2>
          <p className={cn("m-0 max-w-2xl", marketingSectionBody)}>
            Start with one location or tell us about a group setup. We&apos;ll
            review your details and send the right next step.
          </p>
        </header>

        <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
          {setupOptions.map((option) => (
            <SetupCard
              key={option.title}
              title={option.title}
              description={option.description}
              details={option.details}
              buttonLabel={option.buttonLabel}
            />
          ))}
        </div>
      </div>
    </section>
  )
}

export default Setup
