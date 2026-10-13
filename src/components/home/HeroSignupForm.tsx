import { CircleCheckBig } from "lucide-react"
import { type FormEvent, useId, useState } from "react"
import { Link, useNavigate } from "react-router-dom"

import { AuthSocialContinueButtons } from "@/components/auth/AuthSocialContinueButtons"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { Input } from "@/components/ui/input"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { HOME_HERO } from "@/content/marketing/homePage"
import {
  buildSignupPath,
  saveSignupPlanIntent,
} from "@/lib/signupPlanIntent"

/**
 * Home hero signup strip — Figma `5569:32620`.
 * Email + OAuth continue into `/signup` with Pilot intent (light chrome).
 */
export function HeroSignupForm() {
  const navigate = useNavigate()
  const emailFieldId = useId()
  const [email, setEmail] = useState("")

  const signupReturnPath = buildSignupPath(HOME_HERO.planIntent)

  const persistPlanIntent = () => {
    saveSignupPlanIntent(HOME_HERO.planIntent)
  }

  const goToSignup = (nextEmail: string) => {
    persistPlanIntent()
    navigate(
      buildSignupPath(HOME_HERO.planIntent, {
        email: nextEmail,
      }),
    )
  }

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    goToSignup(email)
  }

  return (
    <div className="flex w-full max-w-[573px] flex-col items-center gap-[26px]">
      <div className="inline-flex items-center gap-2 rounded-[40px] bg-[#ebebeb] px-2.5 py-2">
        <CircleCheckBig
          aria-hidden
          className="size-4 shrink-0 text-[#727272]"
          strokeWidth={2}
        />
        <p className="m-0 text-xs font-medium leading-normal text-[#727272]">
          {HOME_HERO.trustBadge}
        </p>
      </div>

      <div className="flex w-full flex-col gap-3">
        <form
          onSubmit={onSubmit}
          className="flex w-full flex-col items-stretch gap-2.5 sm:flex-row sm:items-center"
        >
          <label className="sr-only" htmlFor={emailFieldId}>
            {HOME_HERO.emailPlaceholder}
          </label>
          <Input
            id={emailFieldId}
            type="email"
            name="email"
            autoComplete="email"
            inputMode="email"
            placeholder={HOME_HERO.emailPlaceholder}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="h-[46px] min-h-[46px] flex-1 rounded-[8px] border-[#969696] bg-transparent px-[17px] py-[15px] text-base leading-5 text-[#141414] placeholder:text-[#7d7d7d] focus-visible:border-[#141414] focus-visible:ring-[#141414]/20 md:text-base"
          />
          <MarketingButton
            type="submit"
            variant="primary"
            className="h-[46px] min-h-[46px] w-full shrink-0 rounded-[8px] sm:w-[176px]"
          >
            {HOME_HERO.primaryLabel}
          </MarketingButton>
        </form>

        <div className="flex w-full items-center gap-2.5">
          <div className="h-px min-w-0 flex-1 bg-[#d0d0d0]" />
          <span className="shrink-0 text-sm leading-normal text-[#707070]">
            or
          </span>
          <div className="h-px min-w-0 flex-1 bg-[#d0d0d0]" />
        </div>

        <AuthSocialContinueButtons
          layout="row"
          tone="light"
          returnPath={signupReturnPath}
          onBeforeStart={persistPlanIntent}
          className="rounded-[8px]"
        />
      </div>

      <p className="m-0 text-center text-xs leading-[18px] text-[#141414]">
        By signing up, I agree to Tummly&apos;s{" "}
        <Link to={LEGAL_ROUTES.terms} className="underline underline-offset-2">
          Terms of Service
        </Link>{" "}
        and{" "}
        <Link
          to={LEGAL_ROUTES.privacy}
          className="underline underline-offset-2"
        >
          Privacy Policy
        </Link>
        .
      </p>
    </div>
  )
}
