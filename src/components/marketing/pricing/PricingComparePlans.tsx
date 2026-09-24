import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { Button } from "@/components/ui/button"
import {
  PRICING_COMPARE_HEADING,
  PRICING_COMPARISON_GROUPS,
  PRICING_PLAN_CARDS,
  PRICING_PLAN_IDS,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  pricingCompareAnnualSubline,
  pricingPlanPrice,
  type PricingPlanId,
} from "@/content/marketing/pricingPage"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

const secondaryButtonClass =
  "h-11 w-full rounded-[4px] border border-[#4e4e4e] bg-transparent text-sm font-medium text-[#141414] shadow-none hover:bg-[#141414]/5"

const primaryButtonClass =
  "h-11 w-full rounded-[4px] bg-[#14a74a] text-sm font-medium text-white shadow-none hover:bg-[#14a74a]/90"

/**
 * Figma compare header (`5437:13879`): Feature 224px + four equal plan cols,
 * gap 22px, light grey strip `#f8f8f8`, pad 24px.
 */
function ComparisonPlanHeader({
  planId,
  cadence,
  showDivider,
}: {
  planId: PricingPlanId
  cadence: BillingCadence
  showDivider: boolean
}) {
  const card = PRICING_PLAN_CARDS.find((entry) => entry.id === planId)
  if (card == null) {
    return null
  }
  const price = pricingPlanPrice(card, cadence)
  const annualLine =
    cadence === "monthly" ? pricingCompareAnnualSubline(planId) : null

  return (
    <div
      className={cn(
        "flex min-h-[198px] min-w-0 flex-1 flex-col justify-between",
        showDivider && "border-l border-[#e0e0e0] pl-[22px]",
      )}
    >
      <div className="flex flex-col gap-[22px]">
        <h3 className="m-0 font-jakarta text-xl font-[500] leading-normal text-[#141414]">
          {card.id}
        </h3>
        <div className="flex flex-col gap-2">
          <p className="m-0 flex flex-wrap items-baseline text-[#141414]">
            <span className="font-jakarta text-[28px] font-[500] leading-normal">
              {price.amount}
            </span>
            <span className="font-sans text-sm font-medium leading-normal">
              {price.suffix}
            </span>
          </p>
          <p className="m-0 min-h-5 max-w-[200px] text-sm font-medium text-[#7c7c7c]">
            {annualLine ?? price.subline}
          </p>
        </div>
      </div>
      <Button
        asChild
        className={card.isMostPopular ? primaryButtonClass : secondaryButtonClass}
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
      </Button>
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
 */
export function PricingComparePlans({ cadence }: PricingComparePlansProps) {
  return (
    <section id="compare-plans" className="w-full scroll-mt-45 bg-white">
      <div
        className={cn(
          "flex w-full flex-col gap-15",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <h2 className="m-0 font-jakarta text-[34px] font-[500] leading-normal text-black lg:text-[46px]">
          {PRICING_COMPARE_HEADING}
        </h2>

        <div className="-mx-5 overflow-x-auto rounded-[6px] sm:mx-0">
          <div className="min-w-[960px] sm:min-w-0">
            {/* Plan header strip — Figma sticky `#f8f8f8` (`5437:13879`). */}
            <div className="flex items-end gap-[22px] border-b border-[#e0e0e0] bg-[#f8f8f8] p-6">
              <div className="flex w-[140px] shrink-0 items-center sm:w-[224px]">
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
                  />
                ))}
              </div>
            </div>

            {/* Feature matrix body */}
            <div className="flex flex-col gap-[26px] bg-white px-6 py-[26px]">
              {PRICING_COMPARISON_GROUPS.map((group) => (
                <div key={group.id} className="flex flex-col">
                  <div className="flex min-h-12.5 items-start">
                    <p className="m-0 w-[140px] shrink-0 text-base font-bold text-[#141414] sm:w-[224px]">
                      {group.title}
                    </p>
                  </div>
                  {group.rows.map((row) => (
                    <div
                      key={row.label}
                      className="flex min-h-12.5 items-start gap-[22px]"
                    >
                      <p className="m-0 w-[140px] shrink-0 py-3.5 text-sm font-semibold text-[#141414] sm:w-[224px]">
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
