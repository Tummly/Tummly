import { CrownIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type {
  ActivationPeriodBadgePresentation,
  ActivationPeriodBadgeTone,
} from "@/lib/operatorHome/activationPeriod"
import {
  formatActivationPeriodBadgeAriaLabel,
  formatActivationPeriodBadgeFullVisibleText,
  OPERATOR_HOME_FREE_HERO_COPY,
  OPERATOR_HOME_HERO_BADGE_CLASS,
  OPERATOR_HOME_HERO_COPY_CLASS,
  OPERATOR_HOME_HERO_CTA_ROW_CLASS,
  OPERATOR_HOME_HERO_INNER_CLASS,
  OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS,
  OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS,
  OPERATOR_HOME_HERO_SUBTITLE_CLASS,
  OPERATOR_HOME_HERO_TITLE_CLASS,
  type OperatorHomeHeroMode,
} from "@/lib/operatorHome/heroPresentation"
import { cn } from "@/lib/utils"

type HomeHeroProps = {
  mode?: OperatorHomeHeroMode
  activationPeriodBadge: ActivationPeriodBadgePresentation | null
  canPreviewGuestForm: boolean
  canCopySmartGuestLink: boolean
  previewBusy?: boolean
  choosePlanHref?: string | null
  onPreviewGuestForm?: () => void
  onCopySmartGuestLink?: () => void
  onChoosePlan?: () => void
  onStartPilot?: () => void
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

/** Figma hero — live Guest Loop CTAs, or Free workspace CTAs (`5043:10201`). */
export function HomeHero({
  mode = "live",
  activationPeriodBadge,
  canPreviewGuestForm,
  canCopySmartGuestLink,
  previewBusy = false,
  choosePlanHref = null,
  onPreviewGuestForm,
  onCopySmartGuestLink,
  onChoosePlan,
  onStartPilot,
}: HomeHeroProps) {
  if (mode === "free") {
    const copy = OPERATOR_HOME_FREE_HERO_COPY
    return (
      <section className={OPERATOR_HOME_HERO_INNER_CLASS}>
        <div className={OPERATOR_HOME_HERO_COPY_CLASS}>
          <div className="flex flex-col gap-4">
            <h1 className={OPERATOR_HOME_HERO_TITLE_CLASS}>{copy.title}</h1>
            <p className={OPERATOR_HOME_HERO_SUBTITLE_CLASS}>{copy.subtitle}</p>
          </div>
        </div>

        <div className={OPERATOR_HOME_HERO_CTA_ROW_CLASS}>
          {choosePlanHref != null ? (
            <Button
              asChild
              className={OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS}
              variant="op-primary"
            >
              <Link to={choosePlanHref}>
                <CrownIcon className="size-4" aria-hidden />
                {copy.choosePlanCta}
              </Link>
            </Button>
          ) : (
            <Button
              type="button"
              className={OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS}
              variant="op-primary"
              onClick={onChoosePlan}
            >
              <CrownIcon className="size-4" aria-hidden />
              {copy.choosePlanCta}
            </Button>
          )}
          <Button
            type="button"
            variant="op-secondary"
            className={OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS}
            onClick={onStartPilot}
          >
            {copy.startPilotCta}
          </Button>
        </div>
      </section>
    )
  }

  return (
    <section className={OPERATOR_HOME_HERO_INNER_CLASS}>
      <div className={OPERATOR_HOME_HERO_COPY_CLASS}>
        {activationPeriodBadge ? (
          <Badge
            variant="soft"
            className={cn(
              OPERATOR_HOME_HERO_BADGE_CLASS,
              activationPeriodBadge.tone === "default"
                ? null
                : ACTIVATION_PERIOD_BADGE_TONE_CLASS[activationPeriodBadge.tone]
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
            Your guest feedback form is ready to use. Complete the setup actions
            below to brand the guest experience, review the form and start
            collecting responses.
          </p>
        </div>
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
    </section>
  )
}
