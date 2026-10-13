import { Link } from "react-router-dom"

import aiAssistantGlyph from "@/assets/svg/marketing/ai-assistant-glyph.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  AI_ASSISTANT_PLANS,
  AI_ASSISTANT_SECTION_BODY,
  AI_ASSISTANT_SECTION_HEADING,
  AI_ASSISTANT_SECTION_INSET,
  AI_ASSISTANT_SECTION_Y,
  type AiAssistantPlanStripCard,
} from "@/content/marketing/aiAssistantPage"
import { cn } from "@/lib/utils"

export type MarketingPlansStripContent = {
  eyebrow: string
  title: string
  /** Omit or leave empty to hide the body (Home Pricing). */
  body?: string
  compareLabel: string
  compareTo: string
  cards: readonly AiAssistantPlanStripCard[]
}

type AiAssistantPlansProps = {
  /** Defaults to AI Assistant plans copy (`5661:17526`). */
  content?: MarketingPlansStripContent
  /** Show per-plan AI allowance row. Default true. */
  showAiAllowance?: boolean
  /** Section surface. Default `bg-[#f0f0f0]`. */
  surfaceClassName?: string
  /**
   * Compact header metrics for Home Pricing (`4902:19866`):
   * 14px eyebrow, 36/48 title, no body.
   */
  compactHeader?: boolean
}

function PlanCard({
  card,
  showDivider,
  showAiAllowance,
}: {
  card: AiAssistantPlanStripCard
  /** Right divider between adjacent non-highlighted cards (skip before Growth). */
  showDivider: boolean
  showAiAllowance: boolean
}) {
  return (
    <div
      className={cn(
        "flex min-w-[280px] flex-1 flex-col gap-10 overflow-hidden py-6 lg:min-w-0",
        card.isHighlighted
          ? "rounded-[6px] border border-[#14a74a] bg-[rgba(20,167,74,0.05)] px-[26px]"
          : cn("border-[#cacaca]", showDivider && "pr-[26px] lg:border-r"),
      )}
    >
      <div className="flex flex-col gap-3">
        <h3 className="m-0 font-jakarta text-[30px] font-medium leading-none text-[#141414]">
          {card.name}
        </h3>
        <p className="m-0 max-w-[346px] text-sm font-normal leading-[19px] text-[#141414]">
          {card.description}
        </p>
      </div>

      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-2">
          <p className="m-0 text-[#141414]">
            <span className="text-[36px] font-normal leading-none">
              {card.priceAmount}
            </span>
            <span className="text-sm font-normal leading-none">
              {card.priceSuffix}
            </span>
          </p>
          <p className="m-0 text-sm font-normal leading-normal text-[#7c7c7c]">
            {card.subline}
          </p>
        </div>

        <MarketingButton
          asChild
          variant={card.isHighlighted ? "primary" : "secondary"}
          className="w-full"
        >
          <RequestTrialLink
            planIntent={
              card.id === "Pilot"
                ? { plan: "Pilot" }
                : { plan: card.id, cadence: "monthly" }
            }
          >
            {card.ctaLabel}
          </RequestTrialLink>
        </MarketingButton>
      </div>

      {showAiAllowance ? (
        <div className="mt-auto flex items-center gap-2">
          <img
            src={aiAssistantGlyph}
            alt=""
            width={15}
            height={15}
            className="size-[15px] shrink-0"
            aria-hidden
          />
          <p className="m-0 text-sm font-normal leading-5 text-[#171717]">
            {card.aiAllowance}
          </p>
        </div>
      ) : null}
    </div>
  )
}

/**
 * Shared plans strip — AI Assistant `5661:17526` / Home Pricing `4902:19866`.
 */
export function AiAssistantPlans({
  content = AI_ASSISTANT_PLANS,
  showAiAllowance = true,
  surfaceClassName = "bg-[#f0f0f0]",
  compactHeader = false,
}: AiAssistantPlansProps = {}) {
  const body = content.body?.trim() ?? ""
  const showBody = body.length > 0

  return (
    <section className={cn("w-full", surfaceClassName)}>
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px]",
          AI_ASSISTANT_SECTION_INSET,
          AI_ASSISTANT_SECTION_Y,
        )}
      >
        <header
          className={cn(
            "flex w-full flex-col",
            compactHeader ? "max-w-[548px] gap-3.5" : "max-w-[580px] gap-[18px]",
          )}
        >
          <p
            className={cn(
              "m-0 font-medium text-[#141414]",
              compactHeader
                ? "font-jakarta text-sm leading-4"
                : "text-base uppercase leading-normal text-black",
            )}
          >
            {content.eyebrow}
          </p>
          <h2
            className={cn(
              "m-0",
              compactHeader
                ? "font-jakarta text-[28px] font-medium leading-9 text-[#141414] lg:text-[36px] lg:leading-[48px]"
                : AI_ASSISTANT_SECTION_HEADING,
            )}
          >
            {content.title}
          </h2>
          {showBody ? (
            <p className={cn("m-0", AI_ASSISTANT_SECTION_BODY)}>{body}</p>
          ) : null}
        </header>

        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 lg:mx-0 lg:w-full lg:overflow-visible lg:px-0">
          <div className="flex min-w-[960px] gap-[26px] lg:min-w-0">
            {content.cards.map((card, index) => {
              const next = content.cards[index + 1]
              const showDivider =
                !card.isHighlighted && next != null && !next.isHighlighted
              return (
                <PlanCard
                  key={card.id}
                  card={card}
                  showDivider={showDivider}
                  showAiAllowance={showAiAllowance}
                />
              )
            })}
          </div>
        </div>

        <MarketingButton asChild variant="secondary">
          <Link to={content.compareTo}>{content.compareLabel}</Link>
        </MarketingButton>
      </div>
    </section>
  )
}
