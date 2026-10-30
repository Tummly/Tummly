import { Link } from "react-router-dom"

import aiVisual from "@/assets/images/marketing/how-it-works/ai-visual.webp"
import campaignsVisual from "@/assets/images/marketing/how-it-works/campaigns-visual.webp"
import feedbackVisual from "@/assets/images/marketing/how-it-works/feedback-visual.webp"
import guestsVisual from "@/assets/images/marketing/how-it-works/guests-visual.webp"
import reportsVisual from "@/assets/images/marketing/how-it-works/reports-visual.webp"
import aiExploreQuestionIcon from "@/assets/svg/marketing/how-it-works/ai-explore-question.svg"
import aiPrepareDraftIcon from "@/assets/svg/marketing/how-it-works/ai-prepare-draft.svg"
import aiSummariseCommentsIcon from "@/assets/svg/marketing/how-it-works/ai-summarise-comments.svg"
import campaignsChooseInvitationIcon from "@/assets/svg/marketing/how-it-works/campaigns-choose-invitation.svg"
import campaignsReviewAudienceIcon from "@/assets/svg/marketing/how-it-works/campaigns-review-audience.svg"
import campaignsSeeResponseIcon from "@/assets/svg/marketing/how-it-works/campaigns-see-response.svg"
import feedbackFocusConcernsIcon from "@/assets/svg/marketing/how-it-works/feedback-focus-concerns.svg"
import feedbackLearnWithTeamIcon from "@/assets/svg/marketing/how-it-works/feedback-learn-with-team.svg"
import feedbackReadOriginalIcon from "@/assets/svg/marketing/how-it-works/feedback-read-original.svg"
import guestsCustomerChoicesIcon from "@/assets/svg/marketing/how-it-works/guests-customer-choices.svg"
import guestsPreviousCommentsIcon from "@/assets/svg/marketing/how-it-works/guests-previous-comments.svg"
import guestsUnderstandExperienceIcon from "@/assets/svg/marketing/how-it-works/guests-understand-experience.svg"
import reportsPlanDiscussionIcon from "@/assets/svg/marketing/how-it-works/reports-plan-discussion.svg"
import reportsReviewEngagementIcon from "@/assets/svg/marketing/how-it-works/reports-review-engagement.svg"
import reportsReviewFeedbackIcon from "@/assets/svg/marketing/how-it-works/reports-review-feedback.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  ProductSplitFeatureSection,
  type FeaturePointIconImg,
} from "@/components/marketing/product/ProductSplitFeatureSection"
import {
  HOW_IT_WORKS_FEATURE_GET_STARTED_LABEL,
  HOW_IT_WORKS_FEATURE_VISUAL_ALT,
  HOW_IT_WORKS_FEATURES,
  type HowItWorksFeature,
  type HowItWorksFeatureId,
} from "@/content/marketing/howItWorksFeatures"
import { HOW_IT_WORKS_STEPS } from "@/content/marketing/howItWorksPage"
import { buildSignupPath, saveSignupPlanIntent } from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

/** Figma `tummly-icon-*` glyphs: 24px, top-aligned with the row title. */
function icon(src: string): FeaturePointIconImg {
  return { src, width: 24, height: 24, className: "mt-0" }
}

const POINT_ICONS: Record<
  HowItWorksFeatureId,
  Record<string, FeaturePointIconImg>
> = {
  feedback: {
    "read-original": icon(feedbackReadOriginalIcon),
    "focus-concerns": icon(feedbackFocusConcernsIcon),
    "learn-with-team": icon(feedbackLearnWithTeamIcon),
  },
  guests: {
    "previous-comments": icon(guestsPreviousCommentsIcon),
    "understand-experience": icon(guestsUnderstandExperienceIcon),
    "customer-choices": icon(guestsCustomerChoicesIcon),
  },
  campaigns: {
    "choose-invitation": icon(campaignsChooseInvitationIcon),
    "review-audience": icon(campaignsReviewAudienceIcon),
    "see-response": icon(campaignsSeeResponseIcon),
  },
  reports: {
    "review-feedback": icon(reportsReviewFeedbackIcon),
    "review-engagement": icon(reportsReviewEngagementIcon),
    "plan-discussion": icon(reportsPlanDiscussionIcon),
  },
  "ai-assistant": {
    "summarise-comments": icon(aiSummariseCommentsIcon),
    "explore-question": icon(aiExploreQuestionIcon),
    "prepare-draft": icon(aiPrepareDraftIcon),
  },
}

