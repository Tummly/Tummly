import { useRef, type UIEvent } from "react"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { PublicPlanPriceFallback } from "@/components/marketing/PublicPlanPricesStatus"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PRICING_COMPARE_HEADING,
  PRICING_COMPARISON_GROUPS,
  PRICING_PLAN_CARDS,
  PRICING_PLAN_IDS,
  PRICING_SECTION_HEADING,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  pricingCompareAnnualSubline,
  pricingPlanPrice,
  type PricingPlanId,
} from "@/content/marketing/pricingPage"
import { marketingHeaderScrollMarginClass } from "@/lib/marketing-layout"
import type { PublicPlanPrices } from "@/lib/marketingCustomPlan/publicPlanPrices"
import {
  usePublicPlanPrices,
  type PublicPlanPricesStatus,
} from "@/lib/marketingCustomPlan/usePublicPlanPrices"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

/**
 * Figma compare header (`5437:13879`): Feature 224px + four equal plan cols,
 * gap 22px, light grey strip `#f8f8f8`, pad 24px.
 */
function ComparisonPlanHeader({
  planId,
  cadence,
  showDivider,
  prices,
  status,
}: {
  planId: PricingPlanId
  cadence: BillingCadence
  showDivider: boolean
  prices: PublicPlanPrices
  status: PublicPlanPricesStatus
}) {
  const card = PRICING_PLAN_CARDS.find((entry) => entry.id === planId)
  if (card == null) {
    return null
  }
  const price = pricingPlanPrice(card, cadence, prices)
  const annualLine =
    cadence === "monthly" ? pricingCompareAnnualSubline(planId, prices) : null

  return (
    <div
      className={cn(
        "flex min-h-[198px] min-w-0 flex-1 flex-col justify-between",
        showDivider && "border-l border-[#e0e0e0] pl-[22px]",
      )}
    >
      <div className="flex flex-col gap-[22px]">
        <h3 className="m-0 font-jakarta text-xl font-[500] leading-none text-[#141414]">
          {card.id}
        </h3>
        <div className="flex flex-col gap-2">
          {price.amount != null ? (
            <p className="m-0 flex flex-wrap items-baseline text-[#141414]">
              <span className="font-sans text-[28px] font-[500] leading-none">
                {price.amount}
              </span>
              <span className="font-sans text-sm font-medium leading-[normal]">
                {price.suffix}
              </span>
            </p>
          ) : (
            <PublicPlanPriceFallback status={status} />
          )}
          <p className="m-0 min-h-5 max-w-[200px] text-sm font-medium text-[#7c7c7c]">
            {annualLine ?? price.subline}
          </p>
        </div>
      </div>
      <MarketingButton
        asChild
        variant={card.isMostPopular ? "primary" : "secondary"}
        className="w-full"
      >
        <RequestTrialLink
          planIntent={
            planId === "Pilot"
              ? { plan: "Pilot" }
              : { plan: planId, cadence }
          }
        >
          {card.ctaLabel}
        </RequestTrialLink>
      </MarketingButton>
    </div>
  )
}

type PricingComparePlansProps = {
  cadence: BillingCadence
}

/**
 * Figma Compare plans (`5437:13695`):
 * white section, Jakarta Medium 46 heading, 60px gap to table,
 * sticky `#f8f8f8` plan header, 224px feature col.
 *
 * Header and body use separate overflow-x scrollers so `sticky` can clear
 * the marketing nav; horizontal scroll stays in sync.
 */
export function PricingComparePlans({ cadence }: PricingComparePlansProps) {
  // The hero shows the loading or failed copy once; this table only needs the prices.
  const { status, prices } = usePublicPlanPrices()
  const headerScrollRef = useRef<HTMLDivElement>(null)
  const bodyScrollRef = useRef<HTMLDivElement>(null)
  const syncLockRef = useRef(false)

  function syncHorizontalScroll(
    source: "header" | "body",
    event: UIEvent<HTMLDivElement>,
  ) {
    if (syncLockRef.current) {
      return
    }
    const left = event.currentTarget.scrollLeft
    const target =
      source === "header" ? bodyScrollRef.current : headerScrollRef.current
    if (target == null || target.scrollLeft === left) {
      return
    }
    syncLockRef.current = true
    target.scrollLeft = left
    syncLockRef.current = false
  }

  return (
    <section
      id="compare-plans"
      className={cn("w-full bg-white", marketingHeaderScrollMarginClass)}
    >
      <div
        className={cn(
          "flex w-full flex-col gap-15",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <h2 className={cn("m-0", PRICING_SECTION_HEADING)}>
          {PRICING_COMPARE_HEADING}
        </h2>

        <div className="rounded-[6px]">
          {/* Plan header strip — sticky under marketing nav (`5437:13879`). */}
          <div
            ref={headerScrollRef}
            onScroll={(event) => {
              syncHorizontalScroll("header", event)
            }}
            className={cn(
              // 20px below the nav; the white shadow fills the gap so rows do not show through.
              "sticky top-[106px] z-20 -mx-5 overflow-x-auto overscroll-x-contain bg-[#f8f8f8] shadow-[0_-20px_0_0_#fff] sm:mx-0",
            )}
          >
            <div className="flex min-w-[960px] items-end gap-[22px] p-6">
              <div className="flex w-[224px] shrink-0 items-center">
                <p className="m-0 text-lg font-semibold text-[#141414]">
                  Feature
                </p>
              </div>
              <div className="flex min-w-0 flex-1 gap-[22px]">
                {PRICING_PLAN_IDS.map((planId, index) => (
                  <ComparisonPlanHeader
                    key={planId}
                    planId={planId}
                    cadence={cadence}
                    showDivider={index > 0}
                    prices={prices}
                    status={status}
                  />
                ))}
              </div>
            </div>
          </div>

          {/* Feature matrix body */}
          <div
            ref={bodyScrollRef}
            onScroll={(event) => {
              syncHorizontalScroll("body", event)
            }}
            className="-mx-5 overflow-x-auto overscroll-x-contain sm:mx-0"
          >
            <div className="flex min-w-[960px] flex-col gap-[26px] bg-white px-6 py-[26px]">
              {PRICING_COMPARISON_GROUPS.map((group) => (
                <div key={group.id} className="flex flex-col">
                  <div className="flex min-h-12.5 items-start">
                    <p className="m-0 w-[224px] shrink-0 text-base font-bold text-[#141414]">
                      {group.title}
                    </p>
                  </div>
                  {group.rows.map((row) => (
                    <div
                      key={row.label}
                      className="flex min-h-12.5 items-start gap-[22px]"
                    >
                      <p className="m-0 w-[224px] shrink-0 py-3.5 text-sm font-semibold text-[#141414]">
                        {row.label}
                      </p>
                      <div className="flex min-w-0 flex-1 gap-[22px]">
                        {PRICING_PLAN_IDS.map((planId, index) => {
                          const value = row.values[planId]
                          const isDash = value === "—" || value === "ー"
                          return (
                            <p
                              key={planId}
                              className={cn(
                                "m-0 min-w-0 flex-1 py-3.5 text-sm font-normal",
                                index > 0 && "border-l border-[#e0e0e0] pl-[22px]",
                                isDash ? "text-[#7c7c7c]" : "text-[#141414]",
                              )}
                            >
                              {isDash ? "—" : value}
                            </p>
                          )
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
