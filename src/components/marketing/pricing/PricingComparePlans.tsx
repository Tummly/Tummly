import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { Button } from "@/components/ui/button"
import {
  PRICING_COMPARE_HEADING,
  PRICING_COMPARISON_GROUPS,
  PRICING_PLAN_CARDS,
  PRICING_PLAN_IDS,
  PRICING_SECTION_INSET,
  pricingCompareAnnualSubline,
  pricingPlanPrice,
  type PricingPlanId,
} from "@/content/marketing/pricingPage"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  MANAGE_PLAN_CARD_TITLE_CLASS,
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_PRICE_AMOUNT_CLASS,
  MANAGE_PLAN_PRICE_SUFFIX_CLASS,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

const secondaryButtonClass =
  "h-11 w-full rounded-[4px] border border-[#4e4e4e] bg-transparent text-sm font-medium text-[#141414] shadow-none hover:bg-[#141414]/5"

const primaryButtonClass =
  "h-11 w-full rounded-[4px] bg-[#14a74a] text-sm font-medium text-white shadow-none hover:bg-[#14a74a]/90"

function ComparisonPlanHeader({
  planId,
  cadence,
}: {
  planId: PricingPlanId
  cadence: BillingCadence
}) {
  const card = PRICING_PLAN_CARDS.find((entry) => entry.id === planId)
  if (card == null) {
    return null
  }
  const price = pricingPlanPrice(card, cadence)
  const annualLine =
    cadence === "monthly" ? pricingCompareAnnualSubline(planId) : null

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <div className="flex items-center justify-between gap-2">
          <h3 className={cn(MANAGE_PLAN_CARD_TITLE_CLASS, "text-[25px]")}>
            {card.id}
          </h3>
          {card.isMostPopular ? (
            <span className="rounded bg-[#14a74a]/18 px-3 py-2 text-xs font-medium text-[#14a74a]">
              {MANAGE_PLAN_COPY.mostPopular}
            </span>
          ) : null}
        </div>
        <div className="flex flex-col gap-2">
          <p className="flex flex-wrap items-baseline gap-x-1 text-[#141414]">
            <span className={cn(MANAGE_PLAN_PRICE_AMOUNT_CLASS, "text-[34px]")}>
              {price.amount}
            </span>
            <span className={MANAGE_PLAN_PRICE_SUFFIX_CLASS}>{price.suffix}</span>
          </p>
          <p className="m-0 min-h-5 text-sm font-medium text-[#737373]">
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

/** Figma Compare plans matrix (`5152:10808`). */
export function PricingComparePlans({ cadence }: PricingComparePlansProps) {
  return (
    <section id="compare-plans" className="w-full scroll-mt-[180px] bg-[#f0f0f0]">
      <div
        className={cn(
          "mx-auto flex w-full max-w-[1668px] flex-col gap-8 py-[70px]",
          PRICING_SECTION_INSET,
        )}
      >
        <h2 className="m-0 font-serif text-[34px] font-medium leading-none text-[#141414] lg:text-[46px]">
          {PRICING_COMPARE_HEADING}
        </h2>

        <div className="-mx-5 overflow-x-auto rounded-[6px] bg-white sm:mx-0">
          <div className="min-w-240">
            <div className="grid grid-cols-[160px_repeat(4,minmax(140px,1fr))] gap-x-4 border-b border-[#e0e0e0] px-4 pb-6 pt-6 sm:grid-cols-[224px_repeat(4,minmax(0,1fr))] sm:gap-x-5 sm:px-6">
              <div className="flex items-end pb-1">
                <p className="m-0 text-lg font-medium text-[#141414]">
                  {MANAGE_PLAN_COPY.comparisonFeature}
                </p>
              </div>
              {PRICING_PLAN_IDS.map((planId) => (
                <ComparisonPlanHeader
                  key={planId}
                  planId={planId}
                  cadence={cadence}
                />
              ))}
            </div>

            <div className="px-4 py-2 sm:px-6">
              {PRICING_COMPARISON_GROUPS.map((group) => (
                <div key={group.id} className="py-4">
                  <p className="m-0 pb-4 text-base font-semibold text-[#141414]">
                    {group.title}
                  </p>
                  {group.rows.map((row) => (
                    <div
                      key={row.label}
                      className="grid grid-cols-[160px_repeat(4,minmax(140px,1fr))] gap-x-4 border-b border-[#e0e0e0]/60 py-4 last:border-b-0 sm:grid-cols-[224px_repeat(4,minmax(0,1fr))] sm:gap-x-5"
                    >
                      <p className="m-0 text-sm font-medium text-[#141414]">
                        {row.label}
                      </p>
                      {PRICING_PLAN_IDS.map((planId) => {
                        const value = row.values[planId]
                        const isDash = value === "—" || value === "ー"
                        return (
                          <p
                            key={planId}
                            className={cn(
                              "m-0 text-sm font-normal",
                              isDash ? "text-[#737373]" : "text-[#141414]",
                            )}
                          >
                            {isDash ? "—" : value}
                          </p>
                        )
                      })}
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
