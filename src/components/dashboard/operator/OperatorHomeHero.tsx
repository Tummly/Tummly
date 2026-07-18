import { CrownIcon } from "lucide-react"

import tearImg from "@/assets/images/tear-img.png"
import guestLoopLivePhone from "@/assets/operator-home/guest-loop-live-phone.png"
import heroFormAccentDark from "@/assets/svg/hero-form-accent-dark.svg"
import heroFormAccent from "@/assets/svg/hero-form-accent.svg"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type {
  ActivationPeriodBadgeCopy,
  ActivationPeriodBadgeTone,
} from "@/lib/operatorHome/activationPeriod"
import { cn } from "@/lib/utils"

type OperatorHomeHeroProps = {
  activationPeriodBadge: ActivationPeriodBadgeCopy | null
  canPreviewGuestForm: boolean
  canCopySmartGuestLink: boolean
  previewBusy?: boolean
  onPreviewGuestForm?: () => void
  onCopySmartGuestLink?: () => void
}

const ACTIVATION_PERIOD_BADGE_TONE_CLASS: Record<
  ActivationPeriodBadgeTone,
  string
> = {
  default: "bg-black/5 text-foreground dark:bg-[#202020] dark:text-white",
  warning:
    "bg-[#f3eae4] text-foreground dark:bg-[#f3eae4]/25 dark:text-[#f4f4f4]",
  urgent:
    "bg-[#f9dfdf] text-foreground dark:bg-[#f9dfdf]/25 dark:text-[#f4f4f4]",
}

const primaryButtonClassName =
  "h-auto min-h-0 rounded-[2px] border-transparent bg-primary px-4 py-2.5 text-sm font-medium leading-5 text-primary-foreground hover:bg-primary/90 disabled:opacity-50"

const secondaryButtonClassName =
  "h-auto min-h-0 rounded-[2px] border-transparent bg-[#e8e8e8] px-4 py-2.5 text-sm font-medium leading-5 text-foreground hover:bg-[#dedede] disabled:opacity-50 dark:bg-[#333] dark:text-white dark:hover:bg-[#3d3d3d]"

/** Figma “Your Guest Loop is live” hero — Preview + Copy Smart Guest Link. */
export function OperatorHomeHero({
  activationPeriodBadge,
  canPreviewGuestForm,
  canCopySmartGuestLink,
  previewBusy = false,
  onPreviewGuestForm,
  onCopySmartGuestLink,
}: OperatorHomeHeroProps) {
  return (
    <section className="relative pt-4">
      <div
        className={cn(
          "relative overflow-hidden rounded-md border border-[#e5e5e5] bg-white",
          "dark:border-[#262626] dark:bg-[#171717]"
        )}
      >
        {/*
          Layer order (Figma): art → tear → phone + copy.
          Art is unrotated accent SVG (652×359 in a 1536 card), phone is
          352px wide with its right edge 241px in from the card right, top
          ~14% down. Tear sits between them (z-10).
        */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-y-0 right-0 z-0 hidden w-[min(48%,622px)] overflow-hidden lg:block"
        >
          {/* Figma ships a separate dark variant (#171717/#262626 fills) */}
          <div className="absolute inset-y-0 right-[-5%] w-[105%]">
            <img src={heroFormAccent} alt="" className="size-full dark:hidden" />
            <img
              src={heroFormAccentDark}
              alt=""
              className="hidden size-full dark:block"
            />
          </div>
          <div
            className={cn(
              "absolute inset-0",
              "bg-[linear-gradient(4deg,rgb(255,255,255)_17%,rgba(255,255,255,0.2)_66%)]",
              "dark:bg-[linear-gradient(4deg,rgb(23,23,23)_17%,rgba(23,23,23,0.2)_66%)]"
            )}
          />
          <div
            className={cn(
              "absolute inset-y-0 left-0 w-[45%]",
              "bg-[linear-gradient(90deg,rgb(255,255,255)_0%,rgba(255,255,255,0)_100%)]",
              "dark:bg-[linear-gradient(90deg,rgb(23,23,23)_0%,rgba(23,23,23,0)_100%)]"
            )}
          />
        </div>

        {/*
          tear-img (3072x1313): green band in rows 1080-1284. Figma draws the
          band 105px tall then shifts it up so only ~42px (mostly the jagged
          edge) stays inside the card. Flip so the edge faces down; size so
          the band is 105px and offset past the solid green.
        */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 z-10 h-[34px] overflow-hidden sm:h-[42px]"
        >
          <img
            src={tearImg}
            alt=""
            className="absolute top-[-62px] left-0 h-[541px] w-full max-w-none -scale-y-100 object-fill sm:top-[-77px] sm:h-[675px]"
          />
        </div>

        <div className="relative z-20 flex flex-col items-stretch px-6 py-10 sm:px-[34px] sm:py-14 lg:flex-row lg:items-center lg:px-[55px] lg:py-[71px]">
          <div className="relative flex min-w-0 flex-1 flex-col items-start gap-[26px]">
            {activationPeriodBadge ? (
              <Badge
                variant="soft"
                className={cn(
                  "h-auto max-w-full flex-wrap justify-start gap-3 rounded px-3 py-2.5 font-medium whitespace-normal",
                  ACTIVATION_PERIOD_BADGE_TONE_CLASS[activationPeriodBadge.tone]
                )}
                aria-label={`${activationPeriodBadge.remaining} in your free trial. Ends ${activationPeriodBadge.endsOn}`}
              >
                <span className="inline-flex items-start gap-2">
                  <CrownIcon className="size-4 shrink-0" aria-hidden />
                  <span className="leading-[15px]">
                    {activationPeriodBadge.remaining} in your free trial · Ends{" "}
                    {activationPeriodBadge.endsOn}
                  </span>
                </span>
                <span className="underline decoration-solid underline-offset-2">
                  Choose a plan
                </span>
              </Badge>
            ) : null}

            <div className="flex flex-col gap-4">
              <h2 className="text-[32px] leading-10 font-bold text-foreground">
                Your Guest Loop is live
              </h2>
              <p className="max-w-[555px] text-sm leading-6 text-foreground">
                Your guest feedback form is ready to use. Complete the setup
                actions below to brand the guest experience, review the form and
                start collecting responses.
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <Button
                type="button"
                className={primaryButtonClassName}
                disabled={!canPreviewGuestForm || previewBusy}
                aria-disabled={!canPreviewGuestForm || previewBusy}
                onClick={onPreviewGuestForm}
              >
                Preview guest form
              </Button>
              <Button
                type="button"
                variant="secondary"
                className={secondaryButtonClassName}
                disabled={!canCopySmartGuestLink}
                aria-disabled={!canCopySmartGuestLink}
                onClick={onCopySmartGuestLink}
              >
                Copy Smart Guest Link
              </Button>
            </div>
          </div>

          {/* Phone above tear; art shows on both sides; bottom cropped by card */}
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 right-0 hidden w-[min(48%,622px)] overflow-hidden lg:block"
          >
            <div className="absolute top-[14%] right-[38.7%] bottom-0 w-[56.6%] overflow-hidden">
              <img
                src={guestLoopLivePhone}
                alt=""
                className="absolute top-0 left-0 w-full max-w-none"
              />
              {/* Bottom fade over the phone — dark mode only (washes it out in light) */}
              <div className="absolute inset-x-0 bottom-0 hidden h-[18%] bg-[linear-gradient(180deg,rgba(23,23,23,0)_0%,rgb(23,23,23)_95%)] dark:block" />
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
