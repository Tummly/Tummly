import {
  useEffect,
  useId,
  useRef,
  useState,
  type CSSProperties,
  type FormEvent,
} from "react"
import { CircleCheckBig } from "lucide-react"
import { useNavigate } from "react-router-dom"

import signupEditorialBg from "@/assets/images/marketing/about/signup-cta-bg.jpg"
import { signupCtaBgPicture } from "@/assets/marketing-images"
import { AuthSocialContinueButtons } from "@/components/auth/AuthSocialContinueButtons"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingSignupLegalNote } from "@/components/marketing/MarketingSignupLegalNote"
import OptimizedImage from "@/components/media/OptimizedImage"
import { Input } from "@/components/ui/input"
import { PANORAMIC_BG_IMAGE_SIZES } from "@/lib/imagePresets"
import { signUpCtaVideoUrl } from "@/lib/marketingMedia"
import {
  buildSignupPath,
  clearSignupPlanIntent,
  saveSignupPlanIntent,
  type SignupPlanIntent,
} from "@/lib/signupPlanIntent"
import { marketingHeroBody } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/** Figma Sign up Frame 270 title — Jakarta Medium 46 / leading normal (~58). */
const SIGN_UP_TITLE_MEDIA =
  "font-jakarta text-[34px] font-medium leading-none text-white lg:text-[46px] lg:leading-[58px]"

/** Figma About Sign up `5661:24424` title — Jakarta Medium 46 / lh 1, #141414. */
const SIGN_UP_TITLE_EDITORIAL =
  "font-jakarta text-[34px] font-medium leading-none text-[#141414] lg:text-[46px] lg:leading-none"

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
  /** Trust line under the form. Omit for editorial About CTA. */
  badge?: string
  /** When set, primary CTA and OAuth open signup with this plan intent. */
  planIntent?: SignupPlanIntent | null
  /**
   * Line under the form. `agree` (default): "By signing up, I agree to…".
   * `read`: Figma Sign up Type=Default `6788:42946` ("Read our Terms of Service
   * and Privacy Notice…") with its 163px primary button.
   */
  legalNote?: "agree" | "read"
}

/** `media` = dark video CTA; `editorial` = light CTA on marble / photo (About, For Groups). */
export type MarketingSignUpCtaVariant = "media" | "editorial"

type MarketingSignUpCtaProps = {
  content: MarketingSignUpCtaContent
  variant?: MarketingSignUpCtaVariant
  /**
   * Editorial background image. Defaults to About green marble.
   * For Groups uses neon lime marble (`6405:36178`).
   */
  editorialBackgroundSrc?: string
  editorialBackgroundSrcSet?: string
  editorialBackgroundWidth?: number
  editorialBackgroundHeight?: number
  /** Desktop aspect for editorial plate. Defaults to About `1668/525`. */
  editorialAspectClass?: string
  /** Merged onto the editorial background `<img>` (e.g. crop focus on mobile). */
  editorialBackgroundClassName?: string
  /** Merged onto the title. QR materials `6942:25257` wraps at 536px. */
  titleClassName?: string
  /** Merged onto the body. QR materials `6942:25257` wraps at 403px. */
  bodyClassName?: string
}

/**
 * Shared marketing bottom CTA.
 * - `media` — Figma `5661:14510` / `4151:3811` (dark + video)
 * - `editorial` — light type on marble (About `5661:24424`, For Groups `6405:36178`)
 *   or a heat-map plate (QR materials `6942:25257`, with `legalNote: "read"`)
 */
