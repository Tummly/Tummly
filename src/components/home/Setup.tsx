import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"

type SetupCardProps = {
  title: string
  description: string
  details: string
  buttonLabel: string
  buttonHref: string
}

function SetupCard({
  title,
  description,
  details,
  buttonLabel,
  buttonHref,
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
          <Link to={buttonHref}>{buttonLabel}</Link>
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
    buttonHref: "/request-trial",
  },
  {
    title: "Multiple locations",
    description:
      "For operators with 2 or more locations who need location-level setup, team roles and shared reporting.",
    details:
      "Includes location structure, team access, location-specific guest links, rollout checklist and reporting by location.",
    buttonLabel: "Request multi-location setup",
    buttonHref: "/request-trial",
  },
] as const

function Setup() {
  return (
    <section className="w-full bg-white">
      <div className="mx-auto flex w-full max-w-360 flex-col gap-12 px-4 py-12 sm:gap-14 sm:px-6 sm:py-16 md:px-10 lg:gap-15 lg:px-16 lg:py-22.5 xl:px-45">
        <header className="flex flex-col gap-3">
          <h2 className="max-w-4xl m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-[#232323]">
            Choose the setup that fits your restaurant
          </h2>
          <p className="max-w-2xl m-0 text-base font-medium leading-6.5 text-[#232323] sm:text-[17px] lg:text-lg">
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
              buttonHref={option.buttonHref}
            />
          ))}
        </div>
      </div>
    </section>
  )
}

export default Setup
