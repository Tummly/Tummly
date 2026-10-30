import { Link } from "react-router-dom"

import aiAssistantGlyph from "@/assets/svg/marketing/ai-assistant-glyph.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PublicPlanPriceFallback,
  PublicPlanPricesStatusMessage,
} from "@/components/marketing/PublicPlanPricesStatus"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  AI_ASSISTANT_PLANS,
  AI_ASSISTANT_SECTION_BODY,
  AI_ASSISTANT_SECTION_HEADING,
  AI_ASSISTANT_SECTION_INSET,
  AI_ASSISTANT_SECTION_Y,
  type AiAssistantPlanStripCard,
} from "@/content/marketing/aiAssistantPage"
import type { PublicPlanPrices } from "@/lib/marketingCustomPlan/publicPlanPrices"
import {
  usePublicPlanPrices,
  type PublicPlanPricesStatus,
} from "@/lib/marketingCustomPlan/usePublicPlanPrices"
import { cn } from "@/lib/utils"

export type MarketingPlansStripContent = {
  eyebrow: string
  title: string
  /** Omit or leave empty to hide the body (Home Pricing). */
  body?: string
  /** Optional note under the plan cards (Pilot `5679:34414`). */
  footnote?: string
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
   * Compact header metrics for Home Pricing (`4902:19866` / mobile `6241:18264`):
   * 12–14px eyebrow, 28/36 title, no body.
   */
  compactHeader?: boolean
  /**
   * Home Pricing `4902:19866`: header copy left, "Compare all plans" right
   * (bottom-aligned on desktop), 46/52 title. The compare button leaves the footer.
   */
  splitHeader?: boolean
  /**
   * Where the grey line under the price comes from. `catalogue` (default) shows the
   * published annual price; `card` shows `card.subline` (Home Pricing location limits).
   * Monthly amounts always come from the catalogue.
   */
  sublineSource?: "catalogue" | "card"
}

/** Pilot keeps its content copy; paid plans read the published catalogue. */
function planPriceLines(
  card: AiAssistantPlanStripCard,
  prices: PublicPlanPrices,
  sublineSource: "catalogue" | "card",
): { amount: string | null; subline: string | null } {
  if (card.id === "Pilot") {
    return { amount: card.priceAmount ?? null, subline: card.subline ?? null }
  }
  const planPrice = prices[card.id]
  return {
    amount: planPrice?.monthly ?? null,
    subline:
      sublineSource === "card"
        ? (card.subline ?? null)
        : planPrice != null
          ? `${planPrice.annual}/year`
          : null,
  }
}