export function MarketingSignUpCta({
  content,
  variant = "media",
  editorialBackgroundSrc = signupEditorialBg,
  editorialBackgroundSrcSet,
  editorialBackgroundWidth = 1920,
  editorialBackgroundHeight = 800,
  editorialAspectClass = "lg:aspect-[1668/525]",
  editorialBackgroundClassName,
  titleClassName,
  bodyClassName,
}: MarketingSignUpCtaProps) {
  const navigate = useNavigate()
  const emailFieldId = useId()
  const mediaRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const [email, setEmail] = useState("")
  const [videoFailed, setVideoFailed] = useState(false)
  const [videoInView, setVideoInView] = useState(false)
  const isEditorial = variant === "editorial"

  useEffect(() => {
    if (isEditorial) {
      return
    }

    const root = mediaRef.current
    if (root == null || typeof IntersectionObserver === "undefined") {
      setVideoInView(true)
      return
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        setVideoInView(entry?.isIntersecting === true)
      },
      {
        root: null,
        rootMargin: "0px",
        threshold: 0.25,
      },
    )

    observer.observe(root)
    return () => observer.disconnect()
  }, [isEditorial])

  useEffect(() => {
    if (isEditorial) {
      return
    }

    const el = videoRef.current
    if (el == null || videoFailed) {
      return
    }

    if (videoInView) {
      void el.play().catch(() => {
        /* Autoplay can still be blocked. */
      })
    } else {
      el.pause()
    }
  }, [isEditorial, videoFailed, videoInView])

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

  const badge = content.badge
  const isReadLegalNote = content.legalNote === "read"

  return (
    <section className="w-full pt-5">
      {/*
        Media Figma `5661:14510` = 1668×615.
        Editorial About Figma `5661:24424` = 1668×525 (no trust badge).
      */}
      <div
        className={cn(
          "relative flex w-full flex-col overflow-hidden rounded-[8px]",
          isEditorial
            ? cn("bg-[#eceae6] lg:block", editorialAspectClass)
            : cn(
                "bg-[#141414] lg:block",
                // Home Figma `6788:42946` is a FIXED 625px frame (other pages: 1668×615).
                isReadLegalNote ? "lg:h-[625px]" : "lg:aspect-[1668/615]",
              ),
        )}
      >
        {isEditorial ? (
          <img
            src={editorialBackgroundSrc}
            srcSet={editorialBackgroundSrcSet}
            sizes="(min-width: 1024px) 1668px, 100vw"
            alt=""
            width={editorialBackgroundWidth}
            height={editorialBackgroundHeight}
            className={cn(
              "pointer-events-none absolute inset-0 size-full max-w-none object-cover object-center",
              editorialBackgroundClassName,
            )}
            decoding="async"
            aria-hidden
          />
        ) : (
          <>
            <div
              ref={mediaRef}
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
                  ref={videoRef}
                  className="absolute inset-0 size-full max-w-none object-cover object-center"
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
          </>
        )}

        {/* Outer: 60px inset (`lg:p-15`). Inner column: 573px content width excluding inset. */}
        <div
          className={cn(
            "relative z-10 order-1 flex w-full flex-col items-start px-6.25 py-12.5",
            "lg:absolute lg:inset-0 lg:h-full lg:p-15",
          )}
        >
          <div
            className={cn(
              "flex w-full flex-col items-start gap-10 sm:gap-12",
              "lg:h-full lg:justify-between lg:gap-0",
              isEditorial ? "lg:w-[573px] lg:max-w-[573px]" : "lg:max-w-2xl",
            )}
          >
            <div className="flex flex-col gap-[18px]">
              <h2
                className={cn(
                  "m-0 max-w-md",
                  // Figma `6788:42946` title box is 544px; Jakarta renders slightly wider here.
                  isReadLegalNote ? "md:max-w-[580px]" : "md:max-w-[544px]",
                  isEditorial ? SIGN_UP_TITLE_EDITORIAL : SIGN_UP_TITLE_MEDIA,
                  // Figma `6942:25257` title uses line-height normal (~58) like `6788:42946`.
                  isEditorial && isReadLegalNote && "lg:leading-[58px]",
                  titleClassName,
                )}
              >
                {content.title}
              </h2>
              <p
                className={cn(
                  "m-0 max-w-sm",
                  isReadLegalNote ? "md:max-w-[544px]" : "md:max-w-[397px]",
                  marketingHeroBody,
                  "font-normal lg:text-[18px] lg:leading-6",
                  isEditorial ? "text-[#141414]" : "text-white",
                  bodyClassName,
                )}
              >
                {content.body}
              </p>
            </div>

            <div className="flex w-full flex-col items-start gap-[26px]">
              {badge != null && badge !== "" ? (
                <div className="inline-flex items-center gap-3">
                  <CircleCheckBig
                    aria-hidden
                    className="size-4 shrink-0 text-[#727272]"
                    strokeWidth={2}
                  />
                  <p className="m-0 text-xs font-inter font-medium leading-[normal] text-[#727272]">
                    {badge}
                  </p>
                </div>
              ) : null}

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
                    className={cn(
                      "h-[46px] min-h-[46px] flex-1 rounded-[8px] bg-transparent px-[17px] py-[15px] text-base leading-5 font-inter md:text-base",
                      isEditorial
                        ? "border-[#5f5f5f] text-[#141414] placeholder:text-[#141414] focus-visible:border-[#141414] focus-visible:ring-[#141414]/20"
                        : "border-[#969696] text-white placeholder:text-[#7d7d7d] focus-visible:border-white focus-visible:ring-white/20",
                    )}
                  />
                  <MarketingButton
                    type="submit"
                    variant={isEditorial ? "dark" : "primary"}
                    className={cn(
                      "h-[46px] min-h-[46px] w-full shrink-0 rounded-[8px]",
                      isEditorial || isReadLegalNote
                        ? "sm:w-[163px]"
                        : "sm:w-[176px]",
                    )}
                  >
                    {content.primaryLabel}
                  </MarketingButton>
                </form>

                <div className="flex w-full items-center gap-2.5">
                  <div className="h-px min-w-0 flex-1 bg-[#4e4e4e]" />
                  <span className="shrink-0 text-sm leading-[normal] text-[#4e4e4e]">
                    or
                  </span>
                  <div className="h-px min-w-0 flex-1 bg-[#4e4e4e]" />
                </div>

                <AuthSocialContinueButtons
                  layout="row"
                  tone={isEditorial ? "light" : "dark"}
                  returnPath={signupReturnPath}
                  onBeforeStart={persistPlanIntent}
                  className={
                    isEditorial
                      ? cn(
                          "h-[46px] min-h-[46px] gap-3 rounded-[8px] border-[#4e4e4e] hover:text-[#141414]",
                          // Figma `6788:42946`: solid white buttons.
                          isReadLegalNote
                            ? "bg-white hover:bg-white/90"
                            : "bg-[rgba(250,250,250,0.79)] hover:bg-[rgba(250,250,250,0.92)]",
                        )
                      : undefined
                  }
                />
              </div>

              {isReadLegalNote ? (
                <MarketingSignupLegalNote
                  className={cn(
                    "max-w-[382px] leading-4",
                    isEditorial ? "text-[#141414]" : "text-[#7c7c7c]",
                  )}
                />
              ) : (
                <MarketingSignupLegalNote
                  variant="agree"
                  className={
                    isEditorial
                      ? "leading-4 text-[#141414]"
                      : "text-sm leading-[22px] text-white"
                  }
                />
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
