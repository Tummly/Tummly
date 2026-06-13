import { Button } from "@/components/ui/button"

const NEXT_STEPS = [
  "We review your restaurant and setup details.",
  "If approved, we send a secure setup link.",
  "You create your account and restaurant workspace.",
  "We help you prepare your first guest prompts, feedback form and launch offer.",
] as const

type HeroTrialSuccessStepProps = {
  onReturnToTummly: () => void
  onSubmitAgain: () => void
}

function HeroTrialSuccessStep({
  onReturnToTummly,
  onSubmitAgain,
}: HeroTrialSuccessStepProps) {
  return (
    <div className="relative z-[2] flex min-h-full w-full flex-col text-[#232323]">
      <div className="flex flex-col gap-7 sm:gap-8 lg:gap-[34px]">
        <header className="flex flex-col gap-3 lg:gap-3">
          <h2 className="m-0 text-[clamp(1.375rem,3vw,1.75rem)] font-bold leading-[normal] tracking-[-0.56px]">
            Email verified
          </h2>
          <p className="m-0 text-sm font-medium leading-[21px] tracking-[-0.32px] sm:text-base">
            Thanks. We&apos;ve verified your email and received your guided
            trial request.
            <br />
            We&apos;ll review your restaurant details and send the right next
            setup step if your request is approved.
          </p>
        </header>

        <section className="flex flex-col gap-2">
          <h3 className="m-0 text-base font-bold leading-[normal] tracking-[-0.32px]">
            What happens next
          </h3>
          <ol className="m-0 list-decimal pl-[21px] text-sm font-medium leading-[22px] tracking-[-0.28px]">
            {NEXT_STEPS.map((step, index) => (
              <li
                key={step}
                className={index < NEXT_STEPS.length - 1 ? "mb-0" : undefined}
              >
                {step}
              </li>
            ))}
          </ol>
        </section>
      </div>

      <div className="mt-auto flex flex-col items-stretch gap-5 pt-7 sm:pt-8 lg:gap-[22px] lg:pt-[34px]">
        <Button
          type="button"
          onClick={onReturnToTummly}
          className="h-auto min-h-0 w-full rounded-[54px] border border-[rgba(20,162,71,0)] bg-[#14a247] px-5 py-[9px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
        >
          Return to Tummly
        </Button>

        <p className="m-0 max-w-[327px] text-sm font-medium leading-5 text-[#232323]">
          Used the wrong email?{" "}
          <Button
            type="button"
            variant="link"
            onClick={onSubmitAgain}
            className="h-auto min-h-0 p-0 text-sm font-medium text-[#232323] underline underline-offset-2"
          >
            Submit the form again
          </Button>{" "}
          or contact{" "}
          <Button
            variant="link"
            size="link-sm"
            asChild
            className="text-sm font-medium text-[#232323] underline underline-offset-2"
          >
            <a href="#">Tummly support</a>
          </Button>
          .
        </p>
      </div>
    </div>
  )
}

export default HeroTrialSuccessStep