function PlanCard({
  card,
  showDivider,
  showAiAllowance,
  prices,
  status,
  sublineSource,
}: {
  card: AiAssistantPlanStripCard
  /** Right divider between adjacent non-highlighted cards on desktop (skip before Growth). */
  showDivider: boolean
  showAiAllowance: boolean
  prices: PublicPlanPrices
  status: PublicPlanPricesStatus
  sublineSource: "catalogue" | "card"
}) {
  const price = planPriceLines(card, prices, sublineSource)
  /* Home Pricing (`sublineSource="card"`): Figma `7190:11030` type metrics. */
  const homeFigma = sublineSource === "card"
  return (
    <div
      className={cn(
        "flex w-full min-w-0 flex-col gap-10 overflow-hidden py-6 lg:flex-1",
        card.isHighlighted
          ? cn(
              "rounded-[6px] border border-[#14a74a] bg-[rgba(20,167,74,0.05)] px-[26px]",
              // Figma `7183:10960`: the 1px stroke sits inside the 315px frame.
              homeFigma && "py-[23px]",
            )
          : cn(
              "border-[#cacaca]",
              showDivider && "border-b lg:border-b-0 lg:border-r lg:pr-[26px]",
            ),
      )}
    >
      <div className="flex flex-col gap-3">
        <h3
          className={cn(
            "m-0 font-jakarta text-[30px] font-medium leading-none text-[#141414]",
            // Figma `7183:10965` (highlighted) #262626; 37.8px intrinsic line height.
            homeFigma && "lg:leading-[37.8px]",
            homeFigma && card.isHighlighted && "text-[#262626]",
          )}
        >
          {card.name}
        </h3>
        <p className="m-0 max-w-[346px] text-sm font-normal leading-[19px] text-[#141414]">
          {card.description}
        </p>
      </div>

      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-2">
          {price.amount != null ? (
            <p className="m-0 text-[#141414]">
              <span
                className={cn(
                  "text-[26px] font-normal leading-none lg:text-[36px]",
                  // Figma `7183:10957`: 36px type in a 47px (1.3) line box.
                  homeFigma && "lg:leading-[47px]",
                )}
              >
                {price.amount}
              </span>
              <span className="text-sm font-normal leading-none">
                {card.priceSuffix}
              </span>
            </p>
          ) : (
            <PublicPlanPriceFallback status={status} />
          )}
          {price.subline != null ? (
            <p
              className={cn(
                "m-0 text-sm font-normal leading-[normal] text-[#7c7c7c]",
                // Figma `7183:10947`: Medium 500, 18.228px line height.
                homeFigma && "font-medium leading-[18.228px]",
              )}
            >
              {price.subline}
            </p>
          ) : null}
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
 * Mobile stacks cards (Figma `6241:18264`); desktop keeps the horizontal strip.
 */
export function AiAssistantPlans({
  content = AI_ASSISTANT_PLANS,
  showAiAllowance = true,
  surfaceClassName = "bg-[#f0f0f0]",
  compactHeader = false,
  splitHeader = false,
  sublineSource = "catalogue",
}: AiAssistantPlansProps = {}) {
  const body = content.body?.trim() ?? ""
  const showBody = body.length > 0
  const { status, prices, retry } = usePublicPlanPrices()

  return (
    <section className={cn("w-full", surfaceClassName)}>
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px]",
          AI_ASSISTANT_SECTION_INSET,
          AI_ASSISTANT_SECTION_Y,
        )}
      >
        <div
          className={cn(
            "flex w-full flex-col gap-[46px]",
            splitHeader && "lg:flex-row lg:items-end lg:justify-between",
          )}
        >
          <header
            className={cn(
              "flex w-full flex-col",
              compactHeader
                ? "max-w-[548px] gap-3.5"
                : "max-w-[651px] gap-[22px]",
              splitHeader && "lg:w-auto",
            )}
          >
            <div className="flex w-full flex-col gap-3.5">
              <p
                className={cn(
                  "m-0 font-medium text-[#141414]",
                  splitHeader
                    ? "font-sans text-sm font-medium uppercase leading-4 tracking-[1.12px] text-[#4a4a4a]"
                    : compactHeader
                      ? "font-jakarta text-xs leading-[14px] lg:text-sm lg:leading-4"
                      : "font-sans text-sm uppercase leading-[normal] tracking-[1.12px] text-black",
                )}
              >
                {content.eyebrow}
              </p>
              <h2
                className={cn(
                  "m-0",
                  splitHeader
                    ? "max-w-[548px] font-jakarta text-[34px] font-medium leading-10 text-[#141414] lg:text-[46px] lg:leading-[52px]"
                    : compactHeader
                      ? "font-jakarta text-[28px] font-medium leading-9 text-[#141414] lg:text-[36px] lg:leading-[48px]"
                      : AI_ASSISTANT_SECTION_HEADING,
                )}
              >
                {content.title}
              </h2>
            </div>
            {showBody ? (
              <p
                className={cn(
                  "m-0",
                  splitHeader ? "max-w-[430px]" : "max-w-[592px]",
                  AI_ASSISTANT_SECTION_BODY,
                )}
              >
                {body}
              </p>
            ) : null}
          </header>
          {splitHeader ? (
            <MarketingButton
              asChild
              variant="secondary"
              className="w-fit shrink-0"
            >
              <Link to={content.compareTo}>{content.compareLabel}</Link>
            </MarketingButton>
          ) : null}
        </div>

        <div className="flex w-full flex-col gap-6">
          <PublicPlanPricesStatusMessage status={status} onRetry={retry} />
          <div className="flex w-full flex-col gap-[26px] lg:flex-row lg:gap-[26px]">
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
                  prices={prices}
                  status={status}
                  sublineSource={sublineSource}
                />
              )
            })}
          </div>
        </div>

        {content.footnote != null && content.footnote.trim() !== "" ? (
          <p className="m-0 max-w-[720px] font-sans text-sm font-normal leading-[18px] text-[#141414]">
            {content.footnote}
          </p>
        ) : null}

        {splitHeader ? null : (
          <MarketingButton asChild variant="secondary">
            <Link to={content.compareTo}>{content.compareLabel}</Link>
          </MarketingButton>
        )}
      </div>
    </section>
  )
}
