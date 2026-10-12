import { useId, useState, type CSSProperties, type FormEvent } from "react"
import { CircleCheckBig } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import { signupCtaBgPicture } from "@/assets/marketing-images"
import { AuthSocialContinueButtons } from "@/components/auth/AuthSocialContinueButtons"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import OptimizedImage from "@/components/media/OptimizedImage"
import { Input } from "@/components/ui/input"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { PANORAMIC_BG_IMAGE_SIZES } from "@/lib/imagePresets"
import { signUpCtaVideoUrl } from "@/lib/marketingMedia"
import {
  buildSignupPath,
  clearSignupPlanIntent,
  saveSignupPlanIntent,
  type SignupPlanIntent,
} from "@/lib/signupPlanIntent"
import {
  marketingHeroBody,
  marketingSectionHeading,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/** Desktop: left-fade over right-anchored media (Figma `4151:3811`). */
const SIGN_UP_GRADIENT_DESKTOP =
  "linear-gradient(90deg, rgb(20, 20, 20) 23.523%, rgba(20, 20, 20, 0) 100%)"

/** Mobile: top-fade when media stacks below the copy. */
const SIGN_UP_GRADIENT_MOBILE =
  "linear-gradient(180deg, rgb(20, 20, 20) 0%, rgba(20, 20, 20, 0) 72%)"

/**
 * Soften the media edge against `#141414`.
 * Desktop: left edge. Mobile: top edge (video below copy).
 */
const SIGN_UP_MEDIA_MASK_DESKTOP =
  "linear-gradient(90deg, transparent 0%, #000 38%, #000 100%)"
const SIGN_UP_MEDIA_MASK_MOBILE =
  "linear-gradient(180deg, transparent 0%, #000 42%, #000 100%)"

export type MarketingSignUpCtaContent = {
  title: string
  body: string
  primaryLabel: string
  badge: string
  /** When set, primary CTA and OAuth open signup with this plan intent. */
  planIntent?: SignupPlanIntent | null
}

type MarketingSignUpCtaProps = {
  content: MarketingSignUpCtaContent
}

/**
 * Shared marketing bottom CTA (Figma `5661:14510` / `4151:3811`).
 * Email + OAuth continue into `/signup` without re-entering the work email.
 */
export function MarketingSignUpCta({ content }: MarketingSignUpCtaProps) {
  const navigate = useNavigate()
  const emailFieldId = useId()
  const [email, setEmail] = useState("")
  const [videoFailed, setVideoFailed] = useState(false)

  const signupReturnPath = buildSignupPath(content.planIntent)

  const persistPlanIntent = () => {
    if (content.planIntent != null) {
      saveSignupPlanIntent(content.planIntent)
    } else {
      clearSignupPlanIntent()
    }
  }

  const goToSignup = (nextEmail: string) => {
    persistPlanIntent()
    navigate(
      buildSignupPath(content.planIntent, {
        email: nextEmail,
      }),
    )
  }

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    goToSignup(email)
  }

  const mediaMaskVars = {
    "--sign-up-media-mask": SIGN_UP_MEDIA_MASK_MOBILE,
    "--sign-up-media-mask-lg": SIGN_UP_MEDIA_MASK_DESKTOP,
  } as CSSProperties

  return (
    <section className="w-full pt-5">
      {/* Figma Sign up `4151:3811` / `5661:14510` is 1688×615 (instance 1668×615). */}
      <div className="relative flex w-full flex-col overflow-hidden rounded-[8px] bg-[#141414] lg:aspect-[1668/615] lg:block">
        {/*
          Mobile: media stacks below copy with a top fade.
          Desktop: right-anchored 16:9 panel with a left fade (Figma).
        */}
        <div
          aria-hidden
          className={cn(
            "relative order-2 aspect-video w-full overflow-hidden [&_picture]:contents",
            "lg:absolute lg:inset-y-0 lg:right-0 lg:order-none lg:h-full lg:w-auto lg:max-w-[72%]",
            "[mask-image:var(--sign-up-media-mask)] [-webkit-mask-image:var(--sign-up-media-mask)]",
            "lg:[mask-image:var(--sign-up-media-mask-lg)] lg:[-webkit-mask-image:var(--sign-up-media-mask-lg)]",
          )}
          style={mediaMaskVars}
        >
          <OptimizedImage
            picture={signupCtaBgPicture}
            sizes={PANORAMIC_BG_IMAGE_SIZES}
            eager
            alt=""
            className="absolute inset-0 size-full max-w-none object-cover object-center"
          />
          {!videoFailed ? (
            <video
              className="absolute inset-0 size-full max-w-none object-cover object-center"
              autoPlay
              muted
              loop
              playsInline
              preload="metadata"
              poster={signupCtaBgPicture.img.src}
              onError={() => setVideoFailed(true)}
            >
              <source src={signUpCtaVideoUrl()} type="video/mp4" />
            </video>
          ) : null}
          <div
            className="absolute inset-0 lg:hidden"
            style={{ backgroundImage: SIGN_UP_GRADIENT_MOBILE }}
          />
        </div>

        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 hidden rounded-[8px] lg:block"
          style={{ backgroundImage: SIGN_UP_GRADIENT_DESKTOP }}
        />

        <div
          className={cn(
            "relative z-10 order-1 flex w-full flex-col items-start gap-10 px-6.25 py-12.5",
            "sm:gap-12 lg:absolute lg:inset-0 lg:h-full lg:max-w-[573px] lg:justify-between lg:gap-0 lg:px-15 lg:py-17.5",
          )}
        >
          <div className="flex flex-col gap-[18px]">
            <h2
              className={cn(
                "m-0 max-w-md md:max-w-[544px]",
                marketingSectionHeading,
                "font-jakarta text-white",
              )}
            >
              {content.title}
            </h2>
            <p
              className={cn(
                "m-0 max-w-sm md:max-w-[397px]",
                marketingHeroBody,
                "font-normal text-white lg:text-[18px] lg:leading-6",
              )}
            >
              {content.body}
            </p>
          </div>

          <div className="flex w-full flex-col items-start gap-[26px]">
            <div className="inline-flex items-center gap-3">
              <CircleCheckBig
                aria-hidden
                className="size-4 shrink-0 text-[#727272]"
                strokeWidth={2}
              />
              <p className="m-0 text-xs font-medium leading-normal text-[#727272]">
                {content.badge}
              </p>
            </div>

            <div className="flex w-full flex-col gap-3">
              <form
                onSubmit={onSubmit}
                className="flex w-full flex-col items-stretch gap-2.5 sm:flex-row sm:items-center"
              >
                <label className="sr-only" htmlFor={emailFieldId}>
                  Work email
                </label>
                <Input
                  id={emailFieldId}
                  type="email"
                  name="email"
                  autoComplete="email"
                  inputMode="email"
                  placeholder="Work email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  className="h-[46px] min-h-[46px] flex-1 rounded-[8px] border-[#969696] bg-transparent px-[17px] py-[15px] text-base leading-5 text-white placeholder:text-[#7d7d7d] focus-visible:border-white focus-visible:ring-white/20 md:text-base"
                />
                <MarketingButton
                  type="submit"
                  variant="primary"
                  className="h-[46px] min-h-[46px] w-full shrink-0 rounded-[8px] sm:w-[176px]"
                >
                  {content.primaryLabel}
                </MarketingButton>
              </form>

              <div className="flex w-full items-center gap-2.5">
                <div className="h-px min-w-0 flex-1 bg-[#4e4e4e]" />
                <span className="shrink-0 text-sm leading-normal text-[#4e4e4e]">
                  or
                </span>
                <div className="h-px min-w-0 flex-1 bg-[#4e4e4e]" />
              </div>

              <AuthSocialContinueButtons
                layout="row"
                tone="dark"
                returnPath={signupReturnPath}
                onBeforeStart={persistPlanIntent}
              />
            </div>

            <p className="m-0 text-sm leading-[22px] text-white">
              By signing up, I agree to Tummly&apos;s{" "}
              <Link
                to={LEGAL_ROUTES.terms}
                className="underline underline-offset-2"
              >
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
        </div>
      </div>
    </section>
  )
}