/** Figma `Frame 5668` product visuals, 720×650 plate (feedback, guests, AI). */
const VISUALS: Record<
  HowItWorksFeatureId,
  { src: string; className?: string }
> = {
  feedback: { src: feedbackVisual },
  guests: { src: guestsVisual },
  /** Figma node `6871:35921` exported at 2×: green texture + campaign card. */
  campaigns: { src: campaignsVisual },
  /** Weekly brief is a 720×720 image, 12px above centre in the 720×650 plate. */
  reports: {
    src: reportsVisual,
    className: "top-[-1.86%] h-[110.77%] object-fill",
  },
  "ai-assistant": { src: aiVisual },
}

/** Figma heading text-box widths keep the Figma line breaks. */
const TITLE_MAX_WIDTH: Record<HowItWorksFeatureId, string> = {
  feedback: "max-w-[458px]",
  guests: "max-w-[435px]",
  campaigns: "max-w-[368px]",
  reports: "max-w-[435px]",
  "ai-assistant": "max-w-[541px]",
}

const PLAN_INTENT = HOW_IT_WORKS_STEPS.planIntent

function FeatureRow({ feature }: { feature: HowItWorksFeature }) {
  const { secondaryCta } = feature
  const visual = VISUALS[feature.id]

  return (
    <ProductSplitFeatureSection
      headingId={`how-it-works-feature-${feature.id}-heading`}
      eyebrow={feature.eyebrow}
      title={feature.title}
      body={feature.body}
      points={feature.points}
      pointIconImgs={POINT_ICONS[feature.id]}
      mediaFirst={feature.mediaFirst}
      balanceToMedia
      fluidBalance
      eyebrowClassName="leading-4 text-[#4a4a4a]"
      titleClassName={cn(
        "text-[28px] leading-[normal] sm:text-[32px] lg:text-[36px] lg:leading-[normal]",
        TITLE_MAX_WIDTH[feature.id],
      )}
      bodyClassName="max-w-[635px]"
      dividerClassName="border-[#c6c6c6]"
      footerClassName={secondaryCta != null ? "gap-9" : "gap-[26px]"}
      mediaClassName="aspect-[720/650] bg-[#eceae6]"
      media={
        <img
          src={visual.src}
          alt={HOW_IT_WORKS_FEATURE_VISUAL_ALT[feature.id]}
          width={1440}
          height={1300}
          className={cn(
            "absolute inset-0 size-full max-w-none object-cover",
            visual.className,
          )}
          decoding="async"
          loading="lazy"
        />
      }
      actions={
        <div className="flex flex-wrap items-center gap-4">
          <MarketingButton asChild variant="primary">
            <Link
              to={buildSignupPath(PLAN_INTENT)}
              onClick={() => {
                saveSignupPlanIntent(PLAN_INTENT)
              }}
            >
              {HOW_IT_WORKS_FEATURE_GET_STARTED_LABEL}
            </Link>
          </MarketingButton>
          {secondaryCta != null ? (
            <MarketingButton
              asChild
              variant="secondary"
              className="sm:w-[164px]"
            >
              <Link to={secondaryCta.to}>{secondaryCta.label}</Link>
            </MarketingButton>
          ) : null}
        </div>
      }
    />
  )
}

/**
 * How it works feature rows — five alternating bands, copy beside a framed
 * product visual (Figma `6870:35827` to `6871:35965`). Built on the shared
 * Product split section (`balanceToMedia` + `fluidBalance`).
 */
export function HowItWorksFeatures() {
  return (
    <>
      {HOW_IT_WORKS_FEATURES.map((feature) => (
        <FeatureRow key={feature.id} feature={feature} />
      ))}
    </>
  )
}
