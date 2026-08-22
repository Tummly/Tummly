import { useState } from "react"

import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Spinner } from "@/components/ui/spinner"
import type { OperatorHomeRecommendationViewModel } from "@/lib/operatorHome/createOperatorHomePageModule"
import {
  HOME_RECOMMENDATION_COPY,
  isHomeRecommendationCampaignType,
  primaryCtaLabelForHomeRecommendation,
} from "@/lib/operatorHome/homeRecommendationPresentation"
import {
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  OPERATOR_HOME_WHITE_CARD_TITLE_CLASS,
  RECOMMENDED_EMPTY_COPY_CLASS,
  RECOMMENDED_HEADER_CLASS,
  RECOMMENDED_INNER_PANEL_CLASS,
  RECOMMENDED_SECTION_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { HomeRecommendation } from "@/types/operatorHome"

type HomeRecommendedNextStepProps = {
  recommendation: OperatorHomeRecommendationViewModel
  locationName: string
  dateRangeLabel: string
  onRetry: () => void
  onPrimaryAction: (recommendation: HomeRecommendation) => void
  onNotNow: () => void
}

function RecommendationMetaLine(props: {
  locationName: string
  dateRangeLabel: string
}) {
  const activityPhrase =
    props.dateRangeLabel === "All time"
      ? "Based on all-time activity"
      : `Based on activity from the ${props.dateRangeLabel.toLowerCase()}`

  return (
    <p className="m-0 text-sm font-normal leading-5 text-op-card-subtitle-color">
      {props.locationName} · {activityPhrase}
    </p>
  )
}

function RecommendationDetailRow(props: {
  label: string
  value: string
}) {
  return (
    <div className="flex flex-wrap items-baseline gap-2 text-sm">
      <span className="font-medium text-op-card-title-color">{props.label}</span>
      <span className="font-normal leading-5 text-op-card-subtitle-color">
        {props.value}
      </span>
    </div>
  )
}

function WhyBullets(props: {
  label: string
  intro?: string
  bullets: string[]
}) {
  return (
    <div className="flex flex-col gap-1.5 text-sm">
      <p className="m-0 font-medium text-op-card-title-color">{props.label}</p>
      <div className="flex flex-col text-op-card-subtitle-color">
        {props.intro != null ? (
          <p className="m-0 leading-5">{props.intro}</p>
        ) : null}
        <ul className="m-0 list-disc space-y-0.5 pl-[21px]">
          {props.bullets.map((bullet) => (
            <li key={bullet} className="leading-5">
              {bullet}
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

function RecommendationSuccessBody(props: {
  recommendation: HomeRecommendation
  locationName: string
  dateRangeLabel: string
  onPrimaryAction: () => void
  onViewAudience: () => void
  onNotNow: () => void
}) {
  const copy = HOME_RECOMMENDATION_COPY
  const isCampaign = isHomeRecommendationCampaignType(
    props.recommendation.type
  )
  const channelLabel =
    props.recommendation.suggestedChannel === "sms"
      ? "SMS"
      : props.recommendation.suggestedChannel === "email"
        ? "Email"
        : null
  const whyBullets = props.recommendation.whyBullets ?? []
  const primaryLabel = primaryCtaLabelForHomeRecommendation(
    props.recommendation
  )

  return (
    <div className={`${RECOMMENDED_INNER_PANEL_CLASS} flex flex-col gap-8`}>
      <div className="flex flex-col gap-[22px]">
        <div className="flex flex-col gap-2">
          <h3 className="m-0 text-base font-semibold leading-normal text-op-card-title-color">
            {props.recommendation.title}
          </h3>
          <RecommendationMetaLine
            locationName={
              props.recommendation.locationName ?? props.locationName
            }
            dateRangeLabel={props.dateRangeLabel}
          />
        </div>

        {props.recommendation.opportunity != null ? (
          <RecommendationDetailRow
            label={copy.opportunityLabel}
            value={props.recommendation.opportunity}
          />
        ) : null}

        {props.recommendation.eligibleAudience != null ? (
          <RecommendationDetailRow
            label={copy.eligibleAudienceLabel}
            value={props.recommendation.eligibleAudience}
          />
        ) : null}

        {whyBullets.length > 0 ? (
          <WhyBullets
            label={isCampaign ? copy.whyLabel : copy.whyLabelHome}
            intro={isCampaign ? copy.whyIntro : undefined}
            bullets={whyBullets}
          />
        ) : null}

        {channelLabel != null ? (
          <RecommendationDetailRow
            label={copy.suggestedChannelLabel}
            value={channelLabel}
          />
        ) : null}

        {props.recommendation.estimatedUsage != null ? (
          <RecommendationDetailRow
            label={copy.estimatedUsageLabel}
            value={props.recommendation.estimatedUsage}
          />
        ) : null}
      </div>

      <div className="flex flex-wrap items-center gap-[18px]">
        <Button
          type="button"
          variant="op-primary"
          className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
          onClick={props.onPrimaryAction}
        >
          {primaryLabel}
        </Button>
        {isCampaign ? (
          <Button
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={props.onViewAudience}
          >
            {copy.viewEligibleAudience}
          </Button>
        ) : null}
        <Button type="button" variant="op-link" onClick={props.onNotNow}>
          {copy.notNow}
        </Button>
      </div>
    </div>
  )
}

function AudienceCountsPanel(props: {
  open: boolean
  onClose: () => void
  recommendation: HomeRecommendation | null
}) {
  const counts = props.recommendation?.echoedCounts
  const copy = HOME_RECOMMENDATION_COPY

  return (
    <Dialog
      open={props.open}
      onOpenChange={(open) => {
        if (!open) {
          props.onClose()
        }
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{copy.viewEligibleAudience}</DialogTitle>
          <DialogDescription>{copy.audienceDisclaimer}</DialogDescription>
        </DialogHeader>
        {counts != null ? (
          <ul className="m-0 flex list-none flex-col gap-2 p-0 text-sm text-op-card-subtitle-color">
            <li>Marketing eligible: {counts.marketingEligible}</li>
            <li>All guests: {counts.allGuests}</li>
            <li>New guests: {counts.newGuests}</li>
            <li>Needs recovery: {counts.needsRecovery}</li>
            <li>Positive feedback: {counts.positiveFeedback}</li>
            <li>Dormant guests: {counts.dormantGuests}</li>
          </ul>
        ) : null}
        <DialogFooter>
          <Button type="button" variant="op-secondary" onClick={props.onClose}>
            {copy.audienceClose}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

/** Home Recommended next step — live card (ticket 05 / Figma 3353:42550). */
export function HomeRecommendedNextStep({
  recommendation,
  locationName,
  dateRangeLabel,
  onRetry,
  onPrimaryAction,
  onNotNow,
}: HomeRecommendedNextStepProps) {
  const copy = HOME_RECOMMENDATION_COPY
  const [showAudiencePanel, setShowAudiencePanel] = useState(false)

  if (recommendation.status === "dismissed") {
    return null
  }

  return (
    <>
      <section
        className={RECOMMENDED_SECTION_CLASS}
        aria-label={copy.title}
      >
        <div className={RECOMMENDED_HEADER_CLASS}>
          <AiIcon size={22} />
          <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
            <h2 className={OPERATOR_HOME_WHITE_CARD_TITLE_CLASS}>
              {copy.title}
            </h2>
            <p className={OPERATOR_HOME_SUBTITLE_CLASS}>{copy.subtitle}</p>
          </div>
        </div>

        {recommendation.status === "loading"
        || recommendation.status === "idle" ? (
          <div
            className={`${RECOMMENDED_INNER_PANEL_CLASS} flex min-h-[120px] items-center justify-center`}
            role="status"
            aria-live="polite"
            aria-label="Loading recommendation"
          >
            <Spinner />
          </div>
        ) : null}

        {recommendation.status === "error" ? (
          <div className={`${RECOMMENDED_INNER_PANEL_CLASS} flex flex-col gap-3`}>
            <p className={RECOMMENDED_EMPTY_COPY_CLASS}>
              {recommendation.errorMessage ?? copy.failCopy}
            </p>
            {recommendation.errorRetryable ? (
              <div>
                <Button
                  type="button"
                  variant="op-secondary"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  onClick={onRetry}
                >
                  {copy.retry}
                </Button>
              </div>
            ) : null}
          </div>
        ) : null}

        {recommendation.status === "ready" && recommendation.isNone ? (
          <div className={RECOMMENDED_INNER_PANEL_CLASS}>
            <p className={RECOMMENDED_EMPTY_COPY_CLASS}>{copy.emptyCopy}</p>
          </div>
        ) : null}

        {recommendation.status === "ready"
        && recommendation.recommendation != null ? (
          <RecommendationSuccessBody
            recommendation={recommendation.recommendation}
            locationName={locationName}
            dateRangeLabel={dateRangeLabel}
            onPrimaryAction={() =>
              onPrimaryAction(recommendation.recommendation!)
            }
            onViewAudience={() => {
              setShowAudiencePanel(true)
            }}
            onNotNow={onNotNow}
          />
        ) : null}
      </section>

      <AudienceCountsPanel
        open={showAudiencePanel}
        onClose={() => {
          setShowAudiencePanel(false)
        }}
        recommendation={recommendation.recommendation}
      />
    </>
  )
}
