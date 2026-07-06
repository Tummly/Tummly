import type { FormEvent } from "react"
import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { Field, FieldError, FieldLabel } from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_URL,
} from "@/config/support"
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero"
import { cn } from "@/lib/utils"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

type SignInActivationCodeStepProps = {
  activationCode: string
  submitting: boolean
  error: string | null
  onActivationCodeChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}

export function SignInActivationCodeStep({
  activationCode,
  submitting,
  error,
  onActivationCodeChange,
  onSubmit,
}: SignInActivationCodeStepProps) {
  return (
    <form
      onSubmit={onSubmit}
      noValidate
      className={cn(
        "flex w-full max-w-[480px] shrink-0 flex-col gap-[34px] rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)]",
        cardShadow
      )}
    >
      <header className="flex flex-col gap-4 text-[#232323]">
        <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
          Your setup is complete
        </h1>
        <p className="m-0 text-sm leading-normal">
          We&apos;re preparing your Tummly onboarding pack with your QR
          materials and activation code. Once it arrives, enter the activation
          code to open your workspace and start your 30-day trial.
        </p>
      </header>

      <div className="flex flex-col gap-4">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start">
          <Field
            className="min-w-0 flex-1"
            data-invalid={error ? true : undefined}
          >
            <FieldLabel htmlFor="activation-code" className="sr-only">
              Activation code
            </FieldLabel>
            <Input
              id="activation-code"
              name="activationCode"
              value={activationCode}
              onChange={(event) =>
                onActivationCodeChange(event.target.value)
              }
              placeholder="Activation code"
              autoComplete="one-time-code"
              disabled={submitting}
              aria-invalid={error ? true : undefined}
              className="h-11 rounded-[6px] border-[#d2d2d2] text-base"
            />
            {error ? <FieldError>{error}</FieldError> : null}
          </Field>

          <Button
            type="submit"
            disabled={submitting}
            className="h-11 shrink-0 rounded-[6px] px-5 text-sm font-semibold"
          >
            Enter activation code
          </Button>
        </div>

        <p className="m-0 text-sm leading-normal text-[#232323]">
          <span className="font-semibold">Need help getting started?</span>{" "}
          Visit the{" "}
          <Link
            to={HELP_CENTRE_URL}
            className="font-medium text-[#232323] underline underline-offset-2"
            onMouseEnter={prefetchHelpCentreHero}
            onFocus={prefetchHelpCentreHero}
            onTouchStart={prefetchHelpCentreHero}
          >
            Help Centre
          </Link>{" "}
          or{" "}
          <Link
            to={HELP_CENTRE_CONTACT_URL}
            className="font-medium text-[#232323] underline underline-offset-2"
          >
            contact support
          </Link>
          .
        </p>
      </div>
    </form>
  )
}
