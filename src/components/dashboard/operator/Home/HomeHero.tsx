import { CrownIcon } from "lucide-react"
import { Link } from "react-router-dom"

import tearImg from "@/assets/svg/tear-v2.svg"
import heroFormAccentDark from "@/assets/svg/hero-form-accent-dark.svg"
import heroFormAccent from "@/assets/svg/hero-form-accent.svg"
import { HomeGuestFormPhone } from "@/components/dashboard/operator/Home/HomeGuestFormPhone"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type {
  ActivationPeriodBadgePresentation,
  ActivationPeriodBadgeTone,
} from "@/lib/operatorHome/activationPeriod"
import {
  formatActivationPeriodBadgeAriaLabel,
  formatActivationPeriodBadgeFullVisibleText,
  OPERATOR_HOME_HERO_ART_EDGE_FADE_CLASS,
  OPERATOR_HOME_HERO_ART_FADE_CLASS,
  OPERATOR_HOME_HERO_BADGE_CLASS,
  OPERATOR_HOME_HERO_CARD_CLASS,
  OPERATOR_HOME_HERO_CTA_ROW_CLASS,
  OPERATOR_HOME_HERO_INNER_CLASS,
  OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS,
  OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS,
  OPERATOR_HOME_HERO_SUBTITLE_CLASS,
  OPERATOR_HOME_HERO_TITLE_CLASS,
} from "@/lib/operatorHome/heroPresentation"
import { cn } from "@/lib/utils"

type HomeHeroProps = {
  activationPeriodBadge: ActivationPeriodBadgePresentation | null
  canPreviewGuestForm: boolean
  canCopySmartGuestLink: boolean
  previewBusy?: boolean
  guestFormPreviewLocationName?: string
  guestFormPreviewAddress?: string
  onPreviewGuestForm?: () => void
  onCopySmartGuestLink?: () => void
}

/** Warning/urgent only — default tone keeps `variant="soft"` chip tokens. */
const ACTIVATION_PERIOD_BADGE_TONE_CLASS: Record<
  Exclude<ActivationPeriodBadgeTone, "default">,
  string
> = {
  warning:
    "bg-[#f3eae4] text-foreground dark:bg-[#f3eae4]/25 dark:text-[#f4f4f4]",
  urgent:
    "bg-[#f9dfdf] text-foreground dark:bg-[#f9dfdf]/25 dark:text-[#f4f4f4]",
}

/** Figma “Your Guest Loop is live” hero — Preview + Copy Smart Guest Link. */
export function HomeHero({
  activationPeriodBadge,
  canPreviewGuestForm,
  canCopySmartGuestLink,
  previewBusy = false,
  guestFormPreviewLocationName = "",
  guestFormPreviewAddress = "",
  onPreviewGuestForm,
  onCopySmartGuestLink,
}: HomeHeroProps) {
  return (
    <section className="relative">
      <div className={OPERATOR_HOME_HERO_CARD_CLASS}>
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
          <div className={OPERATOR_HOME_HERO_ART_FADE_CLASS} />
          <div className={OPERATOR_HOME_HERO_ART_EDGE_FADE_CLASS} />
        </div>

        {/*
          tear-v2 is jagged on both edges, so anchor the band to the bottom of
          the crop: the top jag stays outside the card and only the lower jag
          shows. The asset stretches freely, so the band keeps one height at
          every card width.
        */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 z-10 h-[26px] overflow-hidden sm:h-[32px]"
        >
          <img
            src={tearImg}
            alt=""
            className="absolute inset-x-0 bottom-0 h-[44px] w-full max-w-none sm:h-[54px]"
          />
        </div>

        <div className={OPERATOR_HOME_HERO_INNER_CLASS}>
          <div className="relative flex min-w-0 flex-1 flex-col items-start gap-[26px] lg:max-w-[52%]">
            {activationPeriodBadge ? (
              <Badge
                variant="soft"
                className={cn(
                  OPERATOR_HOME_HERO_BADGE_CLASS,
                  activationPeriodBadge.tone === "default"
                    ? null
                    : ACTIVATION_PERIOD_BADGE_TONE_CLASS[
                        activationPeriodBadge.tone
                      ]
                )}
                aria-label={formatActivationPeriodBadgeAriaLabel(
                  activationPeriodBadge
                )}
              >
                <span className="inline-flex items-center gap-2">
                  <CrownIcon className="size-4 shrink-0" aria-hidden />
                  <span className="leading-[15px] md:hidden">
                    {activationPeriodBadge.remaining}
                  </span>
                  <span className="hidden leading-[15px] md:inline">
                    {formatActivationPeriodBadgeFullVisibleText(
                      activationPeriodBadge
                    )}
                  </span>
                </span>
                {activationPeriodBadge.choosePlanHref != null ? (
                  <Link
                    to={activationPeriodBadge.choosePlanHref}
                    className="underline decoration-solid underline-offset-2"
                  >
                    Choose a plan
                  </Link>
                ) : null}
              </Badge>
            ) : null}

            <div className="flex flex-col gap-4">
              <h1 className={OPERATOR_HOME_HERO_TITLE_CLASS}>
                Your Guest Loop is live
              </h1>
              <p className={OPERATOR_HOME_HERO_SUBTITLE_CLASS}>
                Your guest feedback form is ready to use. Complete the setup
                actions below to brand the guest experience, review the form and
                start collecting responses.
              </p>
            </div>

            <div className={OPERATOR_HOME_HERO_CTA_ROW_CLASS}>
              <Button
                type="button"
                className={OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS}
                variant="op-primary"
                disabled={!canPreviewGuestForm || previewBusy}
                aria-disabled={!canPreviewGuestForm || previewBusy}
                onClick={onPreviewGuestForm}
              >
                Preview guest form
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                className={OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS}
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
            <HomeGuestFormPhone
              locationName={guestFormPreviewLocationName}
              address={guestFormPreviewAddress}
            />
          </div>
        </div>
      </div>
    </section>
  )
}
