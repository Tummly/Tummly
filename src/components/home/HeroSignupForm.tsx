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
  type SignupPlanIntent,
} from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

export type HeroSignupFormProps = {
  /** Figma trust chip above the email row. Default true. */
  showTrustBadge?: boolean
  trustBadge?: string
  emailPlaceholder?: string
  primaryLabel?: string
  planIntent?: SignupPlanIntent
  /** Centered (home / groups) or start-aligned (Pilot split hero). */
  align?: "center" | "start"
  className?: string
}

/**
 * Marketing hero signup strip — Figma `5569:32620` / SignUp `6215:17664`.
 * Email + OAuth continue into `/signup` with plan intent (light chrome).
 */
export function HeroSignupForm({
  showTrustBadge = true,
  trustBadge = HOME_HERO.trustBadge,
  emailPlaceholder = HOME_HERO.emailPlaceholder,
  primaryLabel = HOME_HERO.primaryLabel,
  planIntent = HOME_HERO.planIntent,
  align = "center",
  className,
}: HeroSignupFormProps) {
  const navigate = useNavigate()
  const emailFieldId = useId()
  const [email, setEmail] = useState("")

  const signupReturnPath = buildSignupPath(planIntent)

  const persistPlanIntent = () => {
    saveSignupPlanIntent(planIntent)
  }

  const goToSignup = (nextEmail: string) => {
    persistPlanIntent()
    navigate(
      buildSignupPath(planIntent, {
        email: nextEmail,
      }),
    )
  }

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    goToSignup(email)
  }

  return (
    <div
      className={cn(
        "flex w-full max-w-[573px] flex-col gap-[26px]",
        align === "center" ? "items-center" : "items-start",
        className,
      )}
    >
      {showTrustBadge ? (
        <div className="inline-flex items-center gap-2 rounded-[40px] bg-[#ebebeb] px-2.5 py-2 lg:px-2.5">
          <CircleCheckBig
            aria-hidden
            className="size-[13px] shrink-0 text-[#727272] lg:size-4"
            strokeWidth={2}
          />
          <p className="m-0 text-[10px] font-inter font-medium leading-[12px] text-[#727272] lg:text-xs lg:leading-[normal]">
            {trustBadge}
          </p>
        </div>
      ) : null}

      <div className="flex w-full flex-col gap-3">
        <form
          onSubmit={onSubmit}
          className="flex w-full flex-col items-stretch gap-2.5 sm:flex-row sm:items-center"
        >
          <label className="sr-only" htmlFor={emailFieldId}>
            {emailPlaceholder}
          </label>
          <Input
            id={emailFieldId}
            type="email"
            name="email"
            autoComplete="email"
            inputMode="email"
            placeholder={emailPlaceholder}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="h-[46px] min-h-[46px] flex-1 rounded-[8px] border-[#969696] bg-transparent px-[17px] py-[15px] text-base leading-5 font-inter text-[#141414] placeholder:text-[#7d7d7d] focus-visible:border-[#141414] focus-visible:ring-[#141414]/20 md:text-base"
          />
          <MarketingButton
            type="submit"
            variant="primary"
            className="h-[46px] min-h-[46px] w-full shrink-0 rounded-[8px] sm:w-[176px]"
          >
            {primaryLabel}
          </MarketingButton>
        </form>

        <div className="flex w-full items-center gap-2.5">
          <div className="h-px min-w-0 flex-1 bg-[#d0d0d0]" />
          <span className="shrink-0 text-sm leading-[normal] text-[#707070]">
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

      <p
        className={cn(
          "m-0 w-full text-xs leading-4 text-[#141414]",
          align === "center" ? "text-center leading-[18px]" : "text-left",
        )}
      >
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
